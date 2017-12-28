using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Nethereum.Geth;
using Nethereum.JsonRpc.Client;
using Nethereum.KeyStore;
using Newtonsoft.Json;
using WTCWallet.Annotations;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using Timer = System.Windows.Forms.Timer;

namespace WTCWallet
{
    public class AppVM : INotifyPropertyChanged
    {
        private static AppVM _instance;
        private static readonly object _lock = new object();
        private AddressVM _addressVm;
        private string _status;
        private string _miningStatus;
        private BaseCommand _newAccountCommand;
        private BaseCommand _openAccountCommand;
        private string _nodeStatus;

        public Boolean ShowHome => Address == null;
        private Timer _cmcTimer = new Timer();

        public static void RestartBackend()
        {
            if (Process.GetProcessesByName("WaltonWallet").Any())
            {
                Application.Current.Dispatcher.Invoke(() => MessageBox.Show("Please close the official Walton Wallet first.", "Wallet", MessageBoxButton.OK, MessageBoxImage.Information));
                Environment.Exit(0);
            }

            foreach (var p in Process.GetProcessesByName("walton"))
            {
                p.Kill();
            }

            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wallet\\walton.exe")))
            {
                Application.Current.Dispatcher.Invoke(() => MessageBox.Show("Failed to find the walton.exe file."));
                Environment.Exit(0);
            }

            //Init the wallet data dir
            if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wallet/walletnode")))
            {
                ProcessStartInfo genInfo = new ProcessStartInfo(
                    Path.Combine(Directory.GetCurrentDirectory(), "wallet\\walton.exe"),
                    "--datadir walletnode init genesis.json");

                genInfo.CreateNoWindow = true;
                genInfo.UseShellExecute = false;
                genInfo.WorkingDirectory = "wallet";
                var p = Process.Start(genInfo);

                p.WaitForExit();
            }

            //Start the backend process
            ProcessStartInfo info = new ProcessStartInfo(
                Path.Combine(Directory.GetCurrentDirectory(), "wallet\\walton.exe"),
                "--identity \"mockupwtcwallet\" --rpc --ipcdisable --rpcaddr 127.0.0.1 --rpccorsdomain \"*\"  --datadir \"walletnode\" --port \"30304\" --rpcapi \"admin,personal,db,eth,net,web3,miner\" --networkid 999 --rpcport 8546 console");

            info.CreateNoWindow = true;
            info.UseShellExecute = false;
            info.WorkingDirectory = "wallet";

            if (!File.Exists(info.FileName))
            {
                Application.Current.Dispatcher.Invoke(() => MessageBox.Show("Failed to find " + info.FileName, "Wallet"));
                Environment.Exit(0);

            }

            var process = Process.Start(info);



            if (process.HasExited)
            {
                Application.Current.Dispatcher.Invoke(() => MessageBox.Show("Failed to connect to the WTC RPC. The wallet will now close."));
                Environment.Exit(0);
            }

            ProcessID = process.Id;
        }

        private AppVM()
        {
            MaxMiningThreads = 16;
            MiningSliderValue = Environment.ProcessorCount;
            _cmcTimer.Interval = (int)TimeSpan.FromMinutes(5).TotalMilliseconds;
            _cmcTimer.Tick += CmcTimerOnTick;
            _cmcTimer.Start();
            CmcTimerOnTick(this, EventArgs.Empty);
        }

        public String USDPrice
        {
            get { return _usdPrice; }
            set
            {
                _usdPrice = value;
                OnPropertyChanged();
            }
        }

        public String BTCPrice
        {
            get { return _btcPrice; }
            set
            {
                _btcPrice = value;
                OnPropertyChanged();
            }
        }

        public decimal Percent24HourChange
        {
            get { return _percent24HourChange; }
            set
            {
                _percent24HourChange = value; 
                OnPropertyChanged();
            }
        }

        public Boolean IsPercentPositive
        {
            get { return _isPercentPositive; }
            set
            {
                if (_isPercentPositive == value)
                    return;
                _isPercentPositive = value; 
                OnPropertyChanged();
            }
        }

        private void CmcTimerOnTick(object sender, EventArgs eventArgs)
        {
            try
            {
                WebClient client = new WebClient();
                var tickerInfo = client.DownloadString("https://api.coinmarketcap.com/v1/ticker/walton/");

                var responses = JsonConvert.DeserializeObject<CMCResponse[]>(tickerInfo);
                var response = responses.FirstOrDefault();
                if(response == null)
                    return;

                var priceusd = decimal.Parse(response.price_usd);
                USDPrice = "$" + priceusd.ToString("N");
                BTCPrice = response.price_btc + " BTC";
                Percent24HourChange = decimal.Parse(response.percent_change_24h);
                IsPercentPositive = Percent24HourChange >= 0;
            }
            catch (Exception e)
            {

            }
        }

        internal static Web3Geth Geth { get; set; } = new Web3Geth(new WalletConfigurationService().Client);

        public static int? ProcessID { get; set; }

        private void LoadAccountsAsync(string publicKey)
        {
            Task.Run(() =>
            {
                LoadAccounts(publicKey);
            });
        }

