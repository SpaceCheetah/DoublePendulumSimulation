using LiveChartsCore.SkiaSharpView;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;

namespace DynamicsFinal.ViewModels;

public class MainViewModel : ViewModelBase {
    double _stepSize = 0.0001;
    public double StepSize {
        get => _stepSize;
        set => this.RaiseAndSetIfChanged(ref _stepSize, value);
    }
    int _stepsPerInterval = 100;
    public int StepsPerInterval {
        get => _stepsPerInterval;
        set => this.RaiseAndSetIfChanged(ref _stepsPerInterval, value);
    }
    double _speed = 1;
    public double Speed {
        get => _speed;
        set => this.RaiseAndSetIfChanged(ref _speed, value);
    }
    StateVector _state = new StateVector(1, 2, 0, 0);
    public StateVector State {
        get => _state;
        set => this.RaiseAndSetIfChanged(ref _state, value);
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

    readonly ObservableAsPropertyHelper<double> _energyVelocity;
    public double EnergyVelocity => _energyVelocity.Value;
    readonly ObservableAsPropertyHelper<double> _energyInertia;
    public double EnergyInertia => _energyInertia.Value;
    readonly ObservableAsPropertyHelper<double> _energyGravity;
    public double EnergyGravity => _energyGravity.Value;

    //Using PieSeries<MainViewModel> instead of just PieSeries<double> to take advantage of IObservablePropertyChanged causing auto updates
    PieSeries<MainViewModel> AsPie(Func<double> getter, string name) => new() {
        Values = new List<MainViewModel> {this},
        Mapping = (_, point) => point.PrimaryValue = getter(),
        Name = name
    };

    public PieSeries<MainViewModel>[] EnergyData { get; }

    IObservable<StateVector> CreateRunCommand() {
        Simulation simulation = new(L1, L2, M1, M2, G);
        return Observable.Interval(TimeSpan.FromSeconds(StepSize * StepsPerInterval / Speed))
            .Scan(State, (vector, _) => {
                for (int i = 0; i < StepsPerInterval; i++) {
                    vector = simulation.Step(vector, StepSize);
                }
                return vector;
            })
            .SubscribeOn(RxApp.TaskpoolScheduler)
            .ObserveOn(RxApp.MainThreadScheduler)
            .TakeUntil(StopCommand);
    }

    public MainViewModel() {
        RunCommand = ReactiveCommand.CreateFromObservable(CreateRunCommand);
        RunCommand.Subscribe(vector => State = vector);
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
        
        EnergyData = new[] {
            AsPie(() => EnergyVelocity / (EnergyGravity + EnergyVelocity + EnergyInertia) * 100, "Velocity"),
            AsPie(() => EnergyInertia / (EnergyGravity + EnergyVelocity + EnergyInertia) * 100, "Inertia"),
            AsPie(() => EnergyGravity / (EnergyGravity + EnergyVelocity + EnergyInertia) * 100, "Gravity")
        };
    }

    public ReactiveCommand<Unit, StateVector> RunCommand { get; }
    public ReactiveCommand<Unit, Unit> StopCommand { get; }

}