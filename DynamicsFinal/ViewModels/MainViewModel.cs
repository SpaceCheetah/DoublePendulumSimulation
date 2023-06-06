using LiveChartsCore.SkiaSharpView;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace DynamicsFinal.ViewModels;

public class MainViewModel : ViewModelBase {
    double _stepSize = 0.0001;
    public double StepSize {
        get => _stepSize;
        set => this.RaiseAndSetIfChanged(ref _stepSize, value);
    }
    double _speed = 1;
    public double Speed {
        get => _speed;
        set => this.RaiseAndSetIfChanged(ref _speed, value);
    }
    double _l1 = 1;
    public double L1 {
        get => _l1;
        set => this.RaiseAndSetIfChanged(ref _l1, value);
    }
    double _l2 = 1;
    public double L2 {
        get => _l2;
        set => this.RaiseAndSetIfChanged(ref _l2, value);
    }
    double _m1 = 1;
    public double M1 {
        get => _m1;
        set => this.RaiseAndSetIfChanged(ref _m1, value);
    }
    double _m2 = 1;
    public double M2 {
        get => _m2;
        set => this.RaiseAndSetIfChanged(ref _m2, value);
    }
    double _g = 9.81;
    public double G {
        get => _g;
        set => this.RaiseAndSetIfChanged(ref _g, value);
    }
    double _theta1 = 1;
    public double Theta1 {
        get => _theta1;
        set => this.RaiseAndSetIfChanged(ref _theta1, value);
    }
    double _theta2 = 2;
    public double Theta2 {
        get => _theta2;
        set => this.RaiseAndSetIfChanged(ref _theta2, value);
    }
    double _omega1;
    public double Omega1 {
        get => _omega1;
        set => this.RaiseAndSetIfChanged(ref _omega1, value);
    }
    double _omega2;
    public double Omega2 {
        get => _omega2;
        set => this.RaiseAndSetIfChanged(ref _omega2, value);
    }
    double _interval = 0.01;
    public double Interval {
        get => _interval;
        set => this.RaiseAndSetIfChanged(ref _interval, value);
    }

    readonly ObservableAsPropertyHelper<double> _energyVelocity;
    public double EnergyVelocity => _energyVelocity.Value;
    readonly ObservableAsPropertyHelper<double> _energyInertia;
    public double EnergyInertia => _energyInertia.Value;
    readonly ObservableAsPropertyHelper<double> _energyGravity;
    public double EnergyGravity => _energyGravity.Value;
    readonly ObservableAsPropertyHelper<StateVector> _state;
    public StateVector State => _state.Value;
    readonly ObservableAsPropertyHelper<bool> _editable;
    public bool Editable => _editable.Value;
    readonly ObservableAsPropertyHelper<int> _stepsPerInterval;
    public int StepsPerInterval => _stepsPerInterval.Value;

    //Using PieSeries<MainViewModel> instead of just PieSeries<double> to take advantage of IObservablePropertyChanged causing auto updates
    PieSeries<MainViewModel> AsPie(Func<double> getter, string name) => new() {
        Values = new List<MainViewModel> {this},
        Mapping = (_, point) => point.PrimaryValue = getter(),
        Name = name
    };

    public PieSeries<MainViewModel>[] EnergyData { get; }

    IObservable<StateVector> CreateRunCommand() => this.WhenAnyValue(m => m.StepsPerInterval)
        .Select(_ => {
            Simulation simulation = new(L1, L2, M1, M2, G);
            DateTimeOffset start = DefaultScheduler.Instance.Now;
            //Observable.Interval doesn't seem to take into account the time of execution, so using Generate instead
            return Observable.Generate(0, _ => true, i => i + 1, i => i,
                    i => start + i * TimeSpan.FromSeconds(StepSize * StepsPerInterval / Speed))
                .Scan(State, (vector, _) => {
                    for (int i = 0; i < StepsPerInterval; i++) {
                        vector = simulation.Step(vector, StepSize);
                    }
                    return vector;
                })
                .SubscribeOn(RxApp.TaskpoolScheduler)
                .ObserveOn(RxApp.MainThreadScheduler);
        }).Switch().TakeUntil(StopCommand);

    public MainViewModel() {
        RunCommand = ReactiveCommand.CreateFromObservable(CreateRunCommand);
        RunCommand.Subscribe(vector => {
            Theta1 = vector.Theta1;
            Theta2 = vector.Theta2;
            Omega1 = vector.Omega1;
            Omega2 = vector.Omega2;
        });
        this.WhenAnyValue(v => v.Theta1, v => v.Theta2, v => v.Omega1, v => v.Omega2, (theta1, theta2, omega1,
            omega2) => new StateVector(theta1, theta2, omega1, omega2)).ToProperty(this, m => m.State, out _state);
        StopCommand = ReactiveCommand.Create(() => {}, RunCommand.IsExecuting);
        this.WhenAnyValue(m => m.L1, m => m.L2, m => m.M1, m => m.M2, m => m.State, (l1, l2, m1, m2, state) => {
            double vx = Math.Cos(state.Theta1) * state.Omega1 * l1 +
                        m2 / (m1 + m2) * Math.Cos(state.Theta2) * state.Omega2 * l2;
            double vy = Math.Sin(state.Theta1) * state.Omega1 * l1 +
                        m2 / (m1 + m2) * Math.Sin(state.Theta2) * state.Omega2 * l2;
            return 0.5 * (m1 + m2) * (vx * vx + vy * vy);
        }).ToProperty(this, m => m.EnergyVelocity, out _energyVelocity);
        this.WhenAnyValue(m => m.L2, m => m.M1, m => m.M2, m => m.State, (l2, m1, m2, state) => {
            double ig = l2 * l2 * m1 * m2 / (m1 + m2);
            return 0.5 * ig * state.Omega2 * state.Omega2;
        }).ToProperty(this, m => m.EnergyInertia, out _energyInertia);
        this.WhenAnyValue(m => m.L1, m => m.L2, m => m.M1, m => m.M2, m => m.G, m => m.State, (l1, l2, m1, m2, g, state) => {
            //y should be 0 at lowest point
            double y = l1 + m2 / (m1 + m2) * l2 - Math.Cos(state.Theta1) * l1 - m2 / (m1 + m2) * Math.Cos(state.Theta2) * l2;
            return (m1 + m2) * g * y;
        }).ToProperty(this, m => m.EnergyGravity, out _energyGravity);
        this.WhenAnyValue(m => m.Interval, m => m.StepSize, m => m.Speed,
                (interval, stepSize, speed) => interval / stepSize * speed)
            .Select(Convert.ToInt32)
            .Select(steps => Math.Max(steps, 1))
            .ToProperty(this, m => m.StepsPerInterval, out _stepsPerInterval);
        
        EnergyData = new[] {
            AsPie(() => EnergyVelocity, "Velocity"),
            AsPie(() => EnergyInertia, "Inertia"),
            AsPie(() => EnergyGravity, "Gravity")
        };
        RunCommand.CanExecute.ToProperty(this, m => m.Editable, out _editable);
    }
    

    public ReactiveCommand<Unit, StateVector> RunCommand { get; }
    public ReactiveCommand<Unit, Unit> StopCommand { get; }

}