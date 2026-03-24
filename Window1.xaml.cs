using System;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Runtime.InteropServices;
using System.Drawing;
using Bio_Athun_System;

namespace BioAthunSystem.Views
{
    public partial class LoginWindow : Window
    {
        private VideoCapture _capture; // كائن الكاميرا
        private DispatcherTimer _timer; // مؤقت لتحديث الصورة

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            


        }

        private void BtnSignIn_Click(object sender, RoutedEventArgs e)
        {
            string userName = txtUser.Text;
            string userPass = txtPass.Password;



            if (string.IsNullOrEmpty(userName) && string.IsNullOrEmpty(userPass))
            {
                MessageBox.Show("Enter User Name and Pass plz..");
            }

            if (userName == "aaa" && userPass == "123")
            {
                Window2 form2 = new Window2();
                form2.Show();
                this.Close();
            }
            else
            {
                
            }
        }
    }
}