using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Utilities;
using System;
using System.Globalization;

namespace DynamicsFinal.Views; 

public class AngleConverter : IValueConverter {
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        //Avalonia uses decimal for NumericUpDown, which makes sense (to avoid extraneous digits), though that doesn't help here
        if (value is double v && targetType == typeof(decimal?)) {
            return new decimal(v * 360 / Math.Tau);
        }
        return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (targetType != typeof(double))
            return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
        if (value is decimal v) {
            return (double)v * Math.Tau / 360;
        }
        return 0.0;
    }
}