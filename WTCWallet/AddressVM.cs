using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using WTCWallet.Annotations;
using Timer = System.Windows.Forms.Timer;

namespace WTCWallet
{
    public class AddressVM : INotifyPropertyChanged
    {
        private bool _isTransactionsLoading;
        private bool _isTransactionsDone = true;
        private BaseCommand _startMiningCommand;
  
        private bool _isMining;
        private bool _canMine = true;
        private BaseCommand _showQrCodeCommand;
        private BaseCommand _sendCommand;
        private string _sendAddress;
        private decimal? _sendAmount;
        private BaseCommand _copyBalanceCommand;
        private BaseCommand _copyPublicKeyCommand;
        private TransactionVM _transaction;
        private BaseCommand _openExplorerCommand;
        private BaseCommand _copyTransactionHashCommand;
        private bool _showSendSuccess;
        private string _lastRefreshed;
        private bool _isTransactionsEmpty;
        private System.Windows.Forms.Timer _timer;
        public String PublicKey { get; set; }
        public String Password { get; set; }

        public AccountToken Balance
        {
            get { return _balance; }
            set
            {
                _balance = value; 
                OnPropertyChanged();
            }
        }

        public ObservableCollection<TransactionVM> Transactions { get; } = new ObservableCollection<TransactionVM>();
        public ObservableCollection<BlockVM> MinerBlocks { get; } = new ObservableCollection<BlockVM>();

        public BlockVM MinerBlock
        {
            get { return _minerBlock; }
            set
            {
                _minerBlock = value;
                OnPropertyChanged();
                OnPropertyChanged("HasBlock");
            }
        }

        public Boolean HasBlock => MinerBlock != null;

        public TransactionVM Transaction
        {
            get { return _transaction; }
            set
            {
                _transaction = value; 
                OnPropertyChanged();
                OnPropertyChanged("HasTransaction");
            }
        }

        public Boolean HasTransaction {  get { return Transaction != null; } }

        public String LastRefreshed
        {
            get { return _lastRefreshed; }
            set
            {
                _lastRefreshed = value;
                OnPropertyChanged();
            }
        }

        public BaseCommand OpenExplorerCommand
        {
            get { return _openExplorerCommand ?? (_openExplorerCommand= new BaseCommand(OpenInExplorer)); }
        }

        public BaseCommand CopySenderAddressCommand
        {
            get { return _copySenderAddressCommand ?? (_copySenderAddressCommand = new BaseCommand(CopySenderAddress)); }
        }

        public BaseCommand CopyReceiverAddressCommand
        {
            get { return _copyReceiverAddressCommand ?? (_copyReceiverAddressCommand = new BaseCommand(CopyReceiverAddress)); }
        }

        private void CopyReceiverAddress(object obj)
        {
            if (Transaction == null)
                return;

            Clipboard.SetText(Transaction.To, TextDataFormat.Text);
        }

        private void CopySenderAddress(object obj)
        {
            if (Transaction == null)
                return;

            Clipboard.SetText(Transaction.From, TextDataFormat.Text);
        }

        public BaseCommand CopyTransactionHashCommand
        {
            get { return _copyTransactionHashCommand ?? (_copyTransactionHashCommand = new BaseCommand(CopyTransactionHash)); }
        }

        private void CopyTransactionHash(object obj)
        {
            if (Transaction == null)
                return;

            Clipboard.SetText(Transaction.Hash, TextDataFormat.Text);
        }

