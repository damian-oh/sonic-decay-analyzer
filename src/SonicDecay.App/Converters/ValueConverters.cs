using System.Globalization;

namespace SonicDecay.App.Converters
{
    /// <summary>
    /// Inverts a boolean value.
    /// </summary>
    public class InvertedBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }
    }

    /// <summary>
    /// Converts a string to boolean (true if not null/empty).
    /// </summary>
    public class StringNotEmptyConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return !string.IsNullOrEmpty(value as string);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Converts an integer to boolean (true if greater than 0).
    /// </summary>
    public class IntToBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int intValue)
            {
                return intValue > 0;
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Converts string number (1-6) to picker index (0-5) and back.
    /// </summary>
    public class StringNumberIndexConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int stringNumber)
            {
                return stringNumber - 1; // 1-6 to 0-5
            }
            return 0;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int index)
            {
                return index + 1; // 0-5 to 1-6
            }
            return 1;
        }
    }

    /// <summary>
    /// Converts IsCapturing boolean to button text.
    /// </summary>
    public class CaptureButtonTextConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isCapturing)
            {
                return isCapturing ? "Stop Capture" : "Start Capture";
            }
            return "Start Capture";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Converts decay percentage to health color.
    /// </summary>
    public class DecayToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double decay)
            {
                if (decay < 15)
                    return Color.FromArgb("#22c55e"); // Green
                if (decay < 30)
                    return Color.FromArgb("#eab308"); // Yellow
                if (decay < 50)
                    return Color.FromArgb("#f97316"); // Orange
                return Color.FromArgb("#ef4444"); // Red
            }
            return Colors.Gray;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Converts IsEditing boolean to save button text.
    /// </summary>
    public class SaveButtonTextConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isEditing)
            {
                return isEditing ? "Update" : "Save";
            }
            return "Save";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Converts an object to boolean (true if not null).
    /// </summary>
    public class NotNullConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Converts SelectedPreset to background color for preset buttons.
    /// Returns Primary color if parameter matches value, otherwise Gray.
    /// </summary>
    public class PresetToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var selectedPreset = value as string;
            var buttonPreset = parameter as string;

            if (string.Equals(selectedPreset, buttonPreset, StringComparison.OrdinalIgnoreCase))
            {
                // Selected - use Primary color
                return Application.Current?.Resources.TryGetValue("Primary", out var primaryColor) == true
                    ? primaryColor
                    : Color.FromArgb("#512BD4");
            }

            // Not selected - use Gray based on theme
            if (Application.Current?.RequestedTheme == AppTheme.Dark)
            {
                return Application.Current?.Resources.TryGetValue("Gray600", out var darkGray) == true
                    ? darkGray
                    : Color.FromArgb("#404040");
            }

            return Application.Current?.Resources.TryGetValue("Gray100", out var lightGray) == true
                ? lightGray
                : Color.FromArgb("#F0F0F0");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Converts SelectedPreset to text color for preset buttons.
    /// Returns White if parameter matches value, otherwise theme-appropriate color.
    /// </summary>
    public class PresetToTextColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var selectedPreset = value as string;
            var buttonPreset = parameter as string;

            if (string.Equals(selectedPreset, buttonPreset, StringComparison.OrdinalIgnoreCase))
            {
                // Selected - use White text
                return Colors.White;
            }

            // Not selected - use theme-appropriate text color
            if (Application.Current?.RequestedTheme == AppTheme.Dark)
            {
                return Colors.White;
            }

            return Application.Current?.Resources.TryGetValue("Gray900", out var darkText) == true
                ? darkText
                : Color.FromArgb("#212121");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
