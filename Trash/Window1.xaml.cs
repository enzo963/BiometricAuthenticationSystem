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
        
        
        public LoginWindow()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            FaceEnrollmentWindow window2 = new FaceEnrollmentWindow();
            window2.Show();
            this.Close();
            

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
                FaceEnrollmentWindow form2 = new FaceEnrollmentWindow();
                
                form2.Show();
                this.Close();
            }
            else
            {
                SignStatus.Text = "Not found";
            }
        }
    }
}