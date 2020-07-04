using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Monocast
{
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (targetType == typeof(bool) || targetType == typeof(bool?))
            {
                bool? formatBool = parameter as bool?;
                if (formatBool.HasValue)
                    return !formatBool.Value;
                throw new NullReferenceException("Boolean value cannot be null.");
            }
            else if (targetType == typeof(Visibility))
            {
                switch (parameter as Visibility?)
                {
                    case Visibility.Collapsed:
                        return Visibility.Visible;
                    default:
                        return Visibility.Collapsed;
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return Convert(value, targetType, parameter, language);
        }
    }

    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (targetType == typeof(Visibility) || targetType == typeof(Visibility?))
            {
                var formatValue = parameter as Visibility?;
                if (formatValue.HasValue && formatValue.Value == Visibility.Collapsed)
                {
                    return false;
                }
                return true;
            }
            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (targetType == typeof(bool) || targetType == typeof(bool?))
            {
                var formatValue = parameter as bool?;
                if (formatValue.HasValue && !formatValue.Value)
                {
                    return Visibility.Collapsed;
                }
                return Visibility.Visible;
            }
            throw new NotImplementedException();
        }
    }
}
