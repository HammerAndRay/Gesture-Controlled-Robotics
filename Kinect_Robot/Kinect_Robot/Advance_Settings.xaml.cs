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

namespace Kinect_Robot
{
    /// <summary>
    /// Interaction logic for Advance_Settings.xaml
    /// </summary>
    public partial class Advance_Settings : Window
    {
        string hostname_new { get; set; }
        string user_new { get; set; }
        string password_new { get; set; }
        Boolean save = false;
        public Advance_Settings()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            hostname_new = new_hostname.Text;
            user_new = new_user.Text;
            password_new = new_password.Text;
            save = true;
            this.Close();
        }

        public string hostname() 
        {
            return hostname_new;        
        }

        public string user()
        {
            return user_new;
        }

        public string password()
        {
            return password_new;
        }
        public Boolean want_save() 
        {
            return save;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
