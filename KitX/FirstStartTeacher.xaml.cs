using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace KitX
{
    /// <summary>
    /// FirstStartTeacher.xaml 的交互逻辑
    /// </summary>
    public partial class FirstStartTeacher : Window
    {
        public FirstStartTeacher()
        {
            InitializeComponent();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void CloseTeacher(object sender, RoutedEventArgs e)
        {
            DoubleAnimation ani = PopEye.WPF.Animation.AnimationHelper.CreateAnimation(new TimeSpan(0, 0, 0, 0, 500), 1, 0, System.Windows.Media.Animation.FillBehavior.HoldEnd,
                PopEye.WPF.Animation.AnimationHelper.EasingFunction.Cubic, System.Windows.Media.Animation.EasingMode.EaseInOut, 0, 0);
            ani.Completed += (x, y) =>
            {
                Close();
            };
            BeginAnimation(OpacityProperty, ani);
        }

        private void LangS_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ((App)Application.Current).LoadLanguage((sender as ComboBox).SelectedIndex);
            Library.FileHelper.Config.WriteValue($"{App.WorkBase}\\App.config", "LastLang", $"{(sender as ComboBox).SelectedIndex}");
        }

        private void ChangeTheme(object sender, RoutedEventArgs e)
        {
            switch ((sender as RadioButton).Tag)
            {
                case "Light":
                    Library.FileHelper.Config.WriteValue($"{App.WorkBase}\\App.config", "LastTheme", "Light");
                    break;
                case "Dark":
                    Library.FileHelper.Config.WriteValue($"{App.WorkBase}\\App.config", "LastTheme", "Dark");
                    break;
            }
        }
    }
}