        private void LoadAccounts(string publicKey)
        {
            int retryCount = 0;

            try
            {
                start:

                var accs = AppVM.Geth.Personal.ListAccounts.SendRequestAsync().GetAwaiter().GetResult();

                if (accs.Length > 1 || !accs.Contains(publicKey))
                {
                    if (retryCount++ < 20)
                    {
                        Thread.Sleep(500);
                        goto start;
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                        MessageBox.Show(
                            "There is a bug in the wallet - two wallets are loaded at once. \r\nPlease reopen the wallet to continue.",
                            "Wallet"));
                    Application.Current.Dispatcher.Invoke(() => Application.Current.Shutdown());
                }


                var acc = accs.FirstOrDefault(a => a == publicKey);
                if (acc == null)
                {
                    Application.Current.Dispatcher.Invoke(() => MessageBox.Show(
                        "Failed to automatically open the new wallet. Try manually opening the wallet.",
                        "Wallet"));
                    return;
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var wall = new AddressVM { PublicKey = acc };
                    Address = wall;
                    SelectTab("Wallet");
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                    MessageBox.Show("Error talking to the backend service: " + ex.Message, "Wallet"));
            }
        }


        public BaseCommand NewAccountCommand
        {
            get { return _newAccountCommand ?? (_newAccountCommand = new BaseCommand(NewAccount)); }
        }

        private void NewAccount(object obj)
        {
           NewAddressWindow window = new NewAddressWindow();
            var vm = new NewAddressVM((key) =>
            {
                window.Close();

                if (!String.IsNullOrWhiteSpace(key))
                {
                    LoadAccounts(key);
                }

            });
            window.DataContext = vm;
            window.Owner = Application.Current.MainWindow;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.ShowDialog();
        }

        public BaseCommand OpenAccountCommand
        {
            get { return _openAccountCommand ?? (_openAccountCommand = new BaseCommand(OpenAccount)); }
        }

        private void OpenAccount(object obj)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "All files (*.*)|*.*";
            dialog.Title = "Select Wallet Location";
            dialog.CheckFileExists = true;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if (dialog.FileName != "")
                {
                    if (!File.Exists(dialog.FileName))
                    {
                        MessageBox.Show("File at " + dialog.FileName + " does not exist.", "Wallet");
                    }
                    else
                    {
                        var service = new KeyStoreService();

                        string address = null;

                        try
                        {
                            address = service.GetAddressFromKeyStore(File.ReadAllText(dialog.FileName));
                        }
                        catch (Exception)
                        {
                            MessageBox.Show("Failed to read the wallet.", "Wallet");
                            return;
                        }

                        address = "0x" + address;

                        var nodeFolder = Task.Run(() => AppVM.Geth.Admin.Datadir.SendRequestAsync()).Result;

                        var keyStoreFolder = Path.Combine(nodeFolder, "keysotres");

                        if (!Directory.Exists(keyStoreFolder))
                        {
                            keyStoreFolder = Path.Combine(nodeFolder, "keystores");
                        }

                        if (!Directory.Exists(keyStoreFolder))
                        {
                            Application.Current.Dispatcher.Invoke(() => MessageBox.Show("Error finding keystore folder.", "Wallet", MessageBoxButton.OK, MessageBoxImage.Error));
                            return;
                        }

                        var files = Directory.GetFiles(keyStoreFolder, "*.*", SearchOption.TopDirectoryOnly);

                        foreach (var file in files)
                        {
                            if (File.Exists(file))
                            {
                                File.Delete(file);
                            }
                        }

                        File.Copy(dialog.FileName, Path.Combine(keyStoreFolder, Path.GetFileName(dialog.FileName)));

                        LoadAccounts(address);
                    }
                }
            }
        }

        public String MiningStatus
        {
            get { return _miningStatus; }
            set
            {
                if (_miningStatus == value)
                    return;
                _miningStatus = value;
                OnPropertyChanged();
            }
        }

        public String Status
        {
            get { return _status; }
            set
            {
                if (_status == value)
                    return;
                _status = value;
                OnPropertyChanged();
            }
        }
        private int _miningSliderValue;
        private string _miningSliderText;
        private string _usdPrice;
        private string _btcPrice;
        private decimal _percent24HourChange;
        private bool _isPercentPositive;

        public Int32 MaxMiningThreads { get; set; }

        public Int32 MiningSliderValue
        {
            get { return _miningSliderValue; }
            set
            {
                if (_miningSliderValue == value)
                    return;
                _miningSliderValue = value;
                OnPropertyChanged();

                MiningSliderText = MiningSliderValue + "/" + MaxMiningThreads;
            }
        }

        public String MiningSliderText
        {
            get { return _miningSliderText; }
            set
            {
                _miningSliderText = value;
                OnPropertyChanged();
            }
        }


        public Boolean HasAddress { get { return Address != null; } }

        public AddressVM Address
        {
            get { return _addressVm; }
            set
            {
                if (_addressVm == value)
                    return;

                if (_addressVm != null && _addressVm.IsMining)
                {
                    _addressVm.StopMiningCommand.Execute(null);
                }

                _addressVm = value;
                Status = "Loading wallet";
                Address.Load(() => Status = "Ready");
                OnPropertyChanged("ShowHome");
                OnPropertyChanged("HasAddress");
                OnPropertyChanged();
            }
        }


        public static AppVM Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new AppVM();
                    }
                }
                return _instance;
            }
        }

        public Action<string> SelectTab { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}