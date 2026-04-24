using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace PirepEncoder.ViewModels;

public sealed class EnumEqualsConverter : IValueConverter
{
    public static readonly EnumEqualsConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null || parameter is null)
        {
            return false;
        }
        return value.Equals(parameter);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b && b && parameter is not null)
        {
            return parameter;
        }
        return BindingOperations.DoNothing;
    }
}
