using System;
using System.Security.RightsManagement;
using System.Windows;
using Bio_Athun_System; 
using Microsoft.Data.SqlClient;



namespace Bio_Athun_System.Views
{
    public partial class LoginWindow : Window
    {

        private string connectionString = @"Data Source=ENZO\SQLEXPRESS;Initial Catalog=BioAuthDB;Integrated  Certificate=True";
        

        public LoginWindow()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // فتح نافذة التعرف على الوجه
            FaceEnrollmentWindow window2 = new FaceEnrollmentWindow();
            window2.Show();
            this.Close();
        }


        private void BtnSignIn_Click(object sender, RoutedEventArgs e)
        {
            // 1. تصحيح الـ Connection String (تأكد من وجود Security و TrustServerCertificate)
            string connString = @"Server=ENZO\SQLEXPRESS;Database=BioAuthDB;Integrated Security=True;TrustServerCertificate=True;";

            string userName = txtUser.Text.Trim(); // إزالة أي فراغات
            string userPass = txtPass.Password;

            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userPass))
            {
                MessageBox.Show("Please enter Username and Password.");
                return;
            }

            using (SqlConnection conn = new SqlConnection(connString))
            {
                try
                {
                    conn.Open();
                    // 2. تأكد أن Password هو اسم العمود الحقيقي في جدولك
                    string query = "SELECT  FROM Users WHERE Username = @Username AND Password = @Password";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", userName);
                        cmd.Parameters.AddWithValue("@Password", userPass);

                        int count = Convert.ToInt32(cmd.ExecuteScalar());

                        if (count > 0)
                        {
                            // نجاح!
                            DashboardWindow dash = new DashboardWindow();
                            dash.Show();
                            this.Close();
                        }
                        else
                        {
                            SignStatus.Text = "Invalid login credentials.";
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("خطأ في الاتصال: " + ex.Message);
                }
            }
        }
    }
}