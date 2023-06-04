using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.ReactiveUI;
using DynamicsFinal.ViewModels;
using ReactiveUI;
using System;
using System.Linq.Expressions;
using System.Reactive.Linq;

namespace DynamicsFinal.Views;

public partial class MainView : ReactiveUserControl<MainViewModel> {
    static IObservable<T> GetProperty<T>(IObservable<MainViewModel> model, Expression<Func<MainViewModel, T>> selectProperty) =>
        model.Select(v => v.WhenAnyValue(selectProperty)).Switch();

    static (Point center, Point a, Point b) GetCoords(StateVector state, double l1, double l2, Size canvasSize) {
        (double top, double left, double size) = BorderBounds(canvasSize);
        double ax = l1 * Math.Sin(state.Theta1);
        double ay = -l1 * Math.Cos(state.Theta1);
        double bx = ax + l2 * Math.Sin(state.Theta2);
        double by = ay - l2 * Math.Cos(state.Theta2);
        double scale = size / (l1 + l2) / 2.5;
        double centerX = left + size / 2;
        double centerY = top + size / 2;
        return (new Point(centerX, centerY), new Point(centerX + ax * scale, centerY - ay * scale),
            new Point(centerX + bx * scale, centerY - by * scale));
    }

    static (double top, double left, double size) BorderBounds(Size size) {
        double minDimension = Math.Min(size.Width, size.Height);
        return ((size.Height - minDimension) / 2, (size.Width - minDimension) / 2, minDimension);
    }

    public MainView() {
        InitializeComponent();
        IObservable<MainViewModel> viewModel = this.GetObservable(ViewModelProperty).WhereNotNull();
        IObservable<StateVector> stateVector = GetProperty(viewModel, v => v.State);
        IObservable<double> l1 = GetProperty(viewModel, v => v.L1);
        IObservable<double> l2 = GetProperty(viewModel, v => v.L2);
        IObservable<Size> canvasSize =
            Observable.FromEventPattern<SizeChangedEventArgs>(h => Canvas.SizeChanged += h,
                    h => Canvas.SizeChanged -= h)
                .Select(pattern => pattern.EventArgs.NewSize);
        IObservable <(Point center, Point a, Point b)> coords = stateVector.CombineLatest(l1, l2, canvasSize, GetCoords);
        Line1.Bind(Line.StartPointProperty, coords.Select(points => points.center));
        Line1.Bind(Line.EndPointProperty, coords.Select(points => points.a));
        Line2.Bind(Line.StartPointProperty, coords.Select(points => points.a));
        Line2.Bind(Line.EndPointProperty, coords.Select(points => points.b));
        Border.Bind(Canvas.TopProperty, canvasSize.Select(BorderBounds).Select(bounds => bounds.top));
        Border.Bind(Canvas.LeftProperty, canvasSize.Select(BorderBounds).Select(bounds => bounds.left));
        Border.Bind(HeightProperty, canvasSize.Select(BorderBounds).Select(bounds => bounds.size));
        Border.Bind(WidthProperty, canvasSize.Select(BorderBounds).Select(bounds => bounds.size));
        Support.Bind(Canvas.TopProperty, coords.Select(points => points.center.Y - Support.Height / 2));
        Support.Bind(Canvas.LeftProperty, coords.Select(points => points.center.X - Support.Width / 2));
        PointA.Bind(Canvas.TopProperty, coords.Select(points => points.a.Y - PointA.Height / 2));
        PointA.Bind(Canvas.LeftProperty, coords.Select(points => points.a.X - PointB.Width / 2));
        PointB.Bind(Canvas.TopProperty, coords.Select(points => points.b.Y - PointB.Height / 2));
        PointB.Bind(Canvas.LeftProperty, coords.Select(points => points.b.X - PointB.Width / 2));
    }
}