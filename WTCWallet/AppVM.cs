using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Nethereum.Geth;
using Nethereum.JsonRpc.Client;
using WTCWallet.Annotations;

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

        public static void RestartBackend()
        {
            //Get rid of any old lingering backend processes
            foreach (var process1 in Process.GetProcessesByName("wtcbackend"))
            {
                process1.Kill();
            }

            //Init the wallet data dir
            if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wallet/walletnode")))
            {
                ProcessStartInfo genInfo = new ProcessStartInfo(
                    Path.Combine(Directory.GetCurrentDirectory(), "wallet\\wtcbackend.exe"),
                    "--datadir walletnode init genesis.json");

                genInfo.CreateNoWindow = true;
                genInfo.UseShellExecute = false;
                genInfo.WorkingDirectory = "wallet";
                var p = Process.Start(genInfo);

                p.WaitForExit();
            }

            //Start the backend process
            ProcessStartInfo info = new ProcessStartInfo(
                Path.Combine(Directory.GetCurrentDirectory(), "wallet\\wtcbackend.exe"),
                "--identity \"development\" --rpc --ipcdisable --rpcaddr 127.0.0.1 --rpccorsdomain \"*\"  --datadir \"walletnode\" --port \"30304\" --rpcapi \"admin,personal,db,eth,net,web3,miner\" --networkid 999 --rpcport 8546 console");

            info.CreateNoWindow = true;
            info.UseShellExecute = false;
            info.WorkingDirectory = "wallet";

            if (!File.Exists(info.FileName))
            {
                MessageBox.Show("Failed to find " + info.FileName, "Wallet");
                Environment.Exit(0);

            }

            var process = Process.Start(info);



            if (process.HasExited)
            {
                MessageBox.Show("Failed to connect to the WTC RPC. The wallet will now close.");
                Environment.Exit(0);
            }

            ProcessID = process.Id;
        }

        private AppVM()
        {




            Task.Run(() =>
            {
                RestartBackend();
            });

            LoadAccounts();

            Status = "Ready";
            MiningStatus = "No miner running";
          }

        internal static Web3Geth Geth { get; set; } = new Web3Geth(new WalletConfigurationService().Client);

        public static int? ProcessID { get; set; }

        private void LoadAccounts(bool selectNewAccount = false)
        {
            int failCounter = 0;
            while (true)
            {
                if (failCounter >= 10)
                    break;

                try
                {
                    foreach (var s in AppVM.Geth.Personal.ListAccounts.SendRequestAsync().GetAwaiter().GetResult())
                    {
                        if (Addresses.Any(a => a.PublicKey == s))
                            continue;

                        var address = new AddressVM(UpdateMinerStatus) {PublicKey = s};

                        Addresses.Add(address);

                        if (selectNewAccount)
                        {
                            Address = address;
                        }
                    }



                    break;

                }
                catch (AggregateException ex) when (ex.GetBaseException() is RpcClientUnknownException)
                {
                    Thread.Sleep(1000);
                    failCounter++;

                    if (failCounter > 10)
                        throw;
                }
                catch (RpcClientUnknownException ex)
                {
                    Thread.Sleep(1000);
                    failCounter++;

                    if (failCounter > 10)
                        throw;
                }
            }

          


        }


        public BaseCommand NewAccountCommand
        {
            get { return _newAccountCommand ?? (_newAccountCommand = new BaseCommand(NewAccount)); }
        }

        private void NewAccount(object obj)
        {
           NewAddressWindow window = new NewAddressWindow();
            var vm = new NewAddressVM((success) =>
            {
                window.Close();

                if (success)
                {
                    LoadAccounts(true);
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
            
        }

        private void UpdateMinerStatus(AddressVM address, string val)
        {
            MiningStatus = val;

            foreach (var item in Addresses)
            {
                item.CanMine = !address.IsMining || address == item;
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

        public AddressVM Address
        {
            get { return _addressVm; }
            set
            {
                if (_addressVm == value)
                    return;
                _addressVm = value;
                Status = "Loading wallet";
                Address.Load(() => Status = "Ready");
                
                OnPropertyChanged();
            }
        }


        public ObservableCollection<AddressVM> Addresses { get; } = new ObservableCollection<AddressVM>();

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

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}