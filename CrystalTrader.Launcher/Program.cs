using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace CrystalTrader.Launcher
{
    class Program
    {
        const int PROCESS_WAIT_TIMEOUT = 1;

        [DllImport("user32.dll")]
        static extern int SetWindowText(IntPtr hWnd, string text);

        static void Main(string[] args)
        {
            string instanceName = args.Length == 1 ? args[0] : null;
            string processFileName = $"{nameof(CrystalTrader)}.dll";
            string processPath = Directory.EnumerateFiles(Directory.GetCurrentDirectory(), processFileName, SearchOption.AllDirectories).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(processPath))
            {
                Process process = new Process();
                try
                {
                    process.StartInfo.FileName = "dotnet";
                    process.StartInfo.Arguments = $"\"{processPath}\"";
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                    process.Start();
                    Thread.Sleep(TimeSpan.FromSeconds(PROCESS_WAIT_TIMEOUT));
                    if (process.HasExited)
                    {
                        MessageBox.Show($"Unable to start CrystalTrader.{Environment.NewLine}{Environment.NewLine}Please make sure you have the latest .NET Core Runtime installed from{Environment.NewLine}https://www.microsoft.com/net/download", nameof(CrystalTrader));
                        return;
                    }
                    SpinWait.SpinUntil(() =>
                    {
                        return process.MainWindowHandle != IntPtr.Zero;
                    }, TimeSpan.FromSeconds(PROCESS_WAIT_TIMEOUT));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}{Environment.NewLine}{Environment.NewLine}Please download the latest .NET Core Runtime from{Environment.NewLine}https://www.microsoft.com/net/download", nameof(CrystalTrader));
                }

                try
                {
                    if (!String.IsNullOrWhiteSpace(instanceName))
                    {
                        SetWindowText(process.MainWindowHandle, $"{nameof(CrystalTrader)} - {instanceName}");
                    }
                    else
                    {
                        SetWindowText(process.MainWindowHandle, $"{nameof(CrystalTrader)}");
                    }
                }
                catch { }
            }
            else
            {
                MessageBox.Show($"{processFileName} not found", nameof(CrystalTrader));
            }
        }
    }
}