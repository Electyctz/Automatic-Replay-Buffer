using Automatic_Replay_Buffer.ViewModel;
using System.ComponentModel;
using System.Windows;

namespace Automatic_Replay_Buffer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Window_Closing(object sender, CancelEventArgs e)
        {
            if (DataContext is MainViewModel vm)
                await vm.SleepyTime();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                if (WindowState == WindowState.Minimized && vm.MinimizeToTray)
                {
                    Hide();
                }
            }
        }
    }
}
