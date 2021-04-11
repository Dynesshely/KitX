using KitX.Core;
using lib_windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Xml;
using Window = System.Windows.Window;

namespace KitX
{
    /// <summary>
    /// AppsBar.xaml 的交互逻辑
    /// </summary>
    public partial class AppsBar : Window
    {
        public bool CanCloseNow = false;
        public bool CanAnimate = true;
        private readonly DoubleAnimation Chrome_FadeIn;
        private readonly DoubleAnimation Chrome_FadeOut;
        private readonly PopEye.WPF.Animation.AnimationHelper animationHelper = new PopEye.WPF.Animation.AnimationHelper();
        private readonly System.Windows.Forms.Timer ti;

        public Theme NowTheme = Theme.Light;

        #region HotKey
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindow([MarshalAs(UnmanagedType.LPTStr)] string lpClassName, [MarshalAs(UnmanagedType.LPTStr)] string lpWindowName);

        [DllImport("user32")]
        private static extern IntPtr FindWindowEx(IntPtr hWnd1, IntPtr hWnd2, string lpsz1, string lpsz2);

        [DllImport("user32.dll")]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        int hotkey;
        #endregion

        #region Window styles
        [Flags]
        public enum ExtendedWindowStyles
        {
            // ...
            WS_EX_TOOLWINDOW = 0x00000080,
            // ...
        }

        public enum GetWindowLongFields
        {
            // ...
            GWL_EXSTYLE = (-20),
            // ...
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        public static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            int error = 0;
            IntPtr result = IntPtr.Zero;
            // Win32 SetWindowLong doesn't clear error on success
            SetLastError(0);

            if (IntPtr.Size == 4)
            {
                // use SetWindowLong
                Int32 tempResult = IntSetWindowLong(hWnd, nIndex, IntPtrToInt32(dwNewLong));
                error = Marshal.GetLastWin32Error();
                result = new IntPtr(tempResult);
            }
            else
            {
                // use SetWindowLongPtr
                result = IntSetWindowLongPtr(hWnd, nIndex, dwNewLong);
                error = Marshal.GetLastWin32Error();
            }

            if ((result == IntPtr.Zero) && (error != 0))
            {
                throw new System.ComponentModel.Win32Exception(error);
            }

            return result;
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr IntSetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern Int32 IntSetWindowLong(IntPtr hWnd, int nIndex, Int32 dwNewLong);

        private static int IntPtrToInt32(IntPtr intPtr)
        {
            return unchecked((int)intPtr.ToInt64());
        }

        [DllImport("kernel32.dll", EntryPoint = "SetLastError")]
        public static extern void SetLastError(int dwErrorCode);
        #endregion

        public AppsBar()
        {
            InitializeComponent();

            Docker.Height = 0;

            Width = App.WorkAreaWidth;
            Top = 0;
            Left = 0;

            SizeToContent = SizeToContent.Height;

            Chrome_FadeOut = animationHelper.CreateDoubleAnimation(TimeSpan.FromMilliseconds(300), 80, 0, FillBehavior.HoldEnd,
                PopEye.WPF.Animation.AnimationHelper.EasingFunction.Cubic, EasingMode.EaseOut, 0, 0);
            Chrome_FadeIn = animationHelper.CreateDoubleAnimation(TimeSpan.FromMilliseconds(300), 0, 80, FillBehavior.HoldEnd,
                PopEye.WPF.Animation.AnimationHelper.EasingFunction.Cubic, EasingMode.EaseOut, 0, 0);

            Loaded += AppsBar_Loaded;
            Loaded += (x, y) =>
            {
                RefreshBackground();
                HideAltTab();
                AddLocalAppsIcon();
                LS.SelectionChanged += (m, n) =>
                {
                    switch (LS.SelectedIndex)
                    {
                        case 0:
                            Width = App.WorkAreaWidth;
                            Top = 0;
                            Left = 0;
                            SizeToContent = SizeToContent.Height;
                            AppsContainer.VerticalAlignment = VerticalAlignment.Top;
                            Docker.VerticalAlignment = VerticalAlignment.Top;
                            Quicker.Margin = new Thickness(0, 130, 0, 0);
                            Quicker.VerticalAlignment = VerticalAlignment.Top;
                            checker.VerticalAlignment = VerticalAlignment.Top;
                            App.AppsBarStartLocation = 0;
                            break;
                        case 1:
                            Width = App.WorkAreaWidth;
                            Top = 0;
                            Left = 0;
                            SizeToContent = SizeToContent.Manual;
                            Height = App.WorkAreaHeight - 30;
                            AppsContainer.VerticalAlignment = VerticalAlignment.Bottom;
                            Docker.VerticalAlignment = VerticalAlignment.Bottom;
                            Quicker.Margin = new Thickness(0, 0, 0, 130);
                            Quicker.VerticalAlignment = VerticalAlignment.Bottom;
                            checker.VerticalAlignment = VerticalAlignment.Bottom;
                            App.AppsBarStartLocation = 1;
                            break;
                    }
                    App.SaveConfig();
                };
                PinToScreen.IsChecked = Convert.ToBoolean(Library.FileHelper.Config.ReadValue($"{App.WorkBase}\\App.config", "AppsBarLockState"));
                UpdatePinEvent(PinToScreen, null);
            };

            KeyDown += AppsBar_KeyDown;

            Closing += (x, y) =>
            {
                if (!CanCloseNow)
                {
                    y.Cancel = true;
                }
                else
                {
                    ti.Stop();
                    ti.Dispose();
                }
            };

            timer.Text = DateTime.Now.ToString($"{(TS.SelectedIndex == 0 ? "hh" : "HH")}:mm");
            ti = new System.Windows.Forms.Timer()
            {
                Interval = 10 * 1000
            };
            ti.Tick += (x, y) =>
            {
                timer.Text = DateTime.Now.ToString($"{(TS.SelectedIndex == 0 ? "hh" : "HH")}:mm");
            };
            ti.Start();

            IntPtr pWnd = FindWindow("AppsBar", null);
            pWnd = FindWindowEx(pWnd, IntPtr.Zero, "SHELLDLL_DefVIew", null);
            pWnd = FindWindowEx(pWnd, IntPtr.Zero, "SysListView32", null);
            SetParent(new WindowInteropHelper(this).Handle, pWnd);
        }

