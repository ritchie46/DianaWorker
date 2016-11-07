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

namespace ServerWorker
{
    /// <summary>
    /// Interaction logic for dialog.xaml
    /// </summary>
    public partial class Dialog : Window
    {
        public double conv_val = 1;
        public Dialog()
        {
            InitializeComponent();
        }

        private void dialogOk(object sender, RoutedEventArgs e)
        {
            var val_str = TextBoxConv.Text.ToString().Replace(".", ",");
            Double val;
            Double.TryParse(val_str, out val);
            if (val.GetType() == typeof(Double))
            {   
                conv_val = val;
                this.DialogResult = true;
            }
            
        }

    }
}
