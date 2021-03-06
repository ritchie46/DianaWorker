﻿using System;
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
    /// Interaction logic for insertJobWindow.xaml
    /// </summary>
    public partial class inputDialog : Window
    {
        public int queueIndex;

        public inputDialog()
        {
            InitializeComponent();
            textBox.Text = "1";
        }

        public string title
        {
            set
            {
                this.Title = value;
            }
            get
            {
                return this.Title;
            }
        }

        public Label label
            {
            set
            {
                this.queueIndexLabel = value;
            }
            get
            {
                return this.queueIndexLabel;
            }
        }

        private void rowIndexDialogOk(object sender, RoutedEventArgs e)
        {
            queueIndex = 0;
            Int32.TryParse(textBox.Text, out queueIndex);

            this.DialogResult = true;
        }
    }
}