        /// <summary>
        /// 键盘事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AppsBar_KeyDown(object sender, KeyEventArgs e)
        {
            int targetIndex = 0;
            switch (e.Key)
            {
                case Key.NumPad1:
                    targetIndex = 1;
                    break;
                case Key.NumPad2:
                    targetIndex = 2;
                    break;
                case Key.NumPad3:
                    targetIndex = 3;
                    break;
                case Key.NumPad4:
                    targetIndex = 4;
                    break;
                case Key.NumPad5:
                    targetIndex = 5;
                    break;
                case Key.NumPad6:
                    targetIndex = 6;
                    break;
                case Key.NumPad7:
                    targetIndex = 7;
                    break;
                case Key.NumPad8:
                    targetIndex = 8;
                    break;
                case Key.NumPad9:
                    targetIndex = 9;
                    break;
            }
            int ni = 1;
            foreach (IContract item in Names.Values)
            {
                if (ni == targetIndex)
                {
                    item.Start();
                    break;
                }
                else
                {
                    ni++;
                }
            }
        }

        /// <summary>
        /// 将窗口置顶并保持在所有虚拟桌面显示
        /// </summary>
        private void HideAltTab()
        {
            WindowInteropHelper wndHelper = new WindowInteropHelper(this);
            int exStyle = (int)GetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE);
            exStyle |= (int)ExtendedWindowStyles.WS_EX_TOOLWINDOW;
            SetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);
        }

        #region 全局热键

        /// <summary>
        /// 添加全局热键事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AppsBar_Loaded(object sender, RoutedEventArgs e)
        {
            HwndSource hWndSource;
            WindowInteropHelper wih = new WindowInteropHelper(this);
            hWndSource = HwndSource.FromHwnd(wih.Handle);
            //添加处理程序
            hWndSource.AddHook(MainWindowProc);
            //hotkey = HotKey.GlobalAddAtom("Alt-K");
            hotkey = HotKey.GlobalAddAtom(Library.FileHelper.Config.ReadValue($"{App.WorkBase}\\App.config", "GlobalHotKey"));
            HotKey.RegisterHotKey(wih.Handle, hotkey, HotKey.KeyModifiers.Alt, (int)System.Windows.Forms.Keys.K);
        }

