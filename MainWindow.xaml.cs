using Automatic_Replay_Buffer.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Application = System.Windows.Application;

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
