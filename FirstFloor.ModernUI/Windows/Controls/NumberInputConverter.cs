using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using FirstFloor.ModernUI.Serialization;
using JetBrains.Annotations;

namespace FirstFloor.ModernUI.Windows.Controls {
    public class NumberInputConverter : INumberInputConverter, IValueConverter, IMultiValueConverter {
        public static INumberInputConverter Default { get; } = new NumberInputConverter(null, null);

        [CanBeNull]
        private readonly Func<string, double?> _parse;

        [CanBeNull]
        private readonly Func<double, string> _backToString;

        public NumberInputConverter([CanBeNull] Func<string, double?> parse, [CanBeNull] Func<double, string> backToString) {
            _parse = parse;
            _backToString = backToString;
        }

        public double? TryToParse(string value) {
            return _parse != null ? _parse.Invoke(value)
                    : BetterTextBox.FlexibleParser.TryParseDouble(value, out var parsed) ? parsed : (double?)null;
        }

        public string BackToString(double value) {
            return _backToString?.Invoke(value) ?? value.ToString(CultureInfo.CurrentUICulture);
        }

        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return BackToString(value.As<double>());
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return TryToParse(value?.ToString() ?? "") ?? 0;
        }

        object IMultiValueConverter.Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            return BackToString(values.FirstOrDefault().As<double>());
        }

        object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            return new object[]{ TryToParse(value?.ToString() ?? "") ?? 0 };
        }
    }
}