using Automatic_Replay_Buffer.ViewModel;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using Application = System.Windows.Application;

namespace Automatic_Replay_Buffer
{
    public partial class App : Application
    {
        public NotifyIcon? notifyIcon;
        private ContextMenuStrip? notifyIconContextMenu;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var vm = new MainViewModel();

            await vm.StorageService.LoadAsync();

            var mainWindow = new MainWindow() { DataContext = vm };
            Current.MainWindow = mainWindow;

            notifyIconContextMenu = BuildContextMenu(mainWindow);

            var iconResource = GetResourceStream(new Uri("pack://application:,,,/Resources/icon.ico"));

            Icon icon = iconResource?.Stream != null ? new Icon(iconResource.Stream) : SystemIcons.Application;

            notifyIcon = new NotifyIcon
            {
                Icon = icon,
                ContextMenuStrip = notifyIconContextMenu,
                Text = "Automatic Replay Buffer",
                Visible = false
            };

            notifyIcon.MouseUp += (sender, args) =>
            {
                if (args.Button == MouseButtons.Right)
                {
                    notifyIconContextMenu?.Show(Cursor.Position);
                }
                else if (args.Button == MouseButtons.Left)
                {
                    ShowMainWindow(mainWindow);
                }
            };

            notifyIcon.Visible = true;

            if (vm.StorageService.Settings.StartMinimized)
            {
                mainWindow.WindowState = WindowState.Minimized;
                mainWindow.Hide();
            }
            else
            {
                ShowMainWindow(mainWindow);
            }

            await vm.InitializeAsync();
        }

        private void ShowMainWindow(MainWindow mainWindow)
        {
            mainWindow.Dispatcher.Invoke(() =>
            {
                try
                {
                    if (mainWindow.WindowState == WindowState.Minimized)
                        mainWindow.WindowState = WindowState.Normal;

                    if (!mainWindow.IsVisible)
                        mainWindow.Show();

                    mainWindow.ShowInTaskbar = true;
                    mainWindow.Activate();
                }
                catch {}
            }, DispatcherPriority.Send);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (notifyIcon != null)
            {
                notifyIcon.Visible = false;
                notifyIcon.ContextMenuStrip = null;
                notifyIcon.Dispose();
                notifyIcon = null;
            }

            base.OnExit(e);
        }

        private static ContextMenuStrip BuildContextMenu(MainWindow mainWindow)
        {
            var menu = new ContextMenuStrip();

            menu.Items.Add(new ToolStripMenuItem("Open", null, (s, ev) =>
            {
                mainWindow.Dispatcher.BeginInvoke(new Action(() =>
                {
                    mainWindow.Show();
                    mainWindow.Activate();

                    if (mainWindow.WindowState == WindowState.Minimized)
                        mainWindow.WindowState = WindowState.Normal;
                }));
            }));

            menu.Items.Add(new ToolStripMenuItem("Exit", null, (s, ev) =>
            {
                Current.Dispatcher.BeginInvoke(new Action(() => Current.Shutdown()));
            }));

            return menu;
        }
    }
}