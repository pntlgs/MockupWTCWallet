using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;

namespace WTCWallet
{
    /// <summary>
    /// Interaction logic for WelcomeWindow.xaml
    /// </summary>
    public partial class WelcomeWindow : MetroWindow
    {
        public WelcomeWindow()
        {
            InitializeComponent();
            ShowCloseButton = false;
            
            Task.Run(() =>
            {
                while (true)
                {
                    var t = AppVM.Geth.Eth.Blocks.GetBlockNumber.SendRequestAsync().GetAwaiter().GetResult();
                    if (t.Value != 0)
                    {
                        var syncResult = AppVM.Geth.Eth.Syncing.SendRequestAsync().GetAwaiter().GetResult();

                        if (syncResult.IsSyncing)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                LoadingMessage.Text =
                                    "Please wait while the blockchain is downloaded (" + syncResult.CurrentBlock.Value + " of " + syncResult.HighestBlock.Value + " blocks).";
                            });
                        }
                        else
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                LoadingMessage.Text =
                                    "The blockchain has finished downloading (" + t.Value + " blocks).";
                                OKButton.IsEnabled = true;
                                OKButton.ToolTip = null;
                            });
                            break;
                        }
                    }

                    Thread.Sleep(1000);
                }
            });
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
           this.Close();
        }
    }
}
