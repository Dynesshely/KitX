using KitX.Core;
using KitX.Controls;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;
using PointConverter = System.Windows.PointConverter;

namespace KitX
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 变量常量预定义
        /// </summary>
        public AppsBar_Controller abc = new AppsBar_Controller();//创建 工具栏控制类 实例
        public static string StartTheme = Library.FileHelper.Config.ReadValue($"{App.WorkBase}\\App.config", "LastTheme");//读取配置文件 启动主题
        private readonly bool ShouldShow_AppsBar = Convert.ToBoolean(Library.FileHelper.Config.ReadValue($"{App.WorkBase}\\App.config", "ShouldAppsBarOpen"));//读取配置文件 是否启动时显示工具栏
        private bool CanClose = false; //是否可以关闭
        private bool CanMovePage = true; //是否可以切换页面
        private bool CanMoveAccPages = true; //是否可以切换用户界面的页面
        public int LangIndex = 0; //语言组脚标
        readonly Helper.MySQLConnection msc = new Helper.MySQLConnection(); //数据库连接器
        private int nowIndex = 0; //工具市场加载下标
        private bool willRestart = false;
        //private bool firLang = true;

        /// <summary>
        /// 退出程序
        /// </summary>
        private void Quit()
        {
            CanClose = false;//标记工具栏窗体可以关闭
            Close();//关闭此窗体
            abc.Quit();//关闭工具栏
            msc.Dispose();//通知数据库连接器结束任务
            if (willRestart)
            {
                App.LazyRestart();
            }
            else
            {
                App.LazyClose();//延时结束整个程序
            }
        }

        /// <summary>
        /// 主窗体构造函数
        /// </summary>
        public MainWindow()
        {
            #region 第一次启动事件
            if (File.Exists($"{App.WorkBase}\\FirstRun.signal"))
            {
                System.Diagnostics.Process.Start($"{App.WorkBase}\\KitX.Helper.exe", "-f");

                App.WriteLineLog(App.MainLogFile, "Hello World!");
                App.WriteLineLog(App.MainLogFile, "It's my first running on your computer.");
                App.WriteLineLog(App.MainLogFile, "What's your name?");

                File.Delete($"{App.WorkBase}\\FirstRun.signal");
                new FirstStartTeacher().ShowDialog();
            }
            #endregion

            //设定语言组合
            signInfos = new string[4][]
            {
                signInfo_cn, signInfo_cnt, signInfo_en, signInfo_jp
            };
            signUpInfos = new string[4][]
            {
                signUpInfo_cn, signUpInfo_cnt, signUpInfo_en, signUpInfo_jp
            };

            //加载用户界面
            InitializeComponent();

            //设定用户名为系统登录用户名
            Resources["UserName"] = Environment.UserName;

            //启动加载完毕后事件
            Loaded += (x, y) =>
              {
                  //启动时加载插件
                  ReFresher_Click(null, null);
                  //启动时重置主题
                  if (StartTheme.Equals("Dark"))
                  {
                      ModifyTheme(theme => theme.SetBaseTheme(MaterialDesignThemes.Wpf.Theme.Dark));
                      (Template.FindName("ToggleButton_Theme", this) as ToggleButton).IsChecked = true;
                      abc.SetForeground(AppsBar_Controller.Fore.white);
                      ToastToolThemeChanged(Core.Theme.Dark);
                      ChangeSkin(SkinType.Dark);
                  }

                  //更新语言选择器
                  //LangIndex = ((App)Application.Current).LangIndex;
                  LangIndex = Convert.ToInt32(Library.FileHelper.Config.ReadValue($"{App.WorkBase}\\App.config", "LastLang"));
                  (Template.FindName("LangS", this) as ComboBox).SelectedIndex = LangIndex;

                  //刷新路径界面设定
                  RefreshPathSetting();

                  //更新工具栏控制按钮状态
                  //检查启动时是否显示工具栏
                  ToggleButton tb = Template.FindName("AppsBarManager", this) as ToggleButton;
                  if (ShouldShow_AppsBar)
                  {
                      AppsBarManager_Checked(tb, null);
                  }

                  //解决 ScrollViewer 内嵌 ListBox 鼠标滚动失效的问题
                  ListBox libox = (Template.FindName("ToolsList", this) as ListBox);
                  libox.PreviewMouseWheel += (sender, e) =>
                  {
                      var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                      {
                          RoutedEvent = UIElement.MouseWheelEvent,
                          Source = sender
                      };
                      libox.RaiseEvent(eventArg);
                  };

                  (Template.FindName("HideAtStart", this) as CheckBox).IsChecked = Convert.ToBoolean(Library.FileHelper.Config.ReadValue($"{App.WorkBase}\\App.config", "ShouldHideStart"));
              };

            //设定窗体键盘事件
            KeyDown += MainWindow_KeyDown;
        }

        /// <summary>
        /// 键盘事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyStates == e.KeyboardDevice.GetKeyStates(Key.P) && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                Command.NormalCommand(App.Input_Normal("输入调试命令", "调试"), this);
            }
            var facer = (Template.FindName("Facer", this) as MaterialDesignThemes.Wpf.Transitions.Transitioner);
            if (CanMovePage)
            {
                if (e.KeyStates == e.KeyboardDevice.GetKeyStates(Key.W) && facer.SelectedIndex != 0)
                    facer.SelectedIndex--;
                if (e.KeyStates == e.KeyboardDevice.GetKeyStates(Key.S) && facer.SelectedIndex != facer.Items.Count)
                    facer.SelectedIndex++;

                var cog = (Template.FindName("Cog_Pages", this) as MaterialDesignThemes.Wpf.Transitions.Transitioner);
                if (e.KeyStates == e.KeyboardDevice.GetKeyStates(Key.A) && cog.SelectedIndex != 0)
                    cog.SelectedIndex--;
                if (e.KeyStates == e.KeyboardDevice.GetKeyStates(Key.D) && cog.SelectedIndex != cog.Items.Count)
                    cog.SelectedIndex++;

                var lib = (Template.FindName("LibViewer", this) as MaterialDesignThemes.Wpf.Transitions.Transitioner);
                if (e.KeyStates == e.KeyboardDevice.GetKeyStates(Key.Q) && lib.SelectedIndex != 0)
                    lib.SelectedIndex--;
                if (e.KeyStates == e.KeyboardDevice.GetKeyStates(Key.E) && lib.SelectedIndex != lib.Items.Count)
                    lib.SelectedIndex++;
            }
        }

        /// <summary>
        /// 取消窗体关闭事件
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosing(CancelEventArgs e)
        {
            if (CanClose)
            {
                e.Cancel = false;
            }
            else
            {
                e.Cancel = true;
                Hide();
                SaveLocationConfig();
            }
        }

        /// <summary>
        /// 保存窗体位置配置信息
        /// </summary>
        private void SaveLocationConfig()
        {
            Library.FileHelper.Config.WriteValue($"{App.WorkBase}\\App.config", "LastSize", $"{ActualWidth},{ActualHeight}"); //储存窗体大小
            Library.FileHelper.Config.WriteValue($"{App.WorkBase}\\App.config", "LastLocation", $"{Left},{Top}"); //储存窗体位置
        }

        /// <summary>
        /// 窗体移动代码
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        /// <summary>
        /// 切换主题
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ToggleButton_Theme_Click(object sender, RoutedEventArgs e)
        {
            await Dispatcher.BeginInvoke(new Action(delegate
            {
                //ModifyTheme(theme => theme.SetBaseTheme((bool)(sender as ToggleButton).IsChecked ? MaterialDesignThemes.Wpf.Theme.Dark : MaterialDesignThemes.Wpf.Theme.Light));
                if (StartTheme.Equals("Dark"))
                {
                    StartTheme = "Light";
                    abc.SetForeground(AppsBar_Controller.Fore.black);
                    ToastToolThemeChanged(Core.Theme.Light);
                    ChangeSkin(SkinType.Light);
                }
                else
                {
                    StartTheme = "Dark";
                    abc.SetForeground(AppsBar_Controller.Fore.white);
                    ToastToolThemeChanged(Core.Theme.Dark);
                    ChangeSkin(SkinType.Dark);
                }
                Library.FileHelper.Config.WriteValue($"{App.WorkBase}\\App.config", "LastTheme", StartTheme);
                abc.RefreshTheme();
            }));
        }

        /// <summary>
        /// 通知插件切换主题
        /// </summary>
        /// <param name="theme"></param>
        private void ToastToolThemeChanged(Core.Theme theme)
        {
            foreach (var item in finds)
            {
                item.SetTheme(theme);
            }
            abc.RefreshToolTheme(theme);
        }

        /// <summary>
        /// MD的主题切换
        /// </summary>
        /// <param name="modificationAction"></param>
        private static void ModifyTheme(Action<ITheme> modificationAction)
        {
            PaletteHelper paletteHelper = new PaletteHelper();
            ITheme theme = paletteHelper.GetTheme();
            modificationAction?.Invoke(theme);
            paletteHelper.SetTheme(theme);
        }

        /// <summary>
        /// 语言改变事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LangS_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LangIndex = (sender as ComboBox).SelectedIndex;
            ((App)Application.Current).UpdateLangIndex(LangIndex);
            ((App)Application.Current).LoadLanguage(LangIndex);

            #region 元片段
            //string langName;
            //switch (LangIndex)
            //{
            //    case 0:
            //        langName = "zh-cn";
            //        break;
            //    case 1:
            //        langName = "zh-cht";
            //        break;
            //    case 2:
            //        langName = "en-us";
            //        break;
            //    case 3:
            //        langName = "ja-jp";
            //        break;
            //    default:
            //        langName = "zh-cn";
            //        (sender as ComboBox).SelectedIndex = 0;
            //        HandyControl.Controls.Growl.Info($"Your selected language wasn't surpported.");
            //        break;
            //}

            //ResourceDictionary langRd = null;
            //try
            //{
            //    //根据名字载入语言文件
            //    langRd = Application.LoadComponent(new Uri($"Lang\\{langName}.xaml", UriKind.Relative)) as ResourceDictionary;
            //}
            //catch (Exception e2)
            //{
            //    HandyControl.Controls.Growl.Error($"File {App.WorkBase}\\Lang\\{langName}.xaml didn't found.\r\n{e2.Message}");
            //}

            //if (langRd != null)
            //{
            //    ResourceDictionary rd = Application.Current.Resources;
            //    if (!firLang)
            //    {
            //        ResourceDictionary tmp = null;
            //        foreach (ResourceDictionary item in rd.MergedDictionaries)
            //        {
            //            if (item.Source.ToString().StartsWith("Lang"))
            //            {
            //                tmp = item;
            //            }
            //        }
            //        rd.MergedDictionaries.Remove(tmp);
            //    }
            //    else
            //    {
            //        firLang = true;
            //    }
            //    rd.MergedDictionaries.Add(langRd);
            //}
            //else
            //{
            //    HandyControl.Controls.Growl.Warning($"Please selected one Language first.");
            //} 
            #endregion
        }

        //已发现的插件 List
        public List<IContract> finds = new List<IContract>();
        //插件装载容器
        private CompositionContainer container = null;

        /// <summary>
        /// 寻找并发现插件
        /// </summary>
        public void Tools_Refind()
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate
            {
                if (Directory.Exists(App.ToolsPath))
                {
                    var catalog = new DirectoryCatalog(App.ToolsPath, "*.dll");
                    container = new CompositionContainer(catalog);
                    IEnumerable<IContract> sub = container.GetExportedValues<IContract>();
                    foreach (var item in sub)
                    {
                        finds.Add(item);
                        item.SetWorkBase($"{App.ToolsBase}\\{item.GetPublisher()}\\{item.GetName()}");
                    }

                    foreach (IContract item in finds)
                    {
                        _ = Task.Run(() => AddTool(item));
                    }
                }
            }));
        }

        /// <summary>
        /// 多线程更新工具
        /// </summary>
        private void AddToolThread()
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate
            {
                ListBox lister = Template.FindName("ToolsList", this) as ListBox;
                lister.Items.Clear();
            }));
            finds.Clear();

            if (container != null)
            {
                container.Dispose();
            }

            Tools_Refind();
        }

        /// <summary>
        /// 刷新工具按钮单击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void ReFresher_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(AddToolThread);

            #region 原先发现工具代码块
            //if (!Directory.Exists(App.ToolsPath))
            //{
            //    Directory.CreateDirectory(App.ToolsPath);
            //}
            //var catalog = new DirectoryCatalog(App.ToolsPath, "*.dll");
            //if (catalog != null)
            //{
            //    using (CompositionContainer container = new CompositionContainer(catalog))
            //    {
            //        IEnumerable<IContract> sub = container.GetExportedValues<IContract>();
            //        foreach (var item in sub)
            //        {
            //            finds.Add(item);
            //        }
            //    }
            //}
            //catalog.Dispose();

            //foreach (var item in finds)
            //{
            //    _ = Task.Run(() => AddTool(item));
            //}
            #endregion
        }

        /// <summary>
        /// 异步添加工具条
        /// </summary>
        /// <param name="item">接口传参</param>
        private void AddTool(IContract item)
        {
            ListBox lister = Template.FindName("ToolsList", this) as ListBox;
            Dispatcher.BeginInvoke(new Action(delegate
            {
                ToolItem ti = CreateItem(item);
                ListBoxItem lb = new ListBoxItem() { Content = ti };
                lb.Selected += (x, y) =>
                {
                    Resources["ToolName"] = item.GetName();
                    Resources["Publisher"] = item.GetPublisher();
                    Resources["InstalledVersion"] = item.GetVersion();
                    Resources["Discription"] = item.GetDescribe_Complex();
                    Resources["HelpLink"] = item.GetHelpLink();
                    Resources["HostLink"] = item.GetHostLink();
                    Resources["Icon"] = item.GetIcon();
                    Resources["LangProvided"] = item.GetLang();
                    SetTager(item.GetTag());
                    ChangePage_Lib(1);
                };
                lister.Dispatcher.BeginInvoke(new Action(() => lister.Items.Add(lb)));
            }));
        }

        /// <summary>
        /// 选项分类动态资源设置
        /// </summary>
        /// <param name="tag"></param>
        private void SetTager(Tags tag)
        {
            string sourceName = null;
            switch (tag)
            {
                case Tags.Process:
                    sourceName = "Process";
                    break;
                case Tags.Program:
                    sourceName = "Program";
                    break;
                case Tags.Normal:
                    sourceName = "Normal";
                    break;
                case Tags.Design:
                    sourceName = "Design";
                    break;
                case Tags.System:
                    sourceName = "System";
                    break;
            }
            (Template.FindName("Tager", this) as HandyControl.Controls.Shield).SetResourceReference(HandyControl.Controls.Shield.StatusProperty, $"Left_Sort_{sourceName}");
        }

        /// <summary>
        /// 创建工具条
        /// </summary>
        /// <param name="item">接口传参</param>
        /// <returns></returns>
        private ToolItem CreateItem(IContract item)
        {
            ToolItem ti = new ToolItem(ref item);
            ti.Initialize();
            string binName = $"{item.GetPublisher()}_{item.GetName().Replace(' ', '_')}";
            ti.Adder.Click += (x, y) =>
            {
                if (ti.HasAdded)
                {
                    abc.RemoveTool(binName);
                    ti.HasAdded = false;
                }
                else
                {
                    abc.AddTool(item, item.GetIcon(), binName);
                    ti.HasAdded = true;
                }
            };
            if (ti.HasAdded)
            {
                abc.RemoveTool(binName);
                ti.HasAdded = false;
            }
            else
            {
                abc.AddTool(item, item.GetIcon(), binName);
                ti.HasAdded = true;
            }
            item.SetWorkBase($"{App.ToolsBase}\\{item.GetPublisher()}\\{item.GetName()}");
            ti.HasAdded = true;
            return ti;
        }

        /// <summary>
        /// 添加工具按钮事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Adder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog()
                {
                    Filter = "通用插件|*.dll|专用插件|*.kxt", //kxt - KitX Tool 的缩写
                    Multiselect = true,
                    CheckFileExists = true,
                    InitialDirectory = App.WorkBase,
                    Title = "选取要添加的工具 - Select one or more tools which are your wants",
                    DereferenceLinks = true
                };
                ofd.ShowDialog();
                if (ofd.FileNames != null)
                {
                    foreach (string fiPath in ofd.FileNames)
                    {
                        AddPG(fiPath);
                    }
                }
                ofd.Reset();
                ReFresher_Click(null, null);
            }
            catch (Exception f)
            {
                App.WriteLineLog(App.MainLogFile, f.Message);
            }
        }

        /// <summary>
        /// 复制插件到插件目录
        /// </summary>
        /// <param name="path">插件路径</param>
        private void AddPG(string path)
        {
            if (Library.TextHelper.Text.ToCapital(Path.GetExtension(path)).Equals(".DLL"))
            {
                string tp = $"{App.ToolsPath}\\{System.IO.Path.GetFileName(path)}";
                if (!File.Exists(tp))
                {
                    File.Move(path, tp);
                    File.Copy(tp, path);
                }
                else
                {
                    string ntp = $"{App.ToolsPath}\\{System.IO.Path.GetFileNameWithoutExtension(path)}{new Random().Next(0, 300)}{System.IO.Path.GetExtension(path)}";
                    if (!File.Exists(ntp))
                    {
                        File.Copy(path, ntp);
                    }
                }
            }
            else if (Library.TextHelper.Text.ToCapital(Path.GetExtension(path)).Equals(".KXT"))
            {
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    KxtFileManager.InstallKxt(path);
                }));
            }
        }

        /// <summary>
        /// 显示工具栏
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AppsBarManager_Checked(object sender, RoutedEventArgs e)
        {
            (sender as ToggleButton).IsChecked = true;
            abc.Show();
            abc.SelectLocation(App.AppsBarStartLocation);
            Library.FileHelper.Config.WriteValue($"{App.WorkBase}\\App.config", "ShouldAppsBarOpen", "true");
        }

        /// <summary>
        /// 不显示工具栏
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AppsBarManager_Unchecked(object sender, RoutedEventArgs e)
        {
            (sender as ToggleButton).IsChecked = false;
            abc.Hide();
            Library.FileHelper.Config.WriteValue($"{App.WorkBase}\\App.config", "ShouldAppsBarOpen", "false");
        }

        /// <summary>
        /// 关闭工具栏
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseAppsbar(object sender, RoutedEventArgs e) => abc.Close();

        /// <summary>
        /// 退出应用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExitApp(object sender, RoutedEventArgs e)
        {
            Library.FileHelper.Config.WriteValue($"{App.WorkBase}\\App.config", "LastSize", $"{ActualWidth},{ActualHeight}");
            Library.FileHelper.Config.WriteValue($"{App.WorkBase}\\App.config", "LastLocation", $"{Left},{Top}");
            Quit();
        }

        /// <summary>
        /// 退出应用2
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExitApp2(object sender, RoutedEventArgs e)
        {
            Library.FileHelper.Config.WriteValue($"{App.WorkBase}\\App.config", "LastSize", $"{ActualWidth},{ActualHeight}");
            Library.FileHelper.Config.WriteValue($"{App.WorkBase}\\App.config", "LastLocation", $"{Left},{Top}");
            new Thread(() =>
            {
                Thread.Sleep(500);
                Dispatcher.Invoke(() =>
                {
                    Quit();
                });
            }).Start();
        }

        /// <summary>
        /// 置顶窗口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KeepToper_Checked(object sender, RoutedEventArgs e) => Topmost = true;

        /// <summary>
        /// 取消置顶
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KeepToper_Unchecked(object sender, RoutedEventArgs e) => Topmost = false;

        /// <summary>
        /// 重启应用事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReStart(object sender, RoutedEventArgs e)
        {
            willRestart = true;
            Quit();
            //Helper.Program.RestartMainDomain(App.WorkBase);
        }

        /// <summary>
        /// 跳至 库
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void JumpTo_Lib(object sender, RoutedEventArgs e) => ChangePage_Main(0);

        /// <summary>
        /// 跳至 市场
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void JumpTo_Shop(object sender, RoutedEventArgs e) => ChangePage_Main(1);

        /// <summary>
        /// 跳至 任务计划
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void JumpTo_Task(object sender, RoutedEventArgs e) => ChangePage_Main(2);

        /// <summary>
        /// 跳至 用户
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void JumpTo_User(object sender, RoutedEventArgs e) => ChangePage_Main(3);

        /// <summary>
        /// 跳至 设置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void JumpTo_Cog(object sender, RoutedEventArgs e) => ChangePage_Main(4);

        /// <summary>
        /// 跳至 主页
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Go_Homepage(object sender, RoutedEventArgs e) => ChangePage_Lib(0);

        /// <summary>
        /// 改变页面Index
        /// </summary>
        /// <param name="index">index</param>
        public void ChangePage_Main(int index) => (Template.FindName("Facer", this) as MaterialDesignThemes.Wpf.Transitions.Transitioner).SelectedIndex = index;

        /// <summary>
        /// 改变页面Index
        /// </summary>
        /// <param name="index">index</param>
        public void ChangePage_Lib(int index) => (Template.FindName("LibViewer", this) as MaterialDesignThemes.Wpf.Transitions.Transitioner).SelectedIndex = index;

        /// <summary>
        /// 设置页面切换
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CogPageSelect(object sender, RoutedEventArgs e) => (Template.FindName("Cog_Pages", this) as MaterialDesignThemes.Wpf.Transitions.Transitioner).SelectedIndex = Convert.ToInt32((sender as MenuItem).Tag);

        /// <summary>
        /// 跳转到关于
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToAbout(object sender, RoutedEventArgs e)
        {
            Point p = (Point)new PointConverter().ConvertFromString((sender as MenuItem).Tag.ToString());
            ChangePage_Main((int)p.X);
            (Template.FindName("Cog_Pages", this) as MaterialDesignThemes.Wpf.Transitions.Transitioner).SelectedIndex = (int)p.Y;
        }

        /// <summary>
        /// 统一跳转
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Normal_Jump(object sender, RoutedEventArgs e)
        {
            Point p = (Point)new PointConverter().ConvertFromString((sender as FrameworkElement).Tag.ToString());
            ChangePage_Main((int)p.X);
            switch ((int)p.X)
            {
                case 0:
                    (Template.FindName("LibViewer", this) as MaterialDesignThemes.Wpf.Transitions.Transitioner).SelectedIndex = (int)p.Y;
                    break;
                case 3:
                    if (CanMoveAccPages)
                    {
                        (Template.FindName("Acc_Pages", this) as MaterialDesignThemes.Wpf.Transitions.Transitioner).SelectedIndex = (int)p.Y;
                    }
                    break;
                case 4:
                    (Template.FindName("Cog_Pages", this) as MaterialDesignThemes.Wpf.Transitions.Transitioner).SelectedIndex = (int)p.Y;
                    break;
            }
        }

        /// <summary>
        /// 图标保存到本地
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveIcon(object sender, RoutedEventArgs e) => Dev_ShowInfo();

        /// <summary>
        /// 显示开发中信息
        /// </summary>
        private void Dev_ShowInfo() => HandyControl.Controls.Growl.Info($"This function is developing.");

        /// <summary>
        /// 读取第三方通知文本并显示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReadInThirdParty(object sender, RoutedEventArgs e) => (Template.FindName("ThirdPartyNotificationBox", this) as TextBox).Text = File.Exists($"{App.WorkBase}\\Source\\ThirdParty.txt") ? Library.FileHelper.FileHelper.ReadAll($"{App.WorkBase}\\Source\\ThirdParty.txt") : "Undefind... (Please press \"Load\" button first.)";

        /// <summary>
        /// 全屏幕阅读
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FullScreenReading(object sender, RoutedEventArgs e)
        {
            TextBox tb = new TextBox()
            {
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(10),
                Background = new SolidColorBrush(Colors.Transparent),
                Text = File.Exists($"{App.WorkBase}\\Source\\ThirdParty.txt") ? Library.FileHelper.FileHelper.ReadAll($"{App.WorkBase}\\Source\\ThirdParty.txt") : "Undefind... (Please press \"Load\" button first.)"
            };
            ScrollViewer sv = new ScrollViewer()
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Background = new SolidColorBrush(Colors.Transparent),
                Content = tb
            };
            HandyControl.Controls.BlurWindow fsr = new HandyControl.Controls.BlurWindow()
            {
                Content = sv,
                Title = "Full-screen Reading Window，Press \"Alt + F4\" to Close.",
                WindowState = WindowState.Maximized,
                Icon = Icon
            };
            fsr.Show();
        }

        /// <summary>
        /// 打开链接
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenLink(object sender, RoutedEventArgs e)
        {
            string link = (sender as FrameworkElement).Tag.ToString();
            if (link.Contains("http://") && link.Contains("."))
            {
                System.Diagnostics.Process.Start(link);
                return;
            }
            else if (link.StartsWith("path|"))
            {
                System.Diagnostics.Process.Start(link.Split('|')[1]);
            }
            else if (link.StartsWith("config"))
            {
                ConfigOperate(link.Split(':')[1]);
            }
            else
            {
                switch (link)
                {
                    case "uri:logFile":
                        System.Diagnostics.Process.Start(App.MainLogFile);
                        break;
                    case "uri:developing":
                        Dev_ShowInfo();
                        break;
                    case "uri:signin":
                        SignIn();
                        break;
                    case "uri:signup":
                        SignUp();
                        break;
                    case "uri:signout":
                        CanMoveAccPages = true;
                        Normal_Jump(new FrameworkElement()
                        {
                            Tag = "3,0"
                        }, null);
                        Resources["UserName"] = Environment.UserName;
                        Resources["UserSigned"] = Visibility.Hidden;
                        Resources["YUserIcon"] = Visibility.Visible;
                        break;
                    case "uri:uploadicon":
                        new Thread(() =>
                        {
                            Dictionary<string, string> dictionaries = new Dictionary<string, string>
                            {
                                { "PNG Image", "*.png" }
                            };
                            string path = Library.FileHelper.FileWin.OpenFile_Single(dictionaries, signUpInfos[LangIndex][5]);
                            if (path != null && File.Exists(path))
                            {
                                if (!msc.UploadIcon(Library.FileHelper.FileHelper.ReadByteAll(path), ID))
                                {
                                    Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        HandyControl.Controls.Growl.Error(signUpInfos[LangIndex][6]);
                                    }));
                                }
                            }
                        }).Start();
                        break;
                    case "uri:saveinfo":
                        string[][] ret = GetInfoDict();
                        new Thread(() =>
                        {
                            if (!msc.UploadInfo(ID, ret[0], ret[1]))
                            {
                                Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    HandyControl.Controls.Growl.Error(signUpInfos[LangIndex][8]);
                                }));
                            }
                        }).Start();
                        break;
                }
            }
        }

        /// <summary>
        /// 刷新市场页面按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshMarket(object sender, RoutedEventArgs e) => RefreshMarket();

        /// <summary>
        /// 刷新市场页面
        /// </summary>
        private void RefreshMarket()
        {
            WrapPanel wp = Template.FindName("Market", this) as WrapPanel;
            wp.Children.Clear();
            new Thread(() =>
            {
                for (int i = 0; i < 5; i++)
                {
                    if (AddMarketToolName(Library.MathHelper.Basic.CoverPosition(i + 1, 18, '0')))
                    {
                        nowIndex = i + 1;
                    }
                }
                AddAddButton();
            }).Start();
        }

        /// <summary>
        /// 添加追加按钮
        /// </summary>
        private void AddAddButton()
        {
            WrapPanel wp = Template.FindName("Market", this) as WrapPanel;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Button btn = new Button()
                {
                    Content = "+ 10"
                };
                btn.Click += (x, y) =>
                {
                    wp.Children.Remove(btn);
                    new Thread(() =>
                    {
                        for (int j = nowIndex; j < 10; j++)
                        {
                            if (AddMarketToolName(Library.MathHelper.Basic.CoverPosition(j + 1, 18, '0')))
                            {
                                nowIndex = j + 1;
                            }
                        }
                        AddAddButton();
                    }).Start();
                };
                wp.Children.Add(btn);
            }));
        }

        /// <summary>
        /// 向市场中添加工具 UI 项
        /// </summary>
        /// <param name="pgid">工具 ID</param>
        private bool AddMarketToolName(string pgid)
        {
            WrapPanel wp = Template.FindName("Market", this) as WrapPanel;
            var connect = msc;
            string[] infos = connect.GetToolItem(pgid);
            if (infos != null)
            {
                byte[] head = connect.GetToolIcon(pgid);
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ToolCard tc = new ToolCard()
                    {
                        RefreshFather = Template.FindName("Btn_Market_Flush", this) as Button,
                        ToolName = infos[1],
                        SimpleDescribe = infos[3],
                        ComplexDescribe = infos[4],
                        pgID = pgid,
                        FatherWin = this
                    };
                    tc.img.Source = Library.BitmapImageHelper.Converter.ByteArray2BitmapImage(head);
                    wp.Children.Add(tc);
                }));
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 处理对配置文件的操作
        /// </summary>
        /// <param name="cmd"></param>
        private void ConfigOperate(string cmd)
        {
            switch (cmd)
            {
                case "download":
                    new Thread(() =>
                    {
                        byte[] config = msc.GetConfig(ID);
                        if (config != null)
                        {
                            Library.FileHelper.FileHelper.WriteByteIn($"{App.WorkBase}\\App.config", msc.GetConfig(ID));
                            //Library.FileHelper.FileHelper.WriteIn($"{App.WorkBase}\\KitX.exe.config", Library.FileHelper.FileHelper.ReadAll($"{App.WorkBase}\\App.config"));
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                HandyControl.Controls.Growl.Success(signUpInfos[LangIndex][9]);
                            }));
                        }
                        else
                        {
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                HandyControl.Controls.Growl.Warning(signUpInfos[LangIndex][11]);
                            }));
                        }
                    }).Start();
                    break;
                case "upload":
                    new Thread(() =>
                    {
                        if (msc.UploadConfig(Library.FileHelper.FileHelper.ReadByteAll($"{App.WorkBase}\\App.config"), ID))
                        {
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                HandyControl.Controls.Growl.Success(signUpInfos[LangIndex][10]);
                            }));
                        }
                        else
                        {
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                HandyControl.Controls.Growl.Warning(signUpInfos[LangIndex][12]);
                            }));
                        }
                    }).Start();
                    break;
            }
        }

        /// <summary>
        /// 获取当前用户信息表单列表
        /// </summary>
        /// <returns></returns>
        private string[][] GetInfoDict()
        {
            string[] keys = new string[9]
            {
                "username","usermail","usersex","usersign","userbirthday","usereducationbackground","userlocation","userjob","userdescribe",
            };
            string[] values = new string[9]
            {
                GetText("UserNameBox"), GetText("UserMailBox"), (Template.FindName("UserSexBox", this) as ComboBox).SelectedIndex.ToString(),
                GetText("UserSignBox"), ((DateTime)(Template.FindName("UserBirthBox", this) as DatePicker).SelectedDate).ToString("yyyy-MM-dd"),
                GetText("UserEBBox"), GetText("UserLocBox"), GetText("UserJobBox"), GetText("UserDcbBox")
            };
            return new string[2][]
            {
                keys, values
            };
        }

        /// <summary>
        /// 获取用户界面TextBox的Text属性值
        /// </summary>
        /// <param name="name">TextBox的控件名</param>
        /// <returns>属性值</returns>
        private string GetText(string name) => (Template.FindName(name, this) as TextBox).Text;

        /// <summary>
        /// 设置用户界面TextBox的Text属性值
        /// </summary>
        /// <param name="name">TextBox的控件名</param>
        /// <param name="text">要设置的属性值</param>
        /// <returns>属性值</returns>
        private void SetText(string name, string text) => (Template.FindName(name, this) as TextBox).Text = text;

        private string ID; //用户ID
        private string pwd; //用户密码

        /// <summary>
        /// 登录事件
        /// </summary>
        private void SignIn()
        {
            ID = (Template.FindName("SignIn_ID_Box", this) as TextBox).Text;
            pwd = (Template.FindName("SignIn_Pwd_Box", this) as PasswordBox).Password;
            new Thread(CheckSignInfo).Start();
        }

        /// <summary>
        /// 登录信息 - 中文简体
        /// </summary>
        private readonly string[] signInfo_cn = new string[8]
        {
            "登录成功", "登陆失败", "用户标识符或密码不正确", "该账户不存在", "账户标识符或密码不能为空", "不符合登录标识符格式", "该账号已经被封停", "最近可登录时间"
        };

        /// <summary>
        /// 登录信息 - 中文繁体
        /// </summary>
        private readonly string[] signInfo_cnt = new string[8]
        {
            "登錄成功", "登陸失敗", "用戶標識符或密碼不正確", "該賬戶不存在", "賬戶標識符或密碼不能為空", "不符合登錄標識符格式", "該賬號已經被封停", "最近可登錄時間"
        };

        /// <summary>
        /// 登录信息 - 英文
        /// </summary>
        private readonly string[] signInfo_en = new string[8]
        {
            "Sign in succeeded", "Sign in failed", "ID or password is wrong", "This account doesn't exist", "ID or password can't be null", "It doesn't match the format", "This account is forbiddened", "The next time you can sign"
        };

        /// <summary>
        /// 登录信息 - 日语
        /// </summary>
        private readonly string[] signInfo_jp = new string[8]
        {
            "ログインに成功する", "上陸に失敗する", "ユーザ識別子またはパスワードが正しくありません", "この口座は存在しません", "アカウントの識別子やパスワードは空欄にしてはいけません",
            "ログイン識別子フォーマットに準拠していない", "このアカウントは既に停止されている", "直近のログイン可能時間"
        };

        private readonly string[][] signInfos;

        /// <summary>
        /// 注册提示 - 中文简体
        /// </summary>
        private readonly string[] signUpInfo_cn = new string[15]
        {
            "ID不符合身份证格式（要求 11 位）", "两次密码不一致", "请检查表单，无法为您完成注册", "注册失败", "该账户已经存在", "选择用户头像", "上传失败", "注册成功", "保存信息失败", "下载成功，请重启 KitX",
            "上传成功", "下载失败", "上传失败", "您的 KitX 已经是最新版了", "有新的版本待您更新"
        };

        /// <summary>
        /// 注册提示 - 中文繁体
        /// </summary>
        private readonly string[] signUpInfo_cnt = new string[15]
        {
            "ID不符合身份證格式（要求 11 位）", "兩次密碼不壹致", "請檢查表單，無法為您完成註冊", "註冊失敗", "該賬戶已經存在", "選擇用戶頭像", "上傳失敗", "註冊成功", "保存信息失敗", "下載成功，請重啟 KitX",
            "上傳成功", "下載失敗", "上傳失敗", "您的 KitX 已經是最新版了", "有新的版本待您更新"
        };

        /// <summary>
        /// 注册提示 - 英文
        /// </summary>
        private readonly string[] signUpInfo_en = new string[15]
        {
            "ID does not match ID format（For 11 Length）", "The two passwords don't match", "Please check the form, the registration cannot be completed for you",
            "Sign up failed", "The account already exists", "Select the user avatar.", "Uploading failed", "Sign Up Succeed", "Save info failed",
            "Download config file succeeded, please restart the KitX App.", "Upload succeeded.", "Download failed", "Upload failed", "Your KitX is already the latest version.",
            "There is a new version for you to update."
        };

        /// <summary>
        /// 注册提示 - 日语
        /// </summary>
        private readonly string[] signUpInfo_jp = new string[15]
        {
            "idが身分証明書の形式に合っていません（要求 11 位）", "2度パスワードが一致しません", "フォームをチェックしてください。登録が完了しません", "登録に失敗する", "この口座は既に存在します",
            "ユーザーのプロフィール画像を選択する", "アップロード失敗", "登録が成功する", "情報保存に失敗する", "ダウンロード成功、KitX を再起動してください", "アップロード成功", "ダウンロードに失敗する",
            "アップロード失敗", "あなたのkitxはもう最新版です", "新しいバージョンがあります"
        };

        private readonly string[][] signUpInfos;

        /// <summary>
        /// 更新用户界面用户信息
        /// </summary>
        private void UpdateUserUIInfo()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                SetText("UserNameBox", msc.GetName(ID));
                Resources["UserName"] = msc.GetName(ID);
                Resources["UserMail"] = msc.GetMail(ID);
                SetText("UserMailBox", msc.GetMail(ID));
                //SetText("UserPhoneBox", msc.GetPhone(ID));
                (Template.FindName("UserSexBox", this) as ComboBox).SelectedIndex = msc.GetSex(ID);
                SetText("UserSignBox", msc.GetSign(ID));
                (Template.FindName("UserBirthBox", this) as DatePicker).SelectedDate = msc.GetBirth(ID);
                SetText("UserLocBox", msc.GetLoc(ID));
                SetText("UserJobBox", msc.GetJob(ID));
                SetText("UserDcbBox", msc.GetDescribe(ID));
                SetText("UserEBBox", msc.GetEB(ID));
                Resources["UserSigned"] = Visibility.Visible;
                Resources["YUserIcon"] = Visibility.Hidden;
                Resources["UserHeader"] = Library.BitmapImageHelper.Converter.ByteArray2BitmapImage(msc.GetIcon(ID));
            }));
        }

        /// <summary>
        /// 检查登录信息
        /// </summary>
        private void CheckSignInfo()
        {
            if (ID != string.Empty && pwd != string.Empty)
            {
                if (ID.Length == 18 || ID.Length == 11 || ID.Contains("@"))
                {
                    var conect = msc;
                    string tpwd = conect.GetPWD(ID);
                    if (tpwd != "NULL")
                    {
                        if (pwd.Equals(tpwd))
                        {
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                HandyControl.Controls.Growl.Success(signInfos[LangIndex][0]);
                                Normal_Jump(new FrameworkElement()
                                {
                                    Tag = "3,2"
                                }, null);
                                CanMoveAccPages = false;
                                UpdateUserUIInfo();
                            }));
                        }
                        else
                        {
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                HandyControl.Controls.Growl.Error(signInfos[LangIndex][2]);
                            }));
                        }
                    }
                    else
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            HandyControl.Controls.Growl.Warning(signInfos[LangIndex][3]);
                        }));
                    }
                }
                else
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        HandyControl.Controls.Growl.Warning(signInfos[LangIndex][5]);
                    }));
                }
            }
            else
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    HandyControl.Controls.Growl.Warning(signInfos[LangIndex][4]);
                }));
            }
        }

        /// <summary>
        /// 注册事件
        /// </summary>
        private void SignUp()
        {
            if (CanSignUp)
            {
                SignUpThread();
            }
            else
            {
                HandyControl.Controls.Growl.Warning(signUpInfos[LangIndex][2]);
            }
        }

        /// <summary>
        /// 注册事件
        /// </summary>
        private void SignUpThread()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (msc.AddUser(su_id, (Template.FindName("SignUp_Name_Box", this) as TextBox).Text, su_pwd) == 0)
                {
                    HandyControl.Controls.Growl.Warning(signUpInfos[LangIndex][4]);
                }
                else
                {
                    HandyControl.Controls.Growl.Success(signUpInfos[LangIndex][7]);
                    if ((bool)(Template.FindName("ShouldJumpToSignInAfterSignUp", this) as CheckBox).IsChecked)
                    {
                        Normal_Jump(new FrameworkElement()
                        {
                            Tag = "3,0"
                        }, null);
                        (Template.FindName("SignIn_ID_Box", this) as TextBox).Text = su_id;
                        (Template.FindName("SignIn_Pwd_Box", this) as PasswordBox).Password = su_pwd;
                    }
                }
            }));
        }

        string su_id, su_pwd, su_rpwd;

        private bool CanSignUp = false;

        /// <summary>
        /// 检查注册信息格式是否正确
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckSignUpFormat(object sender, TextChangedEventArgs e) => UpdateSignUpInfo();

        /// <summary>
        /// 密码框文本更新事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e) => UpdateSignUpInfo();

        /// <summary>
        /// 更新注册提示框信息
        /// </summary>
        private void UpdateSignUpInfo()
        {
            su_id = (Template.FindName("SignUp_ID_Box", this) as TextBox).Text;
            su_pwd = (Template.FindName("SignUp_PWD_Box", this) as PasswordBox).Password;
            su_rpwd = (Template.FindName("SignUp_RPWD_Box", this) as PasswordBox).Password;
            string infoDisplay = "";
            if (su_id.Length != 11)
            {
                infoDisplay += $"1.{signUpInfos[LangIndex][0]}";
                CanSignUp = false;
                if (su_pwd != su_rpwd)
                {
                    infoDisplay += $"\r\n2.{signUpInfos[LangIndex][1]}";
                }
            }
            else if (su_pwd != su_rpwd)
            {
                infoDisplay += $"1.{signUpInfos[LangIndex][1]}";
                CanSignUp = false;
            }
            else
            {
                CanSignUp = true;
            }
            (Template.FindName("SignUpInfo", this) as TextBox).Text = infoDisplay;
        }

        /// <summary>
        /// 拖入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToolsList_DragEnter(object sender, DragEventArgs e)
        {
            //if (e.Data.GetDataPresent(DataFormats.FileDrop))
            //{
            //    e.Effects = DragDropEffects.Link;
            //    Border dv = Template.FindName("Dragable", this) as Border;
            //    dv.BorderBrush = FindResource("Linear_BlueToRed_3_1") as LinearGradientBrush;
            //}
            //else
            //{
            //    e.Effects = DragDropEffects.None;
            //    Toolslist_SetNoBorder();
            //}
            e.Effects = DragDropEffects.All;
            Border dv = Template.FindName("Dragable", this) as Border;
            dv.BorderBrush = FindResource("Linear_BlueToRed_3_1") as LinearGradientBrush;
        }

        /// <summary>
        /// 拖出
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToolsList_DragLeave(object sender, DragEventArgs e) => Toolslist_SetNoBorder();

        /// <summary>
        /// 工具栏无边框显示
        /// </summary>
        private void Toolslist_SetNoBorder()
        {
            Border dv = Template.FindName("Dragable", this) as Border;
            dv.BorderBrush = new SolidColorBrush((Color)Colors.Transparent);
        }

        /// <summary>
        /// 汇报错误文件
        /// </summary>
        /// <param name="errFile"></param>
        private void ReportErrFile(List<string> errFile)
        {
            if (errFile.Count >= 1)
            {
                DialogHost dig = new DialogHost();
                TextBox tb = new TextBox()
                {
                    IsReadOnly = true,
                    TextWrapping = TextWrapping.Wrap,
                    Text = $"Error plug-ins:\r\n{Library.TextHelper.Text.ListToLines(errFile)}"
                };
                Button but = new Button()
                {
                    Content = "Okay",
                    Command = DialogHost.CloseDialogCommand,
                    CommandTarget = dig
                };
                StackPanel host = new StackPanel();
                host.Children.Add(tb);
                host.Children.Add(but);
                dig.ShowDialog(host);
            }
        }

        /// <summary>
        /// 拖放
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToolsList_Drop(object sender, DragEventArgs e)
        {
            Toolslist_SetNoBorder();
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                List<string> errFile = new List<string>();
                foreach (string path in files)
                {
                    string extension = Library.TextHelper.Text.ToCapital(Path.GetExtension(path));
                    if (extension.Equals(".DLL") || extension.Equals(".KXT"))
                    {
                        AddPG(path);
                    }
                    else
                    {
                        errFile.Add(path);
                    }
                }
                ReportErrFile(errFile);
            }
            e.Handled = true;
            ReFresher_Click(null, null);
        }

        /// <summary>
        /// 刷新路径界面设定
        /// </summary>
        private void RefreshPathSetting()
        {
            (Template.FindName("TPSetter", this) as TextBox).Text = App.ToolsPath;
            (Template.FindName("TWSetter", this) as TextBox).Text = App.ToolsBase;
            (Template.FindName("LPSetter", this) as TextBox).Text = App.MainLogFile;
        }

        /// <summary>
        /// 路径设置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PathSet(object sender, RoutedEventArgs e)
        {
            switch ((sender as FrameworkElement).Tag.ToString())
            {
                case "TP":
                    using (System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog()
                    {
                        Description = "Select Path",
                        ShowNewFolderButton = true,
                        SelectedPath = "C:\\"
                    })
                    {
                        if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK && fbd.SelectedPath != null)
                        {
                            App.ToolsPath = fbd.SelectedPath;
                            App.SaveConfig();
                        }
                        fbd.Dispose();
                    }
                    break;
                case "TW":
                    using (System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog()
                    {
                        Description = "Select Path",
                        ShowNewFolderButton = true,
                        SelectedPath = "C:\\"
                    })
                    {
                        if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK && fbd.SelectedPath != null)
                        {
                            App.ToolsBase = fbd.SelectedPath;
                            App.SaveConfig();
                        }
                        fbd.Dispose();
                    }
                    break;
                case "LP":
                    Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog()
                    {
                        Multiselect = false,
                        CheckFileExists = true,
                        CheckPathExists = true,
                        DefaultExt = "Log file|*.log",
                        Filter = "Log file|*.log|All file|*.*",
                        Title = "Select one log file path",
                        ValidateNames = true
                    };
                    openFileDialog.ShowDialog();
                    if (openFileDialog.FileName != null)
                    {
                        App.MainLogFile = openFileDialog.FileName;
                        App.SaveConfig();
                    }
                    break;
            }
            RefreshPathSetting();
        }

        /// <summary>
        /// 刷新路径界面设定（按钮触发）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshPathSetter(object sender, RoutedEventArgs e) => RefreshPathSetting();

        /// <summary>
        /// 更新工具未选中状态
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToolsList_LostFocus(object sender, RoutedEventArgs e) => (sender as ListBox).SelectedIndex = -1;

        /// <summary>
        /// 搜索时事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Searching(object sender, TextChangedEventArgs e)
        {

        }

        /// <summary>
        /// 检查更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckForUpdate(object sender, RoutedEventArgs e)
        {
            new Thread(() =>
            {
                if (App.MyVersion.Equals(msc.GetLatestVersion()))
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        HandyControl.Controls.Growl.Info(signUpInfos[LangIndex][13]);
                    }));
                }
                else
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        HandyControl.Controls.Growl.Warning(signUpInfos[LangIndex][14]);
                    }));
                }
            }).Start();
        }

        /// <summary>
        /// 标记页面不能切换
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CantMove(object sender, RoutedEventArgs e) => CanMovePage = false;
        
        /// <summary>
        /// 标记页面可以切换
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CanMove(object sender, RoutedEventArgs e) => CanMovePage = true;

        /// <summary>
        /// 设定为自启动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartBySelf_Checked(object sender, RoutedEventArgs e) => Library.Windows.SetStart.SetSelfStarting(true, "KitX");

        /// <summary>
        /// 取消自启动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartBySelf_Unchecked(object sender, RoutedEventArgs e) => Library.Windows.SetStart.SetSelfStarting(false, "KitX");

        /// <summary>
        /// 设置为启动时隐藏至任务栏
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HideAtStart_Checked(object sender, RoutedEventArgs e) => Library.FileHelper.Config.WriteValue($"{App.WorkBase}\\App.config", "ShouldHideStart", "true");

        /// <summary>
        /// 取消设置为启动时隐藏至任务栏
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HideAtStart_Unchecked(object sender, RoutedEventArgs e) => Library.FileHelper.Config.WriteValue($"{App.WorkBase}\\App.config", "ShouldHideStart", "false");

        /// <summary>
        /// HC皮肤类型
        /// </summary>
        public enum SkinType
        {
            Light, Dark
        }

        /// <summary>
        /// 切换 HC皮肤
        /// </summary>
        /// <param name="st">HC皮肤类型</param>
        public void ChangeSkin(SkinType st) => ((App)Application.Current).UpdateSkin(st);
    }

    /// <summary>
    /// 工具栏控制类
    /// </summary>
    public class AppsBar_Controller
    {
        /// <summary>
        /// 新建工具栏实例
        /// </summary>
        AppsBar bar = new AppsBar();

        /// <summary>
        /// 添加工具
        /// </summary>
        /// <param name="item">接口实例</param>
        /// <param name="icon">图标</param>
        /// <param name="name"></param>
        public void AddTool(IContract item, BitmapImage icon, string name) => bar.AddTool(item, icon, name);

        /// <summary>
        /// 移除工具
        /// </summary>
        /// <param name="name">工具名称</param>
        public void RemoveTool(string name) => bar.RemoveTool(name);

        /// <summary>
        /// 刷新主题
        /// </summary>
        public void RefreshTheme() => bar.RefreshBackground();

        /// <summary>
        /// 通知刷新工具主题
        /// </summary>
        /// <param name="tem"></param>
        public void RefreshToolTheme(Core.Theme tem)
        {
            bar.RefreshToolTheme(tem);
            bar.NowTheme = tem;
        }

        public enum Fore { black, white }

        /// <summary>
        /// 设置前景色
        /// </summary>
        /// <param name="fore"></param>
        public void SetForeground(Fore fore)
        {
            switch (fore)
            {
                case Fore.black:
                    bar.Foreground = new SolidColorBrush(Colors.Black);
                    break;
                case Fore.white:
                    bar.Foreground = new SolidColorBrush(Colors.White);
                    break;
            }
        }

        /// <summary>
        /// 显示工具栏
        /// </summary>
        public void Show() => bar.Show();

        /// <summary>
        /// 隐藏工具栏
        /// </summary>
        public void Hide() => bar.Hide();

        /// <summary>
        /// 关闭工具栏
        /// </summary>
        public void Close()
        {
            bar.CanCloseNow = true;
            bar.Close();
            bar = null;
            bar = new AppsBar();
        }

        /// <summary>
        /// 完全退出工具栏
        /// </summary>
        public void Quit()
        {
            bar.CanCloseNow = true;
            bar.Close();
        }

        /// <summary>
        /// 锁定
        /// </summary>
        public void Lock() => bar.Locker(true);

        /// <summary>
        /// 解锁
        /// </summary>
        public void UnLock() => bar.Locker(false);

        /// <summary>
        /// 选择appsBar的位置
        /// </summary>
        /// <param name="loc"></param>
        public void SelectLocation(int loc) => bar.LS.SelectedIndex = loc;
    }
}
