using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WTCWallet.Annotations;

namespace WTCWallet
{
    public class TransactionVM : INotifyPropertyChanged
    {
        private string _blockNumber;
        private string _amount;
        private string _from;
        private string _to;
        private string _type;
        private DateTime _date;

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

        public Boolean HasLoadedAdditioinal { get; set; }

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
                if (From?.Length <= 20 || From == null)
                {
                    return From;
                }

                return From.Substring(0, 10) + "..." + From.Substring(From.Length - 10);
            }
        }

        public String ToShort
        {
            get
            {
                if (To?.Length <= 20 || To == null)
                {
                    return To;
                }

                return To.Substring(0, 10) + "..." + To.Substring(To.Length - 10);
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

        public DateTime Date
        {
            get { return _date; }
            set
            {
                if (_date == value)
                    return;
                _date = value;
                OnPropertyChanged();
            }
        }

        public TransactionVM Clone()
        {
            var vm = new TransactionVM();
            vm.Date = Date;
            vm.From = From;
            vm.Hash = Hash;
            vm.To = To;
            vm.Type = Type;
            vm.Amount = Amount;
            vm.BlockNumber = BlockNumber;
            vm.HasLoadedAdditioinal = HasLoadedAdditioinal;
            vm.IsPending = IsPending;
            return vm;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}