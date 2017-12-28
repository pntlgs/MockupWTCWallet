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

            HamburgerMenuControl.SelectedItem =
                (HamburgerMenuControl.ItemsSource as HamburgerMenuItemCollection).FirstOrDefault();

            ((AppVM) DataContext).SelectTab = SelectTab;
        }

        private void SelectTab(string label)
        {
            var coll = ((HamburgerMenuItemCollection)HamburgerMenuControl.ItemsSource);

            var item = coll.FirstOrDefault(c => c.Label == label);
            if (item != null)
            {
                this.HamburgerMenuControl.Content = item;
                // close the pane
                this.HamburgerMenuControl.IsPaneOpen = false;
            }
        }

        private void HamburgerMenuControl_OnItemClick(object sender, ItemClickEventArgs e)
        {
            AppVM vm = (AppVM) DataContext;

            if (!vm.HasAddress && ((HamburgerMenuGlyphItem)e.ClickedItem).Label == "Wallet")
            {
                return;
            }

            // set the content
            this.HamburgerMenuControl.Content = e.ClickedItem;
            // close the pane
            this.HamburgerMenuControl.IsPaneOpen = false;
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
                try
                {
                    var process = Process.GetProcessById(AppVM.ProcessID.Value);
                    if (process.ProcessName == "walton")
                    {
                        process.Kill();
                    }
                }
                catch (Exception)
                {
                    
                }
            }
        }

        private void UIElement_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
          
                ScrollViewer scv = (ScrollViewer)sender;
                scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
                e.Handled = true;
            
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            AboutWindow window = new AboutWindow();

            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            window.ShowDialog();
        }
    }
}
