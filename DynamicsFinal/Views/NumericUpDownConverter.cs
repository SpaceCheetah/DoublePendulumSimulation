using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace DynamicsFinal.Views; 

public class NumericUpDownConverter : IValueConverter {
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is double v && targetType == typeof(decimal?)) {
            return new decimal(v);
        }
        return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (targetType != typeof(double))
            return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
        if (value is decimal v) {
            return (double)v;
        }
        if (parameter is string s && double.TryParse(s, out double result)) {
            return result;
        }
        return new BindingNotification(new ArgumentException("Parameter must be a valid double"), BindingErrorType.Error);
    }
}