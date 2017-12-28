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
                OnPropertyChanged("FromShort");
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
                OnPropertyChanged("ToShort");
            }
        }

        public String FromShort
        {
            get
            {
                if (From?.Length <= 10 || From == null)
                {
                    return From;
                }

                return From.Substring(0, 5) + "..." + From.Substring(From.Length - 5);
            }
        }

        public String ToShort
        {
            get
            {
                if (To?.Length <= 10 || To == null)
                {
                    return To;
                }

                return To.Substring(0, 5) + "..." + To.Substring(To.Length - 5);
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

        public String HashShort
        {
            get
            {
                if (Hash.Length <= 30 || Hash == null)
                    return Hash;

                return Hash.Substring(0, 15) + "..." + Hash.Substring(Hash.Length - 15);
            }
        }

        public String Receiver
        {
            get { return _receiver; }
            set
            {
                if (_receiver == value)
                    return;
                _receiver = value;
                OnPropertyChanged();
                OnPropertyChanged("ReceiverShort");
            }
        }

        public String ReceiverShort
        {
            get
            {

                if (Receiver.Length <= 30 || Receiver == null)
                    return Receiver;

                return Receiver.Substring(0, 15) + "..." + Receiver.Substring(Receiver.Length - 15);

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