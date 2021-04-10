using System.Windows;
using System.Windows.Controls;

namespace KitX.Controls
{
    /// <summary>
    /// ToolItem.xaml 的交互逻辑
    /// </summary>
    public partial class ToolItem : UserControl
    {
        public Core.IContract item;

        public ToolItem()
        {
            InitializeComponent();
        }

        public ToolItem(ref Core.IContract plugin)
        {
            InitializeComponent();
            item = plugin;
            item.SetWorkBase($"{App.ToolsBase}\\{item.GetPublisher()}\\{item.GetName()}");
        }

        public void Initialize()
        {
            Namer.Text = item.GetName();
            Publisher.Text = item.GetPublisher();
            Versioner.Text = item.GetVersion();
            Description.Text = item.GetDescribe_Simple();
            Header.Source = item.GetIcon();
        }

        public bool HasAdded = false;

        private System.Collections.Generic.List<Window> wins = new System.Collections.Generic.List<Window>();

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Window win = item.GetWindow();
            win.Show();
            wins.Add(win);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            foreach (Window win in wins)
            {
                win.Close();
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            //MainWindow win = (MainWindow)Application.Current.MainWindow;
            //win.RemoveTool(ref item);
        }
    }
}
