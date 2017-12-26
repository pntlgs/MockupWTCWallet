using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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

namespace WTCWallet
{
    public class NewAddressVM : INotifyPropertyChanged
    {
        private readonly Action<Boolean> _closeWindow;
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

        public NewAddressVM(Action<Boolean> closeWindow)
        {
            _closeWindow = closeWindow;
            Init();
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
            _closeWindow(true);
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

            PublicAddress = AppVM.Geth.Personal.NewAccount.SendRequestAsync(Passphrase).GetAwaiter().GetResult();

            var service = new KeyStoreService();
            IKeyStoreService<Pbkdf2Params> ss = new KeyStorePbkdf2Service();
            //var test = service.EncryptAndGenerateDefaultKeyStoreAsJson(Passphrase, PrivateKeyBytes, PublicAddress);
            //service.DecryptKeyStoreFromFile();

            var nodeFolder = Task.Run(() => AppVM.Geth.Admin.Datadir.SendRequestAsync()).Result;

            var keyStoreFolder = Path.Combine(nodeFolder, "keysotres");

            if (!Directory.Exists(keyStoreFolder))
            {
                keyStoreFolder = Path.Combine(nodeFolder, "keystores");
            }

            if (!Directory.Exists(keyStoreFolder))
            {
                MessageBox.Show("Error finding keystore folder.");
                return;
            }

            var files = Directory.GetFiles(keyStoreFolder, "*.*", SearchOption.TopDirectoryOnly);

            string selectedFileText = null;

            foreach (var file in files)
            {
                selectedFileText = File.ReadAllText(file);

                var address = service.GetAddressFromKeyStore(selectedFileText);

                if (address == PublicAddress.Substring(2))
                {
                    break;
                }

                selectedFileText = null;
            }

            if (String.IsNullOrWhiteSpace(selectedFileText))
            {
                MessageBox.Show("Failed to create address", "Wallet", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            var bytes = service.DecryptKeyStoreFromJson(Passphrase, selectedFileText);

            PrivateKey = bytes.ToHex(true);

            ShowPrivateKey = true;
            ShowCreateButton = false;
            ShowCloseButton = true;
            ////using the simple key store service
            //var service = new KeyStoreService();
            //var test = service.EncryptAndGenerateDefaultKeyStoreAsJson(Passphrase, PrivateKeyBytes, PublicAddress);

            //var fileName = DateTime.UtcNow.ToString("UTC--yyyy-MM-dd_hh-mm-ss-fff") + ".wallet";

            //File.WriteAllText(Path.Combine(keyStoreFolder, fileName), test);

            //Thread.Sleep(5000);

            //int retryCount = 0;
            //bool success = false;

            //while (true)
            //{
            //    if (retryCount++ >= 20)
            //    {
            //        File.Delete(Path.Combine(keyStoreFolder, fileName));
            //        MessageBox.Show("Failed to create the address/wallet.", "Wallet", MessageBoxButton.OK, MessageBoxImage.Information);
            //        break;
            //    }

            //    try
            //    {
            //        var accounts = AppVM.Geth.Personal.ListAccounts.SendRequestAsync().GetAwaiter().GetResult();

            //        if (accounts.Contains(PublicAddress.ToLower()))
            //        {

            //           // var result = Task.Run(() =>
            //            //        AppVM.Geth.Personal.UnlockAccount.SendRequestAsync(PublicAddress.ToLower(), Passphrase, 1))
            //            //    .Result;

            //           // if (result)
            //           // {
            //                success = true;
            //                break;
            //           // }
            //        }
            //        else
            //        {
            //            Thread.Sleep(1000);
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        // AppVM.RestartBackend();

            //        //    Thread.Sleep(2000);
            //        Thread.Sleep(1000);
            //    }


            //}



            //   _closeWindow(success);
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