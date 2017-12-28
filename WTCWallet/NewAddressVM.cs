using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using NBitcoin;
using Nethereum.Geth;
using WTCWallet.Annotations;
using Nethereum.HdWallet;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.KeyStore;
using Nethereum.KeyStore.Model;
using Nethereum.Signer;
using Nethereum.Web3;
using Newtonsoft.Json;
using Application = System.Windows.Application;
using Clipboard = System.Windows.Clipboard;
using MessageBox = System.Windows.MessageBox;
using TextDataFormat = System.Windows.TextDataFormat;

namespace WTCWallet
{
    public class NewAddressVM : INotifyPropertyChanged
    {
        private readonly Action<String> _closeWindow;
        private string _seed;
        private string _confirmSeed;
        private string _publicAddress;
        private string _privateKey;
        private string _passphrase = "";
        private BaseCommand _createCommand;
        private BaseCommand _copySeedCommand;
        private BaseCommand _copyPublicKeyCommand;
        private BaseCommand _copyPrivateKeyCommand;
        private BaseCommand _copyPassphraseCommand;
        private BaseCommand _closeCommand;
        private bool _showPrivateKey;
        private bool _showCreateButton = true;
        private bool _showCloseButton;
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public NewAddressVM(Action<String> closeWindow)
        {
            _closeWindow = closeWindow;
            Init();
        }

        public BaseCommand SelectSaveLocationCommand
        {
            get { return _selectSaveLocationCommand ?? (_selectSaveLocationCommand = new BaseCommand(ShowFileDialog)); }
        }

