using System;
using System.Windows;
using Bio_Athun_System; // تأكد من أن هذا يشير لاسم مشروعك لكي يرى Window2

namespace Bio_Athun_System.Views
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
            // فتح نافذة التعرف على الوجه
            Window2 window2 = new Window2();
            window2.Show();
            this.Close();
        }

        private void BtnSignIn_Click(object sender, RoutedEventArgs e)
        {
            string userName = txtUser.Text;
            string userPass = txtPass.Password;

            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userPass))
            {
                MessageBox.Show("Please enter Username and Password.");
                return;
            }

            // تجربة تسجيل دخول وهمية (ستستبدلها لاحقاً بالبحث في قاعدة البيانات)
            if (userName == "aaa" && userPass == "123")
            {
                Window2 form2 = new Window2();
                form2.Show();
                this.Close();
            }
            else
            {
                SignStatus.Text = "User not found or incorrect password";
            }
        }
    }
}