using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TicTacToeNetwork
{
    /// <summary>
    /// Interaction logic for ConnectionHost.xaml
    /// </summary>
    public partial class ConnectionHost : Window
    {
        public ConnectionHost()
        {
            InitializeComponent();

            DataObject.AddPastingHandler(tbIp1, OnPaste);
            DataObject.AddPastingHandler(tbIp2, OnPaste);
            DataObject.AddPastingHandler(tbIp3, OnPaste);
            DataObject.AddPastingHandler(tbIp4, OnPaste);
            tbIp4.Focus();
            //tbIp1.Focusable = false;
            //tbIp2.Focusable = false;
            //tbIp3.Focusable = false;
            //tbIp4.Focusable = false;
        }

        // Обработка Paste
        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (e.SourceDataObject.GetDataPresent(typeof(string)))
            {
                string str = e.SourceDataObject.GetData(typeof(string)) as string;
                if (!Regex.IsMatch(str, @"^[\d\.]*$") || str.Split('.').Length != 4)
                {
                    popup1.IsOpen = true;
                    e.Handled = true;
                }
                else
                    IpAdress = str;
            }
            else
                e.Handled = true;
        }

        // Ввод IP адресса
        private void tbIp_TextChanged(object sender, TextChangedEventArgs e) =>  StringVerification(sender as TextBox);


        // Проверка строки
        private void StringVerification(TextBox tb)
        {
            if (!string.IsNullOrWhiteSpace(tb.Text))
            {
                if (Convert.ToInt32(tb.Text) > 255)
                {
                    tb.Text = "255";
                    tb.CaretIndex = 3;
                }

                if (Convert.ToInt32(tb.Text) < 0)
                    tb.Text = "0";

                if(tb.Text.Length == 3 && tb.Tag.ToString() != "4")
                {
                    TextBox tbNew = this.FindName("tbIp" + (Convert.ToInt32(tb.Tag) + 1)) as TextBox;
                    tbNew.Focus();
                    tbNew.SelectAll();
                }
            }
        }

        // Обработка стрелок и Backspace
        private void tb_PreviewKeyDown(object sender, KeyEventArgs e)
       {
            TextBox tb = sender as TextBox;
            if (tb != null)
            {
                // Обработка кнопок Backspace и Left для перехода на предыдущий октет
                if ((e.Key == Key.Back && tb.Text.Length == 0 || e.Key == Key.Left && tb.SelectionStart == 0 ) && Convert.ToInt32(tb.Tag) != 1)
                {
                    TextBox tbNew = this.FindName("tbIp" + (Convert.ToInt32(tb.Tag) - 1)) as TextBox;
                    tbNew.Focus();
                    tbNew.SelectionStart = tbNew.Text.Length;

                    if (e.Key == Key.Back)
                        tb.Text = "0";

                    e.Handled = true;
                }

                // Обработка кнопки Right для перехода на следующий октет
                if (e.Key == Key.Right && tb.SelectionStart == tb.Text.Length && Convert.ToInt32(tb.Tag) != 4)
                {
                    TextBox tbNew = this.FindName("tbIp" + (Convert.ToInt32(tb.Tag) + 1)) as TextBox;
                    tbNew.Focus();
                    tbNew.SelectionStart = 0;
                    e.Handled = true;
                }
            }          
        }

        // Обработка нажатия точки
        private void tb_TextInput(object sender, TextCompositionEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb != null && e.Text == "." && tb.SelectionStart != 0 && !tb.IsSealed && Convert.ToInt32(tb.Tag) != 4)
            {
                TextBox tbNew = this.FindName("tbIp" + (Convert.ToInt32(tb.Tag) + 1)) as TextBox;
                tbNew.Focus();
                tbNew.SelectAll();
            }
            e.Handled = !(Char.IsDigit(e.Text, 0));
        }

        // IP адрес
        public string IpAdress
        {
            get
            {
                return tbIp1.Text + "." + tbIp2.Text + "." + tbIp3.Text + "." + tbIp4.Text;
            }
            set
            {
                try
                {
                    string[] splitValues = value.Split('.');
                    tbIp1.Text = splitValues[0];
                    tbIp2.Text = splitValues[1];
                    tbIp3.Text = splitValues[2];
                    tbIp4.Text = splitValues[3];
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        // Кнопка ОК
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }

        // Кнопка отмена
        private void Button_Click_1(object sender, RoutedEventArgs e) => this.Close();
    }
}
