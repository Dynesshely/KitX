using System;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Xml;

namespace KitX.Controls
{
    /// <summary>
    /// AddedLocalApps.xaml 的交互逻辑
    /// </summary>
    public partial class AddedLocalApps : UserControl
    {
        public ListBox list;

        public AddedLocalApps(XmlNode node)
        {
            InitializeComponent();
            NameBox.Text = node.Attributes["name"].InnerText;
            PathBox.Text = node.Attributes["path"].InnerText;
            ToolTip = PathBox.Text;
            string iconP = node.Attributes["icon"].InnerText;
            if (iconP.Equals("self"))
            {
                IconBox.Source = Library.Windows.GetAppIcon.GetIcon(PathBox.Text);
            }
            else if (File.Exists(iconP))
            {
                IconBox.Source = new BitmapImage(new Uri(iconP, UriKind.Absolute));
            }
            XmlCode.Text = $"<app name=\"{NameBox.Text}\" path=\"{PathBox.Text}\" icon=\"{iconP}\" argument=\"{node.Attributes["argument"].InnerText}\"/>";
        }

        public AddedLocalApps(string name, string path, string icon, string argument)
        {
            InitializeComponent();
            NameBox.Text = name;
            PathBox.Text = path;
            ToolTip = PathBox.Text;
            if (icon.Equals("self"))
            {
                IconBox.Source = Library.Windows.GetAppIcon.GetIcon(PathBox.Text);
            }
            else if (File.Exists(icon))
            {
                IconBox.Source = new BitmapImage(new Uri(icon, UriKind.Absolute));
            }
            XmlCode.Text = $"<app name=\"{NameBox.Text}\" path=\"{PathBox.Text}\" icon=\"{icon}\" argument=\"{argument}\"/>";
        }

        private void MenuItem_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            double h = ActualHeight;
            DoubleAnimation da = PopEye.WPF.Animation.AnimationHelper.CreateAnimation(new TimeSpan(0, 0, 0, 0, 500), h, 0, FillBehavior.HoldEnd,
                PopEye.WPF.Animation.AnimationHelper.EasingFunction.Cubic, EasingMode.EaseOut, 0, 0);
            da.Completed += (x, y) =>
            {
                list.Items.Remove(this);
            };
            BeginAnimation(HeightProperty, da);
        }
    }
}
