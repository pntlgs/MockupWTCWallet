using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace WTCWallet
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            //Stop multiple instances
            if (Process.GetProcessesByName("WTCWallet").Any(t => t.Id != Process.GetCurrentProcess().Id))
            {
                Environment.Exit(0);
            }

            Application.Current.DispatcherUnhandledException += CurrentOnDispatcherUnhandledException;

            base.OnStartup(e);
        }

        private void CurrentOnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs dispatcherUnhandledExceptionEventArgs)
        {
            dispatcherUnhandledExceptionEventArgs.Handled = true;

            MessageBox.Show("An error occurred: " + dispatcherUnhandledExceptionEventArgs.Exception.GetBaseException().Message, "Wallet", MessageBoxButton.OK);
        }
    }
}