        private IntPtr MainWindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case HotKey.WM_HOTKEY:
                    int sid = wParam.ToInt32();
                    if (sid == hotkey)
                    {
                        Focus();
                        Animate();
                    }
                    handled = true;
                    break;
            }

            return IntPtr.Zero;
        }

        #endregion

        private Dictionary<string, IContract> Names = new Dictionary<string, IContract>();

        /// <summary>
        /// 添加工具图标
        /// </summary>
        /// <param name="item">工具类</param>
        /// <param name="icon">工具图标</param>
        /// <param name="name">工具名称</param>
        public void AddTool(IContract item, BitmapImage icon, string name)
        {
            // 设定工作目录
            item.SetWorkBase($"{App.ToolsBase}\\{item.GetPublisher()}\\{item.GetName()}");
            Dispatcher.Invoke(new Action(delegate
            {
                if (!IsExists(name))
                {
                    ContextMenu menu = new ContextMenu()
                    {
                        StaysOpen = false
                    };
                    menu.Closed += (x, y) =>
                    {
                        CanAnimate = true;
                        Point p = Mouse.GetPosition(this);
                        if (p.X == 0 || p.Y == 0)
                        {
                            Animate();
                        }
                    };
                    MenuItem m1 = new MenuItem();
                    m1.SetResourceReference(HeaderedItemsControl.HeaderProperty, "Item_Close");
                    m1.Click += (x, y) =>
                    {
                        item.End();
                    };
                    menu.Items.Add(m1);

                    Button btn = new Button()
                    {
                        Name = name,
                        Margin = new Thickness(5),
                        Width = 45,
                        Height = 45,
                        AllowDrop = true,
                        Content = new Image()
                        {
                            Source = icon,
                            Stretch = Stretch.UniformToFill
                        },
                        Style = new ResourceDictionary
                        {
                            Source = new Uri("/KitX;component/PulseButton.xaml", UriKind.Relative) // 指定样式文件的路径
                        }["PulseButton"] as Style,
                        ContextMenu = menu
                    };
                    btn.Click += (x, y) =>
                    {
                        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
                        {
                            item.Start();
                            item.SetTheme(NowTheme);
                        }));
                    };
                    btn.MouseRightButtonDown += (x, y) =>
                    {
                        CanAnimate = false;
                    };
                    btn.MouseEnter += (x, y) =>
                    {
                        Quicker.Content = item.GetQuickView();
                        AppInfoBack.Visibility = Visibility.Visible;
                        AppInfoer.Text = $"{item.GetName()}\r\n{item.GetDescribe_Simple()}";
                    };
                    btn.DragEnter += (x, y) => { y.Effects = DragDropEffects.All; Quicker.Content = item.GetQuickView(); };
                    Quicker.MouseLeave += (x, y) => Quicker.Content = null;
                    fishButtons.Children.Add(btn);
                    Names.Add(name, item);
                }
            }));            
        }

        /// <summary>
        /// 判断是否存在工具
        /// </summary>
        /// <param name="name">工具名称</param>
        /// <returns></returns>
        private bool IsExists(string name)
        {
            foreach (string item in Names.Keys)
            {
                if (item.Equals(name))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 通知插件刷新主题
        /// </summary>
        /// <param name="tem"></param>
        public void RefreshToolTheme(Theme tem)
        {
            foreach (var item in Names.Values)
            {
                item.SetTheme(tem);
            }
        }

        /// <summary>
        /// 移除指定工具
        /// </summary>
        /// <param name="name">工具名称</param>
        public void RemoveTool(string name)
        {
            if (IsExists(name))
            {
                Names.Remove(name);
                foreach (Button item in fishButtons.Children)
                {
                    if (item.Name.Equals(name))
                    {
                        fishButtons.Children.Remove(item);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 刷新窗口背景色
        /// </summary>
        public void RefreshBackground()
        {
            Backgrounder.Background = Application.Current.MainWindow.Background;
            checker.Background = Application.Current.MainWindow.Background;
        }

        #region 防止Alt+F4组合键和Alt+Space组合键
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Alt && e.SystemKey == Key.Space)
            {
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Alt && e.SystemKey == Key.F4)
            {
                e.Handled = true;
            }
            else
            {
                base.OnKeyDown(e);
            }
        }
        #endregion

        bool isAnimating = true;

        /// <summary>
        /// 执行一次展开或收起动画
        /// </summary>
        private void Animate()
        {
            if (CanAnimate)
            {
                if (isAnimating)
                {
                    isAnimating = false;
                    Docker.BeginAnimation(HeightProperty, Chrome_FadeOut);
                }
                else
                {
                    isAnimating = true;
                    Docker.BeginAnimation(HeightProperty, Chrome_FadeIn);
                }
            }
        }

        /// <summary>
        /// 鼠标进入事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Border_MouseEnter(object sender, MouseEventArgs e) => Animate();

        /// <summary>
        /// 鼠标离开事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Backgrounder_MouseLeave(object sender, MouseEventArgs e)
        {
            Animate();
            amated = false;
            AppInfoer.Text = null;
            AppInfoBack.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// 退出应用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExitApp(object sender, RoutedEventArgs e) => App.LazyClose();

        /// <summary>
        /// 锁定事件
        /// </summary>
        /// <param name="_lock"></param>
        public void Locker(bool _lock)
        {
            PinToScreen.IsChecked = _lock ? true : false;
            UpdatePinEvent(PinToScreen, null);
        }

        /// <summary>
        /// 更新锁定事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdatePinEvent(object sender, RoutedEventArgs e)
        {
            if ((bool)(sender as CheckBox).IsChecked)
            {
                isAnimating = false;
                Animate();
            }
            else
            {
                isAnimating = false;
                Docker.BeginAnimation(HeightProperty, Chrome_FadeOut);
            }
            CanAnimate = (sender as CheckBox).IsChecked != true;
            Library.FileHelper.Config.WriteValue($"{App.WorkBase}\\App.config", "AppsBarLockState", Convert.ToString(!CanAnimate));
        }

        bool amated = false;

        /// <summary>
        /// 拖拽事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DragEnter_Origin(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.All;
            if (!amated)
            {
                Animate();
                amated = true;
            }
        }

        bool hasOpenedLAM = false;
        LocalAppsManager lam;

        /// <summary>
        /// 打开本地应用管理器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenLAM(object sender, RoutedEventArgs e)
        {
            if (hasOpenedLAM)
            {
                lam.Topmost = true;
                lam.Topmost = false;
            }
            else
            {
                lam = new LocalAppsManager()
                {
                    appsbar = this
                };
                lam.Closed += (x, y) =>
                {
                    hasOpenedLAM = false;
                };
                lam.Show();
                hasOpenedLAM = true;
            }
        }

        private List<XmlNode> addedApps = new List<XmlNode>();

        /// <summary>
        /// 添加本地应用栏图标
        /// </summary>
        public void AddLocalAppsIcon()
        {
            ClearLocalAppsIcons();
            AddAddLAM_Btn();
            LoadAppsFromXML();
            foreach (XmlNode item in addedApps)
            {
                string iconPath = item.Attributes["icon"].InnerText;
                Button btn = new Button()
                {
                    Name = item.Attributes["name"].InnerText,
                    Margin = new Thickness(5),
                    Width = 45,
                    Height = 45,
                    Content = new Image()
                    {
                        Source = (iconPath == "self") ? Library.Windows.GetAppIcon.GetIcon(item.Attributes["path"].InnerText) : new BitmapImage(new Uri(iconPath, UriKind.Absolute)),
                        Stretch = Stretch.UniformToFill
                    },
                    Style = new ResourceDictionary
                    {
                        Source = new Uri("/KitX;component/PulseButton.xaml", UriKind.Relative) // 指定样式文件的路径
                    }["PulseButton"] as Style
                };
                btn.Click += (x, y) =>
                {
                    Process.Start(item.Attributes["path"].InnerText, item.Attributes["argument"].InnerText);
                };
                btn.MouseEnter += (x, y) =>
                {
                    AppInfoer.Text = $"{item.Attributes["name"].InnerText}\r\n{item.Attributes["path"].InnerText}";
                };
                localAppsLister.Children.Add(btn);
            }
        }

        /// <summary>
        /// 添加打开本地应用管理器按钮
        /// </summary>
        private void AddAddLAM_Btn()
        {
            Button btn = new Button()
            {
                Width = 45, Height = 45, Style = new ResourceDictionary
                {
                    Source = new Uri("/KitX;component/PulseButton.xaml", UriKind.Relative)
                }["PulseButton"] as Style, Content = new Image()
                {
                    Margin = new Thickness(5), Stretch = Stretch.Uniform,
                    Source = new BitmapImage(new Uri("/Source/LocalApps.png", UriKind.Relative))
                }
            };
            btn.Click += OpenLAM;
            localAppsLister.Children.Add(btn);
        }

        /// <summary>
        /// 从xml加载本地应用
        /// </summary>
        private void LoadAppsFromXML()
        {
            addedApps.Clear();
            string contactsSavePath = $"{App.WorkBase}\\Config\\localapps.xml";
            XmlDocument apps = new XmlDocument();
            apps.Load(contactsSavePath);
            XmlNode root = apps.DocumentElement;
            foreach (XmlNode app in root.ChildNodes)
            {
                if (app.Name.Equals("app"))
                {
                    addedApps.Add(app);
                }
            }
        }

        /// <summary>
        /// 移除本地应用栏所有应用图标
        /// </summary>
        private void ClearLocalAppsIcons() => localAppsLister.Children.Clear();
    }
}