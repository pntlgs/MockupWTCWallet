using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using WTCWallet.Annotations;

namespace WTCWallet
{
    public class BlockVM : INotifyPropertyChanged
    {
        private BaseCommand _openExplorerCommand;
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public String BlockNumber { get; set; }

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


        public String Reward { get; set; }

        public String Nounce { get; set; }

        public String Size { get; set; }
        public string Difficulty { get; set; }

        public BaseCommand OpenExplorerCommand
        {
            get { return _openExplorerCommand ?? (_openExplorerCommand = new BaseCommand(OpenExplorer)); }
        }

        public DateTime Date { get; set; }
        public int TransactionCount { get; set; }

        private void OpenExplorer(object obj)
        {
            Process.Start($"http://waltonchain.net/block/{BlockNumber}");
        }
    }
}