        private void OpenInExplorer(object obj)
        {
            if (Transaction == null)
                return;

            if (Transaction.IsPending)
            {
                MessageBox.Show("This transaction was still pending last time it was checked. \r\nIf the explorer shows an 'Internal Server Error' it means the transaction has not been included in a block yet.", "WTC", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            Process.Start($"http://waltonchain.net/transaction/{Transaction.Hash}");
        }

        public BaseCommand SendCommand
        {
            get { return _sendCommand ?? (_sendCommand = new BaseCommand(Send)); }
        }

        public BaseCommand EntireBalanceCommand
        {
            get { return _entireBalanceCommand ?? (_entireBalanceCommand= new BaseCommand(EntireBalance)); }
        }

        private void EntireBalance(object obj)
        {
            var price = AppVM.Geth.Eth.GasPrice.SendRequestAsync().GetAwaiter().GetResult();
            var balance = AppVM.Geth.Eth.GetBalance.SendRequestAsync(PublicKey).GetAwaiter().GetResult();
            var maxAmount = balance - (price.Value * 121001);

            var val = (decimal)maxAmount / (decimal)Math.Pow(10, 18);
            if (val < 0)
            {
                MessageBox.Show("You do not have enough WTC to cover the trasaction fee.\r\nAdditional amount required: " + val * -1 + " WTC", "Wallet", MessageBoxButton.OK, MessageBoxImage.Information);
                val = 0;
            }
            SendAmount = val;
        }

        private void Send(object obj)
        {
            if (NodeCount == 0)
            {
                MessageBox.Show("You are connected to 0 nodes. Wait until you are connected to at least one node before sending.", "Wallet", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!Regex.IsMatch(SendAddress, "^0x[a-fA-F0-9]{40}$"))
            {
                MessageBox.Show("Send to Address is not a valid address.", "WTC Wallet", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            if (SendAmount == null)
            {
                MessageBox.Show("You must enter an amount to send.", "WTC Wallet", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            if (SendAmount < 0)
            {
                MessageBox.Show("Negative amounts can not be sent.", "WTC Wallet", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            EnterPassphraseWindow window = new EnterPassphraseWindow();
            window.Owner = Application.Current.MainWindow;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            EnterPassphraseVM vm = new EnterPassphraseVM(() => window.Close(), PublicKey);
            window.DataContext = vm;
            window.ShowDialog();

            if (!vm.Confirmed)
            {
                return;
            }

     
            try
            {
                var unlockAccountResult =
                    Task.Run(() => AppVM.Geth.Personal.UnlockAccount.SendRequestAsync(PublicKey, vm.GetPassphrase(), 60)).Result;

                if (!unlockAccountResult)
                {
                    MessageBox.Show("Failed to unlock your wallet.", "WTC Wallet", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

               

                var amount = SendAmount.Value * (decimal)Math.Pow(10, 18);

                var id = Task.Run(() => AppVM.Geth.Eth.TransactionManager.SendTransactionAsync(PublicKey, SendAddress, new HexBigInteger(new BigInteger(amount)))).Result;

         

                Transactions.Insert(0, new TransactionVM {Hash = id, Amount = SendAmount.ToString(), To = SendAddress, BlockNumber = "Pending", IsPending = true, Type = "Sent WTC", From = PublicKey, Date = DateTime.Now});

                if (SendAddress.ToUpper() == PublicKey.ToUpper())
                {
                    Transactions.Insert(0, new TransactionVM { Hash = id, Amount = SendAmount.ToString(), To = SendAddress, BlockNumber = "Pending", IsPending = true, Type = "Received WTC", From = PublicKey, Date = DateTime.Now });
                }

                Task.Run(() => AppVM.Geth.Personal.LockAccount.SendRequestAsync(PublicKey)).Wait();

                ShowSendSuccess = true;
                SendAmount = null;
                SendAddress = String.Empty;

                Task.Run(() =>
                {
                    Thread.Sleep(6000);
                    ShowSendSuccess = false;
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to send the transaction: " + ex.GetBaseException().Message, "WTC Wallet", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public System.Windows.Forms.Timer MiningTimer
        {
            get
            {
                if (_miningTimer == null)
                {
                    _miningTimer = new System.Windows.Forms.Timer
                    {
                        Enabled = false,
                        Interval = (int)TimeSpan.FromSeconds(5).TotalMilliseconds
                    };

                    _miningTimer.Tick += delegate
                    {
                        RefreshHashrate();
                    };
                }


                return _miningTimer;
            }
        }

        private void RefreshHashrate()
        {
            if (!IsMining)
            {
                MiningTimer.Stop();
                HashRate = null;
                return;
            }

            try
            {
                CustomMinerApiService minerService = new CustomMinerApiService(AppVM.Geth.Client);
                var hashRate = (int)new Int32Converter().ConvertFromString(minerService.MinerHashrate.SendRequestAsync().GetAwaiter().GetResult());
                HashRate = hashRate;
            }
            catch (Exception e)
            {
             
            }
        }

        public int? HashRate
        {
            get { return _hashRate; }
            set
            {
                if (_hashRate == value)
                    return;
                _hashRate = value;
                OnPropertyChanged();
                OnPropertyChanged("HasHashRate");
            }
        }

        public Boolean HasHashRate => HashRate != null;

        private System.Windows.Forms.Timer RefreshTimer
        {
            get
            {
                if (_timer == null)
                {
                    _timer = new System.Windows.Forms.Timer
                    {
                        Enabled = false,
                        Interval = (int) TimeSpan.FromSeconds(13).TotalMilliseconds
                    };

                    _timer.Tick += delegate
                    {
                        LoadBalance();
                        LoadTransactions(false);
                    };
                }

                return _timer;
            }
        }

        private readonly object _loadLock = new object();
        private bool _showTransactionsSpinner;
        private BaseCommand _entireBalanceCommand;
        private AccountToken _balance;
        private BaseCommand _copySenderAddressCommand;
        private BaseCommand _stopMiningCommand;
        private string _miningStatus = "MINING STATUS: NOT MINING";
        private string _nodeStatus = "CONNECTED NODES: 0";
        private BlockVM _minerBlock;
        private BaseCommand _copyReceiverAddressCommand;
        private Timer _miningTimer;
        private int? _hashRate;

        private void LoadTransactions(Boolean showSpinner, Action loaded = null)
        {
            lock (_loadLock)
            {
                if (IsTransactionsLoading)
                    return;

                IsTransactionsLoading = true;
                ShowTransactionsSpinner = showSpinner;
                IsTransactionsDone = !showSpinner;// && Transactions.Count > 0;
                IsTransactionsEmpty = !showSpinner && Transactions.Count == 0;
            }

            Task.Run(() =>
            {
                try
                {
                    foreach (var transactionVm in Service.GetLatestTransactions(PublicKey, 1).Reverse().ToList())
                    {
                        var tr = Transactions.Where(t => t.Hash == transactionVm.Hash).ToArray();

                        if (!tr.Any())
                        {
                            var detail = AppVM.Geth.Eth.Transactions.GetTransactionByHash.SendRequestAsync(transactionVm.Hash).GetAwaiter()
                                .GetResult();

                            if (detail == null)
                                continue;

                            var block =
                                AppVM.Geth.Eth.Blocks.GetBlockWithTransactionsByHash.SendRequestAsync(detail.BlockHash)
                                    .GetAwaiter().GetResult();

                            if (block == null)
                                continue;

                            transactionVm.From = detail.From;
                            transactionVm.To = detail.To;
                            transactionVm.Type = detail.From == PublicKey ? "Sent WTC" : "Received WTC";

                            transactionVm.Date = new DateTime(1970, 1, 1).AddSeconds((double)block.Timestamp.Value);

                            if (detail.From == detail.To)
                            {
                                var clone = transactionVm.Clone();
                                clone.Type = "Received WTC";
                                Application.Current.Dispatcher.Invoke(() => Transactions.Insert(0, clone));
                            }

                            Application.Current.Dispatcher.Invoke(() => Transactions.Insert(0, transactionVm));
                        }
                        else
                        {
                            foreach (var t in tr.Where(t => t.IsPending))
                            {
                                t.BlockNumber = transactionVm.BlockNumber;
                                t.Amount = transactionVm.Amount;
                                t.IsPending = false;
                            }
                        }
                    }

                    foreach (var blockVM in Service.GetMinerBlocks(PublicKey, 1).Reverse().ToList())
                    {
                        var tr = MinerBlocks.FirstOrDefault(t => t.Hash == blockVM.Hash);
                        if (tr == null)
                        {
                            var block =
                                AppVM.Geth.Eth.Blocks.GetBlockWithTransactionsByHash.SendRequestAsync(blockVM.Hash)
                                    .GetAwaiter().GetResult();

                            if (block == null)
                                continue;

                            blockVM.Date = new DateTime(1970, 1, 1).AddSeconds((double)block.Timestamp.Value);
                            blockVM.TransactionCount = block.Transactions.Length;

                            Application.Current.Dispatcher.Invoke(() => MinerBlocks.Insert(0, blockVM));
                        }
                    }

                    LastRefreshed = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToString("HH:mm:ss");

                    try
                    {
                        NodeCount = AppVM.Geth.Admin.Peers.SendRequestAsync().GetAwaiter().GetResult().Children()
                            .Count();
                        NodeStatus = "CONNECTED NODES: " + NodeCount;
                    }
                    catch (RpcClientUnknownException ex)
                    {
                        NodeCount = 0;
                        NodeStatus = "The wallet service is no longer running. Please restart the wallet.";

                        if (IsMining)
                        {
                            MiningStatus = "MINING STATUS: NOT MINING (DISCONNECTED)";
                        }
                    }

                    if (loaded != null)
                        Application.Current.Dispatcher.Invoke(() => loaded());
                }
                finally
                {
                    lock (_loadLock)
                    {
                        IsTransactionsLoading = false;
                        ShowTransactionsSpinner = false;
                        IsTransactionsDone = true;
                        IsTransactionsEmpty = Transactions.Count == 0;
                    }

                }
            });
        }

        public Decimal? SendAmount
        {
            get { return _sendAmount; }
            set
            {
                _sendAmount = value; 
                OnPropertyChanged();
            }
        }

        public string SendAddress
        {
            get { return _sendAddress; }
            set
            {
                _sendAddress = value;
                OnPropertyChanged();
            }
        }

        public Boolean IsTransactionsEmpty
        {
            get { return _isTransactionsEmpty; }
            set
            {
                _isTransactionsEmpty = value; 
                OnPropertyChanged();
            }
        }

        public Boolean ShowTransactionsSpinner
        {
            get { return _showTransactionsSpinner; }
            set
            {
                _showTransactionsSpinner = value;
                OnPropertyChanged();
            }
        }

        public Boolean IsTransactionsLoading
        {
            get { return _isTransactionsLoading; }
            set { _isTransactionsLoading = value; OnPropertyChanged();}
        }

        public AddressVM()
        {

        }

        public BaseCommand StartMiningCommand
        {
            get { return _startMiningCommand ?? (_startMiningCommand = new BaseCommand(StartMining) {Label = "Start Mining"}); }
        }

        public BaseCommand StopMiningCommand
        {
            get { return _stopMiningCommand ?? (_stopMiningCommand = new BaseCommand(StopMining) {Label = "Stop Mining"}); }
        }

        private void StopMining(object obj)
        {
            IsMining = false;
            MiningTimer.Stop();
            HashRate = null;

            try
            {


                AppVM.Geth.Miner.Stop.SendRequestAsync().GetAwaiter().GetResult();

                //var unlockAccountResult =
                //    Task.Run(() => AppVM.Geth.Personal.LockAccount.SendRequestAsync(PublicKey)).Result;
            }
            catch (Exception)
            {
                
            }
            //    Service.StopMining(PublicKey, Password);
            StartMiningCommand.Label = "Start Mining";
            MiningStatus = "MINING STATUS: NOT MINING";
        }

        public Boolean CanMine
        {
            get { return _canMine; }
            set
            {
                if (_canMine == value)
                    return;
                _canMine = value;
                OnPropertyChanged();
            }
        }

        public BaseCommand ShowQRCodeCommand
        {
            get { return _showQrCodeCommand ?? (_showQrCodeCommand = new BaseCommand(ShowQRCode)); }
        }

        private void ShowQRCode(object obj)
        {
            PublicQRCodeWindow window = new PublicQRCodeWindow();
            PublicQRCodeVM vm = new PublicQRCodeVM(PublicKey);
            window.Owner = Application.Current.MainWindow;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.DataContext = vm;
            window.Show();
        }

        public BaseCommand CopyBalanceCommand
        {
            get { return _copyBalanceCommand ?? (_copyBalanceCommand = new BaseCommand(CopyBalance)); }
        }

        public BaseCommand CopyPublicKeyCommand
        {
            get { return _copyPublicKeyCommand ?? (_copyPublicKeyCommand = new BaseCommand(CopyPublicKey)); }
        }

        private void CopyPublicKey(object obj)
        {
            Clipboard.SetText(PublicKey, TextDataFormat.Text);
        }

        private void CopyBalance(object obj)
        {
            Clipboard.SetText(Balance.Balance.ToString(), TextDataFormat.Text);
        }

        public String MiningStatus
        {
            get { return _miningStatus; }
            set
            {
                _miningStatus = value; 
                OnPropertyChanged();
            }
        }

        public Boolean IsMining
        {
            get { return _isMining; }
            set
            {
                _isMining = value; 
                OnPropertyChanged();
                OnPropertyChanged("IsNotMining");
            }
        }

        public Boolean IsNotMining { get { return !IsMining; } }

        private void StartMining(object obj)
        {
            if (NodeCount == 0)
            {
                MessageBox.Show("You are connected to 0 nodes. Wait until you are connected to at least one node before mining.", "Wallet", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (AppVM.Instance.MiningSliderValue > 8)
            {
                if (MessageBox.Show($"You have selected {AppVM.Instance.MiningSliderValue} threads to mine with. This is likely to consume most of your system resources. Continue?", "Wallet", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    return;
            }

            int value = AppVM.Instance.MiningSliderValue;

            //EnterPassphraseWindow window = new EnterPassphraseWindow();
            //window.Owner = Application.Current.MainWindow;
            //window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            //EnterPassphraseVM vm = new EnterPassphraseVM(() => window.Close(), PublicKey);
            //window.DataContext = vm;
            //window.ShowDialog();

            //if (!vm.Confirmed)
            //{
            //    return;
            //}

            //var unlockAccountResult =
            //    Task.Run(() => AppVM.Geth.Personal.UnlockAccount.SendRequestAsync(PublicKey, vm.Passphrase, 1000)).Result;

            try
            {
                AppVM.Geth.Miner.Start.SendRequestAsync(value).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to start mining. Try closing and reopening this program to see if that fixes it.", "Wallet", MessageBoxButton.OK);
                return;
            }


    
            Thread.Sleep(2000);

            if (AppVM.ProcessID.HasValue)
            {
                try
                {
                    Process.GetProcessById(AppVM.ProcessID.Value);
                }
                catch (ArgumentException e)
                {
                    MessageBox.Show("The mining process has crashed. Please reopen the wallet.", "Wallet", MessageBoxButton.OK);
                    Application.Current.Shutdown();
                }
            }

            IsMining = true;

            MiningTimer.Start();


            //   Service.StartMining(PublicKey, Password, MiningSliderValue);
            StartMiningCommand.Label = "Stop Mining";
            MiningStatus = "MINING STATUS: MINING";
        }



        public Boolean IsTransactionsDone
        {
            get { return _isTransactionsDone; }
            set { _isTransactionsDone = value; OnPropertyChanged(); }
        }

        public Boolean ShowSendSuccess
        {
            get { return _showSendSuccess; }
            set
            {
                _showSendSuccess = value; 
                OnPropertyChanged();
            }
        }

        private WTCWalletService Service { get; } = new WTCWalletService(new WalletConfigurationService());

        public void Load(Action loaded)
        {
            PublicQRCode = PublicQRCodeVM.GetBitmap(PublicKey);

            LoadBalance();

            LoadTransactions(true);

            RefreshTimer.Start();
        }

        private void LoadBalance()
        {
            var info = Task.Run(() => Service.GetAccountsInfo(PublicKey)).Result;
            Balance = info.WTC;
        }

        public Int32 NodeCount { get; set; }
        public string NodeStatus
        {
            get { return _nodeStatus; }
            set
            {
                _nodeStatus = value;
                OnPropertyChanged();
            }
        }

        public BitmapImage PublicQRCode
        {
            get;
            set;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}