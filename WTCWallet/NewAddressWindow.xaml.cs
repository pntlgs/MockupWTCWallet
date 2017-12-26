using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MahApps.Metro.Controls;

namespace WTCWallet
{
    /// <summary>
    /// Interaction logic for NewAddressWindow.xaml
    /// </summary>
    public partial class NewAddressWindow : MetroWindow
    {
        public NewAddressWindow()
        {
            InitializeComponent();
        }

        private void UIElement_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (createButton.Visibility == Visibility.Visible)
                {
                    createButton.Command.Execute(null);
                }
            }
        }
    }
}
