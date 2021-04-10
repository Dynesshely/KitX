using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KitX.Controls
{
    /// <summary>
    /// ToolCard.xaml 的交互逻辑
    /// </summary>
    public partial class ToolCard : UserControl
    {
        public Button RefreshFather;
        public MainWindow FatherWin;

        public delegate void DownloadFinished();
        public event DownloadFinished DownloadFinish;

        public string pgID { get; set; }

        public ToolCard()
        {
            errInfos = new string[4][]
            {
                errInfo_cn, errInfo_cnt, errInfo_en, errInfo_jp
            };
            InitializeComponent();
            Loaded += (x, y) =>
            {
                DownloadFinish += () =>
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        string tag = RefreshFather.Tag.ToString();
                        RefreshFather.Tag = tag.Substring(0, tag.Length - 1);
                        if (RefreshFather.Tag.ToString().Length == 0)
                        {
                            RefreshFather.IsEnabled = true;
                        }
                        Btn_Download.IsEnabled = true;
                        DownloadingInfo.BeginAnimation(OpacityProperty, new PopEye.WPF.Animation.AnimationHelper().CreateDoubleAnimation(new TimeSpan(0, 0, 0, 0, 500), 1, 0,
                            System.Windows.Media.Animation.FillBehavior.HoldEnd, PopEye.WPF.Animation.AnimationHelper.EasingFunction.Cubic, System.Windows.Media.Animation.EasingMode.EaseOut, 0, 0));
                    }));
                };
            };
        }

        public string ToolName
        {
            get { return (string)GetValue(ToolNameProperty); }
            set { SetValue(ToolNameProperty, value); }
        }

        public static readonly DependencyProperty ToolNameProperty =
            DependencyProperty.Register("ToolName", typeof(string), typeof(ToolCard));

        public string SimpleDescribe
        {
            get { return (string)GetValue(SimpleDescribeProperty); }
            set { SetValue(SimpleDescribeProperty, value); }
        }

        public static readonly DependencyProperty SimpleDescribeProperty =
            DependencyProperty.Register("SimpleDescribe", typeof(string), typeof(ToolCard));

        public string ComplexDescribe
        {
            get { return (string)GetValue(ComplexDescribeProperty); }
            set { SetValue(ComplexDescribeProperty, value); }
        }

        public static readonly DependencyProperty ComplexDescribeProperty =
            DependencyProperty.Register("ComplexDescribe", typeof(string), typeof(ToolCard));

        private void Btn_Download_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            DownloadingInfo.BeginAnimation(OpacityProperty, new PopEye.WPF.Animation.AnimationHelper().CreateDoubleAnimation(new TimeSpan(0, 0, 0, 0, 500), 0, 1,
                System.Windows.Media.Animation.FillBehavior.HoldEnd, PopEye.WPF.Animation.AnimationHelper.EasingFunction.Cubic, System.Windows.Media.Animation.EasingMode.EaseOut, 0, 0));
            RefreshFather.IsEnabled = false;
            RefreshFather.Tag += "-";
            string toolName = ToolName;

            new Thread(() =>
            {
                var msc = new Helper.MySQLConnection();
                byte[] installerFile = msc.GetInstaller(pgID);
                if (installerFile != null)
                {
                    switch (msc.GetInstallerType(pgID))
                    {
                        case ".kxt":
                            string tarPath = $"{App.ToolsBase}\\Download\\";
                            if (!Directory.Exists(tarPath))
                            {
                                Directory.CreateDirectory(tarPath);
                            }
                            if (!File.Exists($"{tarPath}\\{toolName}.kxt"))
                            {
                                File.Create($"{tarPath}\\{toolName}.kxt");
                            }
                            installerFile = msc.GetInstaller(pgID);
                            Library.FileHelper.FileHelper.WriteByteIn($"{tarPath}\\{toolName}.kxt", installerFile);
                            Thread.Sleep(500);
                            Library.FileHelper.FileHelper.WriteByteIn($"{tarPath}\\{toolName}.kxt", installerFile);
                            KxtFileManager.InstallKxt($"{tarPath}\\{toolName}.kxt");
                            break;
                        case ".dll":
                            installerFile = msc.GetInstaller(pgID);
                            Library.FileHelper.FileHelper.WriteByteIn($"{App.ToolsPath}\\{toolName}.dll", installerFile);
                            Thread.Sleep(500);
                            Library.FileHelper.FileHelper.WriteByteIn($"{App.ToolsPath}\\{toolName}.dll", installerFile);
                            break;
                    }
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        HandyControl.Controls.Growl.Success(errInfos[FatherWin.LangIndex][0]);
                    }));
                }
                else
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        HandyControl.Controls.Growl.Warning(errInfos[FatherWin.LangIndex][1]);
                    }));
                }
                DownloadFinish();
                msc.Dispose();
            }).Start();
        }

        /// <summary>
        /// 登录信息 - 中文简体
        /// </summary>
        private readonly string[] errInfo_cn = new string[2]
        {
            "下载完毕", "下载失败"
        };

        /// <summary>
        /// 登录信息 - 中文繁体
        /// </summary>
        private readonly string[] errInfo_cnt = new string[2]
        {
            "下載完畢", "下載失敗"
        };

        /// <summary>
        /// 登录信息 - 英文
        /// </summary>
        private readonly string[] errInfo_en = new string[2]
        {
            "Download finished", "Download failed"
        };

        /// <summary>
        /// 登录信息 - 日语
        /// </summary>
        private readonly string[] errInfo_jp = new string[2]
        {
            "ダウンロード完了", "ダウンロードに失敗する"
        };

        private readonly string[][] errInfos;
    }
}
