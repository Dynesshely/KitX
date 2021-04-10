using System.Windows.Controls;

namespace KitX.Controls
{
    /// <summary>
    /// Version.xaml 的交互逻辑
    /// </summary>
    public partial class Version : UserControl
    {
        public Version()
        {
            InitializeComponent();
            _v.Text = $"Version: {App.MyVersion}";
        }
    }
}
