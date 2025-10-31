using Automatic_Replay_Buffer.Models;
using Automatic_Replay_Buffer.Models.Helpers;
using Automatic_Replay_Buffer.ViewModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.ComponentModel;
using Application = System.Windows.Application;
using Automatic_Replay_Buffer.Views;

namespace Automatic_Replay_Buffer
{
    public partial class MainView : Window
    {
        private readonly NotifyIcon _notifyIcon;

        public MainView()
        {
            InitializeComponent();

            var vm = new MainViewModel();
            DataContext = vm;

            var iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/icon.ico")).Stream;

            _notifyIcon = new NotifyIcon
            {
                Icon = new Icon(iconStream),
                Visible = true,
                Text = "Automatic Replay Buffer",
                ContextMenuStrip = new ContextMenuStrip()
            };
            _notifyIcon.ContextMenuStrip.Items.Add("Open", null, (s, e) => ShowWindow());
            _notifyIcon.ContextMenuStrip.Items.Add("Exit", null, (s, e) => Application.Current.Shutdown());

            _notifyIcon.DoubleClick += (s, e) => ShowWindow();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                await vm.InitializeAsync();
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (DataContext is MainViewModel vm)
                vm.SleepyTime();

            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }
        }

        private void ShowWindow()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }
    }
}