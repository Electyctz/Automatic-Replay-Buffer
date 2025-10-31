using Automatic_Replay_Buffer.ViewModel;
using OBSWebsocketDotNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Automatic_Replay_Buffer.Models.Helpers
{
    public class GameMonitorService(
        LoggingService LoggingService,
        JsonStorageService StorageService,
        OBSService OBSService,
        MainViewModel vm,
        bool RequireFullscreen = true)
    {
        private readonly Dispatcher Dispatcher = System.Windows.Application.Current.Dispatcher;

        public async Task MonitorGamesAsync(
            IProgress<string> statusProgress,
            IProgress<List<MonitorData>> gamesProgress,
            CancellationToken cts)
        {
            if (StorageService?.Game == null || StorageService.Game.Count == 0)
                return;

            DateTime _lastFilterWrite = DateTime.MinValue;
            var lastReported = new List<MonitorData>();

            Dispatcher.Invoke(() => vm.MonitorState = ServiceState.Online);
            LoggingService.Log("Monitoring service started");

            try
            {
                while (!cts.IsCancellationRequested)
                {
                    // if user is fetching game database, pause monitoring
                    //if (_vm.IsFetching == true)
                    //{
                    //    try
                    //    {
                    //        while (_vm.IsFetching && !cts.IsCancellationRequested)
                    //        {
                    //            _vm.MonitorText = "Paused";
                    //            await Task.Delay(1000, cts);
                    //        }
                    //        continue;
                    //    }
                    //    finally
                    //    {
                    //        _vm.MonitorText = "Running";
                    //    }
                    //}

                    try
                    {
                        // reload filter if it has changed
                        var fileInfo = new FileInfo("filter.json");
                        if (fileInfo.Exists && fileInfo.LastWriteTimeUtc > _lastFilterWrite)
                        {
                            _lastFilterWrite = fileInfo.LastWriteTimeUtc;
                            var loadedFilter = await StorageService.LoadConfigAsync("filter.json", new List<FilterData>());
                            StorageService.Filter = loadedFilter ?? new List<FilterData>();
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Log($"Error loading filter.json: {ex.Message}");
                    }

                    var runningGames = new List<MonitorData>();

                    EnumWindows((hWnd, lParam) =>
                    {
                        if (!IsWindowVisible(hWnd)) return true;

                        var sb = new StringBuilder(256);
                        GetWindowText(hWnd, sb, sb.Capacity);
                        string title = sb.ToString();

                        // remove problematic symbols
                        title = Utilities.NormalizeTitle(title);

                        if (string.IsNullOrWhiteSpace(title)) return true;

                        GetWindowThreadProcessId(hWnd, out int pid);
                        try
                        {
                            // get process executable path
                            string? path = Utilities.GetProcessExecutablePath(pid);
                            string exe;
                            if (!string.IsNullOrEmpty(path))
                                exe = Path.GetFileName(path) ?? "unknown.exe";
                            else
                            {
                                try { exe = Process.GetProcessById(pid).ProcessName + ".exe"; }
                                catch { exe = "unknown.exe"; }
                                path ??= "unknown";
                            }

                            // check if window size matches fullscreen
                            if (RequireFullscreen)
                            {
                                GetWindowRect(hWnd, out RECT rect);
                                var screenWidth = SystemParameters.PrimaryScreenWidth;
                                var screenHeight = SystemParameters.PrimaryScreenHeight;
                                if ((rect.Right - rect.Left) != (int)screenWidth ||
                                    (rect.Bottom - rect.Top) != (int)screenHeight)
                                    return true;
                            }

                            // check against user filter
                            bool isFiltered = StorageService.Filter.Any(f =>
                                (!string.IsNullOrEmpty(f.Title) && title.Contains(f.Title, StringComparison.OrdinalIgnoreCase)) ||
                                (!string.IsNullOrEmpty(f.Path) && path.Contains(f.Path, StringComparison.OrdinalIgnoreCase)) ||
                                (!string.IsNullOrEmpty(f.Executable) && exe.Contains(f.Executable, StringComparison.OrdinalIgnoreCase))
                            );

                            // if it's not filtered, add it
                            if (!isFiltered)
                            {
                                runningGames.Add(new MonitorData
                                {
                                    Title = title,
                                    Path = path,
                                    Executable = exe
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            LoggingService.Log($"Error processing window {title} (pid {pid}): {ex}");
                        }

                        return true;
                    }, IntPtr.Zero);

                    // report changes
                    if (!runningGames.SequenceEqual(lastReported))
                    {
                        string status = runningGames.Count > 0
                            ? $"Detected {runningGames.Count} game(s)"
                            : "Idle";

                        statusProgress?.Report(status);
                        gamesProgress?.Report(runningGames);

                        lastReported = runningGames.ToList();
                    }

                    if (runningGames.Count > 0 && !OBSService.isActive)
                    {
                        OBSService.StartBuffer();
                    }
                    else if (runningGames.Count == 0 && OBSService.isActive)
                    {
                        OBSService.StopBuffer();
                    }

                    if (!cts.IsCancellationRequested)
                        await Task.Delay(5000, cts);
                }
            }
            catch (OperationCanceledException)
            {
                LoggingService.Log("Monitoring service cancelled");
            }
            catch (Exception ex)
            {
                LoggingService.Log($"Monitoring service terminated with exception: {ex.Message}");
            }
            finally
            {
                LoggingService.Log("Monitoring service exited");
                Dispatcher.Invoke(() => vm.MonitorState = ServiceState.Offline);
            }
        }

        #region Win32 P/Invoke
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }
        #endregion
    }
}
