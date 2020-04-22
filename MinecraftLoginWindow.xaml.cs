using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
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

namespace Lanceur_Modder_v2
{
    /// <summary>
    /// Logique d'interaction pour MinecraftLoginWindow.xaml
    /// </summary>
    public partial class MinecraftLoginWindow : Window
    {
        Regex mailRegex = new Regex(".+@.+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public string Email { get; set; }
        public SecureString Passwd { get; set; }
        public bool SavePass { get; set; }
        public MinecraftLoginWindow()
        {
            InitializeComponent();
        }

        private void BT_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void BT_OK_Click(object sender, RoutedEventArgs e)
        {
            Passwd = TB_MDP.SecurePassword;
            SavePass = (bool)SavePassword.IsChecked;
            Email = TB_Email.Text;
            this.DialogResult = true;
        }

        private void Check_Text_Username(object sender, TextChangedEventArgs e)
        {
            if (mailRegex.IsMatch(TB_Email.Text) && TB_MDP.Password.Length > 0)
            {
                BT_OK.IsEnabled = true;
            }
            else
            {
                BT_OK.IsEnabled = false;
            }
        }

        private void TB_MDP_PasswordChanged(object sender, RoutedEventArgs e)
        {
            Check_Text_Username(null, null);
        }
    }
}
