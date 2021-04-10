using System.Threading;

namespace KitX
{
    public class KxtFileManager
    {
        public static void InstallKxt(string src) => Helper.Program.InstallKxtFile(src, App.ToolsBase, App.ToolsPath);

        public static void InstallKxt(string src, int lazy)
        {
            new Thread(() =>
            {
                Thread.Sleep(lazy);
                Helper.Program.InstallKxtFile(src, App.ToolsBase, App.ToolsPath);
            }).Start();
        }
    }
}
