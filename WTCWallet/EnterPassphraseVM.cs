using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using WTCWallet.Annotations;

namespace WTCWallet
{
    public class EnterPassphraseVM : INotifyPropertyChanged
    {
        private readonly Action _closeWindowAction;
        private BaseCommand _confirmCommand;
        public string PublicKey { get; }

        public EnterPassphraseVM(Action closeWindowAction, string publicKey)
        {
            _closeWindowAction = closeWindowAction;
            PublicKey = publicKey;
        }

        public Boolean Confirmed { get; set; }

        public BaseCommand ConfirmCommand
        {
            get { return _confirmCommand ?? (_confirmCommand = new BaseCommand(Confirm)); }
        }

        public Func<String> GetPassphrase { get; set; }

        private void Confirm(object obj)
        {
            Confirmed = true;

            _closeWindowAction();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}