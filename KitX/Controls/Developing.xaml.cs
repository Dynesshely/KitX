using System.Windows;
using System.Windows.Controls;

namespace KitX.Controls
{
    /// <summary>
    /// Developing.xaml 的交互逻辑
    /// </summary>
    public partial class Developing : UserControl
    {
        public Developing()
        {
            InitializeComponent();
        }

        private void Trace(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.catrol.cn");
        }
    }
}
