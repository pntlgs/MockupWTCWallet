using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using Microsoft.Win32;

namespace WTCWallet
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            Closing += OnClosing;

Activated += OnLoaded;
        }

        private Boolean hasShown = false;
        private Object _lock = new object();

        private void OnLoaded(object sender, EventArgs eventArgs)
        {
            Boolean openWindow = false;

            lock (_lock)
            {
                if (!hasShown)
                {
                    openWindow = true;
                    hasShown = true;
                }
            }

            if (openWindow)
            {
                WelcomeWindow window = new WelcomeWindow();
                window.Owner = this;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.ShowDialog();
            }
        }

        private void OnClosing(object sender, CancelEventArgs cancelEventArgs)
        {
            if (AppVM.ProcessID.HasValue)
            {
                var process = Process.GetProcessById(AppVM.ProcessID.Value);
                if (process.ProcessName == "wtcbackend")
                {
                    process.Kill();
                }
            }
        }

        private void UIElement_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
          
                ScrollViewer scv = (ScrollViewer)sender;
                scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
                e.Handled = true;
            
        }
    }
}
