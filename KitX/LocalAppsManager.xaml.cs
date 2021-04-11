/* Info
Author: Catrol
Date  : 2021-04-10
Time  : 11:57 AM
*/

using IWshRuntimeLibrary;
using KitX.Controls;
using Library.FileHelper;
using Library.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using File = System.IO.File;

namespace KitX
{
    /// <summary>
    /// LocalAppsManager.xaml 的交互逻辑
    /// </summary>
    public partial class LocalAppsManager : Window
    {
        public AppsBar appsbar;

        List<AppItem> applist = new List<AppItem>();

        public LocalAppsManager()
        {
            InitializeComponent();
            add_path.Text = $"{App.WorkBase}\\KitX.exe";
            add_name.Text = "KitX";
            RefreshLocalAppsList();

            Loaded += (x, y) =>
            {
                //解决 ScrollViewer 内嵌 ListBox 鼠标滚动失效的问题
                AddedAppsList.PreviewMouseWheel += (sender, e) =>
                {
                    var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                    {
                        RoutedEvent = UIElement.MouseWheelEvent,
                        Source = sender
                    };
                    AddedAppsList.RaiseEvent(eventArg);
                };
            };

            Closed += (x, y) =>
            {
                appsbar.AddLocalAppsIcon();
            };
        }

        /// <summary>
        /// 获取注册表中已安装应用
        /// </summary>
        public void SetInstalledApps()
        {
            List<GetApps.NameAndPath> nameAndPaths = GetApps.GetProgramAndPath();
            foreach (GetApps.NameAndPath item in nameAndPaths)
            {
                if (item.name != null && item.name.Length < 20)
                {
                    string exepath = "";
                    ImageSource isrc = null;
                    if (item.path != "")
                    {
                        string tmp = item.path.Replace("\"", "").Replace("\'", "").Trim();
                        if (System.IO.File.Exists(tmp))
                        {
                            try
                            {
                                isrc = Library.Windows.GetAppIcon.GetIcon(tmp);
                                exepath = tmp;
                            }
                            catch
                            {
                                isrc = null;
                            }
                        }
                        else if (Directory.Exists(tmp))
                        {
                            if (!tmp.EndsWith(@"\"))
                            {
                                tmp += @"\";
                            }
                            string dirName = Path.GetFileName(Path.GetDirectoryName(tmp));
                            tmp += $"{dirName}.exe";
                            if (System.IO.File.Exists(tmp))
                            {
                                try
                                {
                                    isrc = Library.Windows.GetAppIcon.GetIcon(tmp);
                                }
                                catch
                                {
                                    continue;
                                }
                                exepath = tmp;
                            }
                        }
                    }
                    if (isrc != null)
                    {
                        applist.Add(new AppItem()
                        {
                            Name = $"{item.name}",
                            Path = (item.path == "") ? null : item.path,
                            Icon = isrc ?? null,
                            ExePath = exepath
                        });
                    }
                }
            }
        }

        /// <summary>
        /// 添加开始菜单的程序
        /// </summary>
        private void AddStartMenuApplications()
        {
            string ProgramsDir = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            DirectoryInfo programsDir = new DirectoryInfo(ProgramsDir);
            AddDirFiles(programsDir);
            foreach (DirectoryInfo dirs in programsDir.GetDirectories())
            {
                AddDirFiles(dirs);
            }
        }

        /// <summary>
        /// 遍历添加文件夹下所有快捷方式
        /// </summary>
        /// <param name="father">父文件夹</param>
        private void AddDirFiles(DirectoryInfo father)
        {
            if (father.GetFiles().Length >= 0)
            {
                foreach (FileInfo item in father.GetFiles())
                {
                    try
                    {
                        if (Library.TextHelper.Text.ToLower(item.Extension).Equals(".lnk"))
                        {
                            IWshShortcut sc = AnalyzeShortCut.GetShortCut(item.FullName);
                            string name = item.Name;
                            string path = sc.TargetPath;
                            if (!Library.TextHelper.Text.ToLower(name).Contains("uninstall") && !name.Contains("卸载"))
                            {
                                applist.Add(new AppItem()
                                {
                                    Name = name,
                                    Path = path,
                                    ExePath = path,
                                    Icon = Library.Windows.GetAppIcon.GetIcon(path)
                                });
                            }
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
                if (father.GetDirectories().Length >= 0)
                {
                    foreach (DirectoryInfo dirs in father.GetDirectories())
                    {
                        AddDirFiles(dirs);
                    }
                }
            }
        }

        /// <summary>
        /// 应用类
        /// </summary>
        public class AppItem
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public ImageSource Icon { get; set; }
            public string ExePath { get; set; }
        }

        /// <summary>
        /// “添加”按钮事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void add_btn_Click(object sender, RoutedEventArgs e)
        {
            string path = add_path.Text, icon = add_icon.Text;
            if (File.Exists(path))
            {
                AddedAppsList.Items.Add(new AddedLocalApps(add_name.Text, path, icon, add_argu.Text)
                {
                    list = AddedAppsList
                });
            }
        }

        /// <summary>
        /// 刷新本地应用已选择表单
        /// </summary>
        private void Refresh_AddedAppsList()
        {
            AddedAppsList.Items.Clear();
            LoadAddedAppsFromXML();
        }

        /// <summary>
        /// 从XML文件载入已选择本地应用
        /// </summary>
        private void LoadAddedAppsFromXML()
        {
            string contactsSavePath = $"{App.WorkBase}\\Config\\localapps.xml";
            XmlDocument apps = new XmlDocument();
            apps.Load(contactsSavePath);
            XmlNode root = apps.DocumentElement;
            foreach (XmlNode app in root.ChildNodes)
            {
                if (app.Name.Equals("app"))
                {
                    PresentateAddedApps(app);
                }
            }
        }

        /// <summary>
        /// 呈现本地已添加应用
        /// </summary>
        private void PresentateAddedApps(XmlNode xn) => AddedAppsList.Items.Add(new AddedLocalApps(xn)
        {
            list = AddedAppsList
        });

        /// <summary>
        /// “应用”按钮事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void apply_btn_Click(object sender, RoutedEventArgs e)
        {
            string tarPath = $"{App.WorkBase}\\Config\\localapps.xml";
            File.Delete(tarPath);
            File.Create(tarPath).Dispose();
            FileHelper.WriteIn(tarPath, "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\r\n<apps>\r\n");
            foreach (AddedLocalApps ala in AddedAppsList.Items)
            {
                FileHelper.Append(tarPath, $"  {ala.XmlCode.Text}\r\n");
            }
            FileHelper.Append(tarPath, "</apps>");
        }

        /// <summary>
        /// “刷新按钮事件”
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void refresh_btn_Click(object sender, RoutedEventArgs e) => Refresh_AddedAppsList();

        /// <summary>
        /// 刷新本地应用表单
        /// </summary>
        private void RefreshLocalAppsList()
        {
            applist.Clear();
            LocalAppsList.ItemsSource = applist;
            SetInstalledApps();
            AddStartMenuApplications();
            Refresh_AddedAppsList();
        }
    }
}
