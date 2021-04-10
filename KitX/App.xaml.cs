using HandyControl.Tools;
using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace KitX
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        #region 预先全局定义

        public static double WorkAreaWidth = SystemParameters.WorkArea.Width;//获取工作区宽度
        public static double WorkAreaHeight = SystemParameters.WorkArea.Height;//获取工作区高度
        public static double ScreenWidth = SystemParameters.PrimaryScreenWidth;//获取屏幕宽度
        public static double ScreenHeight = SystemParameters.PrimaryScreenHeight;//获取屏幕高度
        public static string WorkBase = Environment.CurrentDirectory;//获取工作路径
        public static string MainLogFile = Environment.CurrentDirectory + Library.FileHelper.Config.ReadValue($"{WorkBase}\\App.config", "LogPath");
        public static string ToolsPath = Converts.Config_ToolsPath(Library.FileHelper.Config.ReadValue($"{WorkBase}\\App.config", "ToolsLocation"));
        public static string ToolsBase = Converts.Config_ToolsPath(Library.FileHelper.Config.ReadValue($"{WorkBase}\\App.config", "ToolsWorkbase"));
        public static int AppsBarStartLocation = Convert.ToInt32(Library.FileHelper.Config.ReadValue($"{WorkBase}\\App.config", "AppsBarLocationOnTop"));
        public static string MyVersion = Library.FileHelper.Config.ReadValue($"{WorkBase}\\App.config", "Version");

        public static bool IsConvenientState = false;
        private bool firLang = true;
        public static int LangIndex = 0;

        readonly Lierda.WPFHelper.LierdaCracker cracker = new Lierda.WPFHelper.LierdaCracker();

        #endregion

        /// <summary>
        /// 保存变更的设定
        /// </summary>
        public static void SaveConfig()
        {

            IsConvenientState = ToolsPath == $"{WorkBase}\\Plug-ins\\";

            //设定配置
            Library.FileHelper.Config.WriteValue($"{WorkBase}\\App.config", "ToolsLocation", IsConvenientState ? "Self" : ToolsPath);
            Library.FileHelper.Config.WriteValue($"{WorkBase}\\App.config", "ToolsWorkbase", ToolsBase);
            Library.FileHelper.Config.WriteValue($"{WorkBase}\\App.config", "LogPath", MainLogFile.Substring(WorkBase.Length));
            Library.FileHelper.Config.WriteValue($"{WorkBase}\\App.config", "AppsBarLocationOnTop", $"{AppsBarStartLocation}");
            Library.FileHelper.Config.WriteValue($"{WorkBase}\\App.config", "LastLang", $"{LangIndex}");
        }

        /// <summary>
        /// 启动事件
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            WriteLineLog(MainLogFile, "App is running.");

            CheckApplicationMutex(); //检测应用是否运行了两个实例

            LoadLanguage(); //装载语言
            LoadLanguage(-1); //装载语言

            base.OnStartup(e);

            //// 配置 IoC 容器
            //IoC.Setup();

            #region 启动主窗体
            Current.MainWindow = new MainWindow
            {
                Width = Converts.Config_Size(Library.FileHelper.Config.ReadValue($"{WorkBase}\\App.config", "LastSize")).X,
                Height = Converts.Config_Size(Library.FileHelper.Config.ReadValue($"{WorkBase}\\App.config", "LastSize")).Y
            };
            if (Current.MainWindow.Width > WorkAreaWidth && WorkAreaWidth > Current.MainWindow.MinWidth)
            {
                Current.MainWindow.Width = WorkAreaWidth;
                Current.MainWindow.Left = 0;
            }
            if (Current.MainWindow.Height > WorkAreaHeight && WorkAreaHeight > Current.MainWindow.MinHeight)
            {
                Current.MainWindow.Height = WorkAreaHeight;
                Current.MainWindow.Top = 0;
            }
            Current.MainWindow.Left = Converts.Config_Location(Library.FileHelper.Config.ReadValue($"{WorkBase}\\App.config", "LastLocation"), Current.MainWindow.Width, Current.MainWindow.Height).X;
            Current.MainWindow.Top = Converts.Config_Location(Library.FileHelper.Config.ReadValue($"{WorkBase}\\App.config", "LastLocation"), Current.MainWindow.Width, Current.MainWindow.Height).Y;

            if (Convert.ToBoolean(Library.FileHelper.Config.ReadValue($"{App.WorkBase}\\App.config", "ShouldHideStart")))
            {
                Current.MainWindow.Show();
                Current.MainWindow.Close();
            }
            else
            {
                Current.MainWindow.Show();
            }

            Current.MainWindow.Resources["Version"] = MyVersion;
            #endregion

            cracker.Cracker(60);//垃圾回收间隔时间
        }

        /// <summary>
        /// 全局捕获并处理异常
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            HandyControl.Controls.Growl.Warning($"Error - {e.Exception.Message}");
            WriteLineLog(MainLogFile, $"UnhandledException: {e.ToString().Replace('\r', ';').Replace('\n', ';')}");
            e.Handled = true;
        }

        #region 延迟强制退出
        /// <summary>
        /// 用于解决托盘菜单引发 ShutdownApp 命令后进程异常不退出问题。
        /// </summary>
        public static void LazyClose() => new Thread(new ThreadStart(CloseEvent)).Start();

        /// <summary>
        /// 退出事件
        /// </summary>
        public static void CloseEvent()
        {
            Thread.Sleep(1000);
            SaveConfig();
            System.Diagnostics.Process.Start($"{WorkBase}\\kitX.Helper.Shutdown.vbs");
            Environment.Exit(0);
        }
        #endregion

        /// <summary>
        /// 标准应用日志输出
        /// </summary>
        /// <param name="path">日志路径</param>
        /// <param name="content">日志内容</param>
        /// <returns></returns>
        public static bool WriteLineLog(string path, string content) => Library.LogHelper.LogManager.SimpleWrite(path, $"{DateTime.UtcNow} - {content}", true);

        /// <summary>
        /// 常规输入框
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="content">内容</param>
        /// <returns>输入</returns>
        public static string Input_Normal(string title, string content) => Microsoft.VisualBasic.Interaction.InputBox(content, title, "input", -1, -1);

        /// <summary>
        /// 进程
        /// </summary>
        private Mutex mutex;

        /// <summary>
        /// 检查应用程序是否在进程中已经存在了
        /// </summary>
        private void CheckApplicationMutex()
        {

            // 第二个参数为 你的工程命名空间名。
            // out 给 ret 为 false 时，表示已有相同实例运行。
            Mutex = new Mutex(true, "KitX", out bool mutexResult);

            if (!mutexResult)
            {
                // 找到已经在运行的实例句柄(给出你的窗体标题名 “Deamon Club”)
                IntPtr hWndPtr = FindWindow(null, "KitX");

                // 还原窗口
                ShowWindow(hWndPtr, SW_RESTORE);

                // 激活窗口
                SetForegroundWindow(hWndPtr);

                // 退出当前实例程序
                Environment.Exit(0);
            }
        }

        #region Windows API

        // ShowWindow 参数  
        public const int SW_RESTORE = 9;

        public Mutex Mutex { get => Mutex1; set => Mutex1 = value; }
        public Mutex Mutex1 { get => mutex; set => mutex = value; }

        /// <summary>
        /// 在桌面窗口列表中寻找与指定条件相符的第一个窗口。
        /// </summary>
        /// <param name="lpClassName">指向指定窗口的类名。如果 lpClassName 是 NULL，所有类名匹配。</param>
        /// <param name="lpWindowName">指向指定窗口名称(窗口的标题）。如果 lpWindowName 是 NULL，所有windows命名匹配。</param>
        /// <returns>返回指定窗口句柄</returns>
        [DllImport("USER32.DLL", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        /// <summary>
        /// 将窗口还原,可从最小化还原
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="nCmdShow"></param>
        /// <returns></returns>
        [DllImport("USER32.DLL")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        /// <summary>
        /// 激活指定窗口
        /// </summary>
        /// <param name="hWnd">指定窗口句柄</param>
        /// <returns></returns>
        [DllImport("USER32.DLL")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        #endregion

        /// <summary>
        /// 装载默认语言
        /// </summary>
        private void LoadLanguage()
        {
            CultureInfo currentCultureInfo = CultureInfo.CurrentCulture;
            ResourceDictionary langRd = null;
            try
            {
                langRd = LoadComponent(new Uri(currentCultureInfo.Name + ".xaml", UriKind.Relative)) as ResourceDictionary;
            }
            catch
            {

            }
            if (langRd != null)
            {
                //if (Resources.MergedDictionaries.Count > 0)
                //{
                //    Resources.MergedDictionaries.Clear();
                //}
                Resources.MergedDictionaries.Add(langRd);
            }
        }

        /// <summary>
        /// 装载新语言
        /// </summary>
        public void LoadLanguage(int index)
        {
            if (firLang)
            {
                firLang = false;
            }
            else
            {
                string langName;
                LangIndex = index;
                switch (LangIndex)
                {
                    case -1:
                        langName = CultureInfo.CurrentCulture.Name;
                        break;
                    case 0:
                        langName = "zh-cn";
                        break;
                    case 1:
                        langName = "zh-cht";
                        break;
                    case 2:
                        langName = "en-us";
                        break;
                    case 3:
                        langName = "ja-jp";
                        break;
                    default:
                        langName = "zh-cn";
                        HandyControl.Controls.Growl.Info($"Your selected language wasn't surpported.");
                        break;
                }

                ResourceDictionary langRd = null;
                try
                {
                    //根据名字载入语言文件
                    langRd = LoadComponent(new Uri($"Lang\\{langName}.xaml", UriKind.Relative)) as ResourceDictionary;
                    langRd = new ResourceDictionary()
                    {
                        Source = new Uri($"Lang\\{langName}.xaml", UriKind.Relative)
                    };
                }
                catch (Exception e2)
                {
                    HandyControl.Controls.Growl.Error($"File {WorkBase}\\Lang\\{langName}.xaml didn't found.\r\n{e2.Message}");
                }

                if (langRd != null)
                {
                    ResourceDictionary rd = Current.Resources;
                    for (int i = 0; i < rd.MergedDictionaries.Count; i++)
                    {
                        ResourceDictionary tmp = null;
                        foreach (ResourceDictionary item in rd.MergedDictionaries)
                        {
                            if (item.Source.ToString().StartsWith("Lang"))
                            {
                                tmp = item;
                                break;
                            }
                        }
                        rd.MergedDictionaries.Remove(tmp);
                    }
                    rd.MergedDictionaries.Add(langRd);
                }
                else
                {
                    HandyControl.Controls.Growl.Warning($"Please selected one Language first.");
                }
            }
        }

        /// <summary>
        /// 更新皮肤
        /// </summary>
        /// <param name="st">皮肤类型</param>
        public void UpdateSkin(MainWindow.SkinType st)
        {
            ResourceDictionary rd = Current.Resources;
            rd.MergedDictionaries.Clear();
            switch (st)
            {
                case KitX.MainWindow.SkinType.Light: //亮色主题
                    Resources.MergedDictionaries.Add(ResourceHelper.GetSkin(HandyControl.Data.SkinType.Default));
                    Resources.MergedDictionaries.Add(new ResourceDictionary()
                    {
                        Source = new Uri("pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Light.xaml")
                    });
                    break;
                case KitX.MainWindow.SkinType.Dark: //暗色主题
                    Resources.MergedDictionaries.Add(ResourceHelper.GetSkin(HandyControl.Data.SkinType.Dark));
                    Resources.MergedDictionaries.Add(new ResourceDictionary()
                    {
                        Source = new Uri("pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Dark.xaml")
                    });
                    break;
            }
            Resources.MergedDictionaries.Add(new ResourceDictionary()
            {
                Source = new Uri("pack://application:,,,/HandyControl;component/Themes/Theme.xaml")
            });
            ReuploadResourceDictionary();
        }

        /// <summary>
        /// 装载其余xaml文件
        /// </summary>
        public void ReuploadResourceDictionary()
        {
            Resources.MergedDictionaries.Add(new ResourceDictionary()
            {
                Source = new Uri("PulseButton.xaml", UriKind.Relative)
            });
            Resources.MergedDictionaries.Add(new ResourceDictionary()
            {
                Source = new Uri("pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Defaults.xaml")
            });
            Resources.MergedDictionaries.Add(new ResourceDictionary()
            {
                Source = new Uri("pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.DeepPurple.xaml")
            });
            Resources.MergedDictionaries.Add(new ResourceDictionary()
            {
                Source = new Uri("pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Accent/MaterialDesignColor.Lime.xaml")
            });
            Resources.MergedDictionaries.Add(new ResourceDictionary()
            {
                Source = new Uri("pack://application:,,,/PopEye.WPF;component/Brushes/Brush.xaml")
            });
            LoadLanguage(LangIndex);
            Current.MainWindow?.OnApplyTemplate();
        }

        /// <summary>
        /// 更新语言脚标
        /// </summary>
        /// <param name="i"></param>
        public void UpdateLangIndex(int i) => LangIndex = i;
    }
}
