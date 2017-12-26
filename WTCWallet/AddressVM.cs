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
using Nethereum.Hex.HexTypes;
using WTCWallet.Annotations;

namespace WTCWallet
{
    public class AddressVM : INotifyPropertyChanged
    {
        private readonly Action<AddressVM, string> _updateMinerStatus;
        private bool _isTransactionsLoading;
        private bool _isTransactionsDone;
        private BaseCommand _startMiningCommand;
        private int _miningSliderValue;
        private string _miningSliderText;
        private bool _isMining;
        private bool _canMine = true;
        private BaseCommand _showQrCodeCommand;
        private BaseCommand _sendCommand;
        private string _sendAddress;
        private decimal _sendAmount;
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

        public TransactionVM Transaction
        {
            get { return _transaction; }
            set
            {
                _transaction = value; 
                OnPropertyChanged();
            }
        }

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

        public BaseCommand CopyTransactionHashCommand
        {
            get { return _copyTransactionHashCommand ?? (_copyTransactionHashCommand = new BaseCommand(CopyTransactionHash)); }
        }

        private void CopyTransactionHash(object obj)
        {
            Clipboard.SetText(Transaction.Hash, TextDataFormat.Text);
        }

        private void OpenInExplorer(object obj)
        {
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

            if (!Regex.IsMatch(SendAddress, "^0x[a-fA-F0-9]{40}$"))
            {
                MessageBox.Show("Send to Address is not a valid address.", "WTC Wallet", MessageBoxButton.OK,
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
                    Task.Run(() => AppVM.Geth.Personal.UnlockAccount.SendRequestAsync(PublicKey, vm.Passphrase, 60)).Result;

                if (!unlockAccountResult)
                {
                    MessageBox.Show("Failed to unlock your wallet.", "WTC Wallet", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

               

                var amount = SendAmount * (decimal)Math.Pow(10, 18);

                var id = Task.Run(() => AppVM.Geth.Eth.TransactionManager.SendTransactionAsync(PublicKey, SendAddress, new HexBigInteger(new BigInteger(amount)))).Result;

                Transactions.Insert(0, new TransactionVM {Hash = id, Amount = SendAmount.ToString(), Receiver = SendAddress, BlockNumber = "Pending", IsPending = true, Type = "Send WTC", From = PublicKey, To = SendAddress});

                ShowSendSuccess = true;
                SendAmount = 0;
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

        private System.Windows.Forms.Timer Timer
        {
            get
            {
                if (_timer == null)
                {
                    _timer = new System.Windows.Forms.Timer
                    {
                        Enabled = false,
                        Interval = (int) TimeSpan.FromSeconds(15).TotalMilliseconds
                    };

                    _timer.Tick += delegate
                    {
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

        private void LoadTransactions(Boolean showSpinner, Action loaded = null)
        {
            lock (_loadLock)
            {
                if (IsTransactionsLoading)
                    return;

                IsTransactionsLoading = true;
                ShowTransactionsSpinner = showSpinner;
                IsTransactionsDone = !showSpinner && Transactions.Count > 0;
                IsTransactionsEmpty = !showSpinner && Transactions.Count == 0;
            }

            Task.Run(() =>
            {
                try
                {
                    foreach (var transactionVm in Service.GetLatestTransactions(PublicKey, 1).ToList())
                    {
                        var tr = Transactions.FirstOrDefault(t => t.Hash == transactionVm.Hash);
                        if (tr == null)
                        {
                            Application.Current.Dispatcher.Invoke(() => Transactions.Add(transactionVm));
                        }
                        else
                        {
                            tr.BlockNumber = transactionVm.BlockNumber;
                            tr.Amount = transactionVm.Amount;
                            tr.Receiver = transactionVm.Receiver;
                            tr.IsPending = false;
                        }
                    }

                    LastRefreshed = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToString("HH:mm:ss");

                    if (loaded != null)
                        Application.Current.Dispatcher.Invoke(() => loaded());
                }
                finally
                {
                    lock (_loadLock)
                    {
                        IsTransactionsLoading = false;
                        ShowTransactionsSpinner = false;
                        IsTransactionsDone = Transactions.Count > 0;
                        IsTransactionsEmpty = Transactions.Count == 0;
                    }

                }
            });
        }

        public Decimal SendAmount
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

        public AddressVM(Action<AddressVM, string> updateMinerStatus)
        {
            _updateMinerStatus = updateMinerStatus;

            MaxMiningThreads = Environment.ProcessorCount;
            MiningSliderValue = MaxMiningThreads;
        }

        public BaseCommand StartMiningCommand
        {
            get { return _startMiningCommand ?? (_startMiningCommand = new BaseCommand(StartMining) {Label = "Start Mining"}); }
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

        public Boolean IsMining
        {
            get { return _isMining; }
            set { _isMining = value; }
        }

        private void StartMining(object obj)
        {
            if (StartMiningCommand.Label == "Start Mining")
            {
                IsMining = true;
                _updateMinerStatus(this, "Starting miner for " + PublicKey);
                //       Service.StartMining(PublicKey, Password, MiningSliderValue);
                StartMiningCommand.Label = "Stop Mining";
                string includeS = MiningSliderValue == 1 ? "" : "s";
                _updateMinerStatus(this, $"Running {MiningSliderValue} mining thread{includeS} for " + PublicKey);
            }
            else
            {
                IsMining = false;
                _updateMinerStatus(this, "Stoping miner for " + PublicKey);
                //   Service.StopMining(PublicKey, Password);
                StartMiningCommand.Label = "Start Mining";
                _updateMinerStatus(this, "No miner running");
            }
        }

        public Int32 MaxMiningThreads { get; set; }

        public Boolean IsTransactionsDone
        {
            get { return _isTransactionsDone; }
            set { _isTransactionsDone = value; OnPropertyChanged(); }
        }

        public Int32 MiningSliderValue
        {
            get { return _miningSliderValue; }
            set
            {
                if (_miningSliderValue == value)
                    return;
                _miningSliderValue = value; 
                OnPropertyChanged();

                MiningSliderText = MiningSliderValue + "/" + 8;
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
            LoadBalance();

            LoadTransactions(true);



            Timer.Start();

        }

        private void LoadBalance()
        {
            var info = Task.Run(() => Service.GetAccountsInfo(PublicKey)).Result;
            Balance = info.WTC;
        }

        public object PublicQRCode
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