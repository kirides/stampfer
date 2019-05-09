/**************************************************************************************
    Stampfer - Gothic Script Editor
    Copyright (C) 2008, 2009 Jpmon1, Alexander "Sumpfkrautjunkie" Ruppert

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
**************************************************************************************/
using Peter.Http;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
//channel
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Peter
{
    internal static class Program
    {
        private static Mutex mutex;
        private const int SW_RESTORE = 9;

        [DllImport("user32.dll")]
        private static extern int ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern int SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int IsIconic(IntPtr hWnd);

        private static async Task SwitchToCurrentInstance(string[] args)
        {
            try
            {
                var httpClient = new HttpClient();
                var files = new List<string>();
                foreach (var arg in args)
                {
                    if (File.Exists(arg))
                    {
                        files.Add(arg);
                    }
                }
                if (files.Count != 0 && File.Exists(IpcDat) && int.TryParse(File.ReadLines(IpcDat).First(), out int port))
                {
                    await httpClient.PostAsync($"http://127.0.0.1:{port}/openfile", new StringContent(string.Join("\n", files), Encoding.Unicode)).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        /// <summary>
        /// check if given exe alread running or not
        /// </summary>
        /// <returns>returns true if already running</returns>
        private static bool IsAlreadyRunning()
        {
            var sExeName = Path.GetFileName(ExePath);
            bool bCreatedNew;

            mutex = new Mutex(true, "Global\\" + sExeName, out bCreatedNew);

            if (bCreatedNew)
                mutex.ReleaseMutex();

            return !bCreatedNew;
        }

        private static HttpServer httpServer;
        private static CancellationTokenSource httpServerToken;
        private static Task HttpServerListening;

        public static readonly string ExePath = Assembly.GetEntryAssembly().Location;
        public static readonly string ExeDirectoryPath = Path.GetDirectoryName(ExePath);
        private static readonly string IpcDat = Path.Combine(Path.GetTempPath(), "Stampfer.2462342.ipc");
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// 

        private static MainForm MainForm;

        [STAThread]
        private static void Main(string[] args)
        {
            if (IsAlreadyRunning()/* && args.Length > 0*/)
            {
                SwitchToCurrentInstance(args).GetAwaiter().GetResult();
                Application.Exit();
            }
            else
            {
                CleanupTempFiles();
                Log.Start();
                SetupIPC();
                System.Runtime.ProfileOptimization.SetProfileRoot(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
                System.Runtime.ProfileOptimization.StartProfile("startup.profile");
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                try
                {
                    Application.Run(MainForm = new MainForm(args));
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                }
                Log.Flush();
            }
        }

        private static void SetupIPC()
        {
            httpServer = new HttpServer();
            var router = new HttpRouter();
            router.HandleRequest("/openfile", HttpOpenFile);
            Application.ApplicationExit += ApplicationClosing;
            httpServerToken = new CancellationTokenSource();
            HttpServerListening = httpServer.ListenLocalAsync(router, httpServerToken.Token);
            File.WriteAllText(IpcDat, httpServer.Port.ToString());
        }

        private static async Task HttpOpenFile(HttpContext arg)
        {
            try
            {
                var files = new List<string>();
                using (var sr = new StreamReader(arg.Request.InputStream, Encoding.Unicode))
                {
                    while (await sr.ReadLineAsync() is string line)
                    {
                        if (File.Exists(line))
                        {
                            files.Add(line);
                        }
                    }
                }

                arg.Response.StatusCode = 200;
                if (files.Count == 0)
                {
                    return;
                }
                MainForm.BeginInvoke((Action)(() =>
                {
                    MainForm.OpenFilesInEditor(files);

                    if (IsIconic(MainForm.Handle) != 0)
                    {
                        ShowWindow(MainForm.Handle, SW_RESTORE);
                    }
                    // Set foreground window.
                    SetForegroundWindow(MainForm.Handle);
                }));
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }
        private static void CleanupTempFiles()
        {
            if (File.Exists(IpcDat)) File.Delete(IpcDat);

        }
        private static void ApplicationClosing(object sender, EventArgs e)
        {
            try
            {
                httpServerToken.Cancel();
                CleanupTempFiles();
                HttpServerListening.GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }
    }

    public static class Log
    {
        private static readonly ConcurrentQueue<string> lines = new ConcurrentQueue<string>();
        private static Task LogTask;
        private static CancellationTokenSource cts = new CancellationTokenSource();

        private static Task StartLogging(CancellationToken token)
        {
            return Task.Run(() =>
            {
                WorkOnQueue(token);
                Stop();
            });
        }
        private static void WorkOnQueue(CancellationToken cancellationToken = default)
        {
            using (var fs = File.Open(Path.Combine(Program.ExeDirectoryPath, "stampfer.log"), FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            using (var sw = new StreamWriter(fs, Encoding.UTF8))
            {
                while (!cancellationToken.IsCancellationRequested && lines.TryDequeue(out var line))
                {
                    sw.WriteLine(line);
                }
            }
        }

        private static void Resume()
        {
            if (LogTask is null)
            {
                Start();
            }
        }
        public static void Flush()
        {
            Stop();
            WorkOnQueue();
        }
        public static void Start()
        {
            Stop();
            LogTask = StartLogging(cts.Token);
        }
        public static void Stop()
        {
            cts?.Cancel();
            cts = new CancellationTokenSource();
            LogTask = null;
        }

        public static void Exception(Exception e)
        {
            Line($"{e.Source}|{e.Message}", "ERROR");
        }

        private static void Line(string line, string type)
        {
            lines.Enqueue($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}|{type}|{line}");
            Resume();
        }
    }
}
