using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WTCWallet.Annotations;

namespace WTCWallet
{
    public class TransactionVM : INotifyPropertyChanged
    {
        private string _blockNumber;
        private string _receiver;
        private string _amount;
        private string _from;
        private string _to;
        private string _type;

        public String BlockNumber
        {
            get { return _blockNumber; }
            set
            {
                if (_blockNumber == value)
                    return;
                _blockNumber = value;
                OnPropertyChanged();
            }
        }

        public String From
        {
            get { return _from; }
            set
            {
                if (_from == value)
                    return;
                _from = value; 
                OnPropertyChanged();
            }
        }

        public String To
        {
            get { return _to; }
            set
            {
                if (_to == value)
                    return;
                _to = value;
                OnPropertyChanged();
            }
        }

        public String Type
        {
            get { return _type; }
            set
            {
                if (_type == value)
                    return;
                _type = value;
                OnPropertyChanged();
            }
        }

        public String Hash { get; set; }

        public String Receiver
        {
            get { return _receiver; }
            set
            {
                if (_receiver == value)
                    return;
                _receiver = value;
                OnPropertyChanged();
            }
        }

        public String Amount
        {
            get { return _amount; }
            set
            {
                if (_amount == value)
                    return;
                _amount = value;
                OnPropertyChanged();
            }
        }

        public TransactionVM()
        {
            
        }

        public Boolean IsPending { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}