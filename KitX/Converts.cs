using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace KitX
{

    [ValueConversion(typeof(string), typeof(bool))]
    public class EditMenuEnable : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((int)value == -1)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((int)value == -1)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    [ValueConversion(typeof(string), typeof(string))]
    public class GetOpenFolder : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => $"path|{value}";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw null;
    }

    [ValueConversion(typeof(int), typeof(bool))]
    public class GetAccViewMenuEnable : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => (int)value != 2;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw null;
    }

    [ValueConversion(typeof(int), typeof(bool))]
    public class GetAccInfoViewMenuEnable : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => (int)value == 2;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw null;
    }

    [ValueConversion(typeof(int), typeof(bool))]
    public class GetAppIcon : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                return Library.Windows.GetAppIcon.GetIcon((string)value);
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw null;
    }

    class Converts
    {
        public static string Config_ToolsPath(string value)
        {
            if (value.Equals("Self"))
            {
                return $"{App.WorkBase}\\Plug-ins\\";
            }
            else
            {
                return value;
            }
        }

        public static Point Config_Location(string value, double width, double height)
        {
            switch (value)
            {
                case "CenterScreen":
                    return new Point()
                    {
                        X = (App.WorkAreaWidth - width) / 2,
                        Y = (App.WorkAreaHeight - height) / 2,
                    };
                default:
                    string[] Locs = value.Split(',');
                    return new Point()
                    {
                        X = Convert.ToDouble(Locs[0]),
                        Y = Convert.ToDouble(Locs[1])
                    };
            }
        }

        public static Point Config_Size(string value)
        {
            string[] lgs = value.Split(',');
            return new Point()
            {
                X = Convert.ToDouble(lgs[0]),
                Y = Convert.ToDouble(lgs[1])
            };
        }
    }
}