        private void ShowFileDialog(object obj)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "All files (*.*)|*.*";
            saveFileDialog1.Title = "Select Wallet Save Location";
            saveFileDialog1.AddExtension = true;
            saveFileDialog1.FileName = Guid.NewGuid().ToString("N").Take(5).Select(a => a.ToString()).Aggregate((a,b) => a.ToString() + b.ToString()) + "-" + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + ".wallet";
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if (saveFileDialog1.FileName != "")
                {
                    SavePath = saveFileDialog1.FileName;
                }
            }
        }

        public string SavePath
        {
            get { return _savePath; }
            set
            {
                _savePath = value;
                OnPropertyChanged();
            }
        }

        public String Passphrase
        {
            get { return _passphrase; }
            set
            {
                if (_passphrase == value)
                    return;
                _passphrase = value; 
                OnPropertyChanged();
            }
        }

        public void Init()
        {
            //Wallet = new Wallet(Wordlist.English, WordCount.Eighteen);
            //Seed = Wallet.Words.Aggregate((a,b) => a + " " + b);
            //PublicAddress = Wallet.GetAddresses(1).First();
            //PrivateKeyBytes = Wallet.GetPrivateKey(PublicAddress);
            //PrivateKey = "0x" + PrivateKeyBytes.ToHex();
        }

        private byte[] PrivateKeyBytes { get; set; }

        public BaseCommand CreateCommand
        {
            get { return _createCommand ?? (_createCommand = new BaseCommand(Create)); }
        }

        public BaseCommand CopySeedCommand
        {
            get { return _copySeedCommand ??(_copySeedCommand = new BaseCommand(CopySeed)); }
        }

        public BaseCommand CopyPublicKeyCommand
        {
            get { return _copyPublicKeyCommand ?? (_copyPublicKeyCommand = new BaseCommand(CopyPublicKey)); }
        }

        public BaseCommand CopyPrivateKeyCommand
        {
            get { return _copyPrivateKeyCommand ?? (_copyPrivateKeyCommand = new BaseCommand(CopyPrivateKey)); }
        }

        private void CopyPrivateKey(object obj)
        {
            Clipboard.SetText(PrivateKey, TextDataFormat.Text);
        }

        public BaseCommand CopyPassphraseCommand
        {
            get { return _copyPassphraseCommand ?? (_copyPassphraseCommand = new BaseCommand(CopyPassphrase)); }
        }

        private void CopyPassphrase(object obj)
        {
            Clipboard.SetText(Passphrase, TextDataFormat.Text);
        }

        private void CopyPublicKey(object obj)
        {
            Clipboard.SetText(PublicAddress, TextDataFormat.Text);
        }

        private void CopySeed(object obj)
        {
            Clipboard.SetText(Seed, TextDataFormat.Text);
        }

        public BaseCommand CloseCommand
        {
            get { return _closeCommand ?? (_closeCommand = new BaseCommand(Close)); }
        }

        private void Close(object obj)
        {
            _closeWindow(PublicAddress);
        }

        public Boolean ShowPrivateKey
        {
            get { return _showPrivateKey; }
            set { _showPrivateKey = value; OnPropertyChanged();}
        }

        public Boolean ShowCreateButton
        {
            get { return _showCreateButton; }
            set
            {
                _showCreateButton = value; 
                OnPropertyChanged();
            }
        }

        public Boolean ShowCloseButton
        {
            get { return _showCloseButton; }
            set
            {
                _showCloseButton = value;
                OnPropertyChanged();
            }
        }

        private readonly object _lock = new object();
        private bool _isBusy;
        private string _savePath;
        private BaseCommand _selectSaveLocationCommand;

   
        public Boolean IsBusy
        {
            get { return _isBusy; }
            set
            {
                _isBusy = value;
                OnPropertyChanged();
                OnPropertyChanged("IsDoingNothing");
            }
        }

        public Boolean IsDoingNothing { get { return !IsBusy; } }



        private void Create(object obj)
        {
            if (String.IsNullOrWhiteSpace(Passphrase))
            {
                MessageBox.Show("Passphrase must be entered.", "Wallet", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (Passphrase.Length < 10)
            {
                MessageBox.Show("Passphrase must be at least 10 characters long.", "Wallet", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (String.IsNullOrWhiteSpace(SavePath))
            {
                MessageBox.Show("Please select a file location to save your wallet to.", "Wallet", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (File.Exists(SavePath))
            {
                if (MessageBox.Show(
                        "There is already an existing wallet at " + SavePath +
                        ".\r\nAre you sure you want to overwrite it?", "Wallet", MessageBoxButton.YesNo,
                        MessageBoxImage.Error) == MessageBoxResult.No)
                {
                    return;
                }
            }

            var folder = Directory.GetParent(SavePath);
            folder.Create();

            IsBusy = true;

            Task.Run(() =>
            {
                var publicAddress = AppVM.Geth.Personal.NewAccount.SendRequestAsync(Passphrase).GetAwaiter().GetResult();

                var service = new KeyStoreService();

                var nodeFolder = Task.Run(() => AppVM.Geth.Admin.Datadir.SendRequestAsync()).Result;

                var keyStoreFolder = Path.Combine(nodeFolder, "keysotres");

                if (!Directory.Exists(keyStoreFolder))
                {
                    keyStoreFolder = Path.Combine(nodeFolder, "keystores");
                }

                if (!Directory.Exists(keyStoreFolder))
                {
                    Application.Current.Dispatcher.Invoke(() => IsBusy = false);
                    Application.Current.Dispatcher.Invoke(() => MessageBox.Show("Error finding keystore folder.", "Wallet", MessageBoxButton.OK, MessageBoxImage.Error));
                    return;
                }

                var files = Directory.GetFiles(keyStoreFolder, "*.*", SearchOption.TopDirectoryOnly);

                string selectedFilePath = null;

                foreach (var file in files)
                {
                    var address = service.GetAddressFromKeyStore(File.ReadAllText(file));

                    if (address == publicAddress.Substring(2))
                    {
                        selectedFilePath = file;
                        break;
                    }
                }

                if (String.IsNullOrWhiteSpace(selectedFilePath))
                {
                    Application.Current.Dispatcher.Invoke(() => IsBusy = false);
                    Application.Current.Dispatcher.Invoke(() => MessageBox.Show("Failed to create wallet", "Wallet", MessageBoxButton.OK, MessageBoxImage.Error));
                    return;
                }

                File.Copy(selectedFilePath, SavePath, true);

                var bytes = service.DecryptKeyStoreFromJson(Passphrase, File.ReadAllText(SavePath));

                foreach (var file in files)
                {
                    if (file != selectedFilePath)
                    {
                        File.Delete(file);
                    }
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    PublicAddress = publicAddress;
                    PrivateKey = bytes.ToHex(true);
                    ShowPrivateKey = true;
                    ShowCreateButton = false;
                    ShowCloseButton = true;
                    IsBusy = false;
                });
            });
        }

        public string PrivateKey
        {
            get { return _privateKey; }
            set
            {
                _privateKey = value;
                OnPropertyChanged();
            }
        }

        public string PublicAddress
        {
            get { return _publicAddress; }
            set
            {
                _publicAddress = value;
                OnPropertyChanged();
            }
        }

        public String Seed
        {
            get { return _seed; }
            set
            {
                _seed = value;
                OnPropertyChanged();
            }
        }

        public String ConfirmSeed
        {
            get { return _confirmSeed; }
            set
            {
                _confirmSeed = value;
                OnPropertyChanged();
            }
        }

        private Wallet Wallet { get; set; }
    }
}