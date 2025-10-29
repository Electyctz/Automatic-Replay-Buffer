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
    public class GameMonitorService
    {
        private readonly JsonStorageService StorageService;
        private readonly bool RequireFullscreen;
        private readonly OBSService _OBSService;
        private readonly MainViewModel vm;

        public GameMonitorService(JsonStorageService _storageService,
            OBSService _obsService,
            MainViewModel _vm,
            bool _requireFullscreen = true)
        {
            StorageService = _storageService;
            RequireFullscreen = _requireFullscreen;
            _OBSService = _obsService;
            vm = _vm;
        }

        public async Task MonitorGamesAsync(
            IProgress<string> statusProgress,
            IProgress<List<MonitorData>> gamesProgress,
            CancellationToken cts)
        {
            if (StorageService?.Game == null || StorageService.Game.Count == 0)
                return;

            DateTime _lastFilterWrite = DateTime.MinValue;
            var lastReported = new List<MonitorData>();

            vm.MonitorText = "Running";
            vm._LoggingService.Log("Monitoring service started");
            Debug.WriteLine("Monitoring service started");

            try
            {
                while (!cts.IsCancellationRequested)
                {
                    if (vm.IsFetching == true)
                    {
                        try
                        {
                            while (vm.IsFetching && !cts.IsCancellationRequested)
                            {
                                vm.MonitorText = "Paused";
                                await Task.Delay(1000, cts);
                            }
                            continue;
                        }
                        finally
                        {
                            vm.MonitorText = "Running";
                        }
                    }

                    try
                    {
                        var fileInfo = new FileInfo("filter.json");
                        if (fileInfo.Exists && fileInfo.LastWriteTimeUtc > _lastFilterWrite)
                        {
                            _lastFilterWrite = fileInfo.LastWriteTimeUtc;
                            var loadedFilter = await JsonStorageService.LoadConfigAsync("filter.json", new List<FilterData>());
                            StorageService.Filter = loadedFilter ?? new List<FilterData>();
                        }
                    }
                    catch (Exception ex)
                    {
                        vm._LoggingService.Log($"Error loading filter.json: {ex.Message}");
                        Debug.WriteLine($"Error loading filter.json: {ex.Message}");
                    }

                    var runningGames = new List<MonitorData>();

                    EnumWindows((hWnd, lParam) =>
                    {
                        if (!IsWindowVisible(hWnd)) return true;

                        var sb = new StringBuilder(256);
                        GetWindowText(hWnd, sb, sb.Capacity);
                        string title = sb.ToString();
                        if (string.IsNullOrWhiteSpace(title)) return true;

                        GetWindowThreadProcessId(hWnd, out int pid);
                        try
                        {
                            var process = Process.GetProcessById(pid);
                            string path = process.MainModule.FileName;
                            string exe = process.ProcessName + ".exe";

                            if (RequireFullscreen)
                            {
                                GetWindowRect(hWnd, out RECT rect);
                                var screenWidth = SystemParameters.PrimaryScreenWidth;
                                var screenHeight = SystemParameters.PrimaryScreenHeight;
                                if ((rect.Right - rect.Left) != (int)screenWidth ||
                                    (rect.Bottom - rect.Top) != (int)screenHeight)
                                    return true;
                            }

                            bool isFiltered = StorageService.Filter.Any(f =>
                                (!string.IsNullOrEmpty(f.Title) && title.Contains(f.Title, StringComparison.OrdinalIgnoreCase)) ||
                                (!string.IsNullOrEmpty(f.Path) && path.Contains(f.Path, StringComparison.OrdinalIgnoreCase)) ||
                                (!string.IsNullOrEmpty(f.Executable) && exe.Contains(f.Executable, StringComparison.OrdinalIgnoreCase))
                            );

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
                            vm._LoggingService.Log($"Error processing window {title}: {ex.Message}");
                            Debug.WriteLine($"Error processing window {title}: {ex.Message}");
                        }

                        return true;
                    }, IntPtr.Zero);

                    if (!runningGames.SequenceEqual(lastReported))
                    {
                        string status = runningGames.Count > 0
                            ? $"Detected {runningGames.Count} game(s)"
                            : "Idle";

                        statusProgress?.Report(status);
                        gamesProgress?.Report(runningGames);

                        lastReported = runningGames.ToList();
                    }

                    if (runningGames.Count > 0 && !_OBSService.isActive)
                    {
                        _OBSService.StartBuffer();
                    }
                    else if (runningGames.Count == 0 && _OBSService.isActive)
                    {
                        _OBSService.StopBuffer();
                    }

                    if (!cts.IsCancellationRequested)
                        await Task.Delay(5000);
                }
            }
            catch (OperationCanceledException)
            {
                vm._LoggingService.Log("Monitoring service cancelled");
            }
            catch (Exception ex)
            {
                vm._LoggingService.Log($"Monitoring service terminated with exception: {ex.Message}");
            }
            finally
            {
                vm._LoggingService.Log("Monitoring service exited");
                vm.MonitorText = "Stopped";
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
