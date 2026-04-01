using System;
using System.Security.RightsManagement;
using System.Windows;
using Bio_Athun_System; 
using Microsoft.Data.SqlClient;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

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
            string connString = @"Data Source=ENZO\SQLEXPRESS;Initial Catalog=BioAuthDB;Integrated Security=True;TrustServerCertificate=True;";
            string userName = txtUser.Text.Trim();
            string userPass = txtPass.Password.Trim();

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

                    // ✅ LTRIM/RTRIM تحذف الفراغات من قاعدة البيانات
                    string query = @"SELECT COUNT(*) FROM Users 
                                WHERE LTRIM(RTRIM(Username)) = @Username 
                                AND LTRIM(RTRIM(Password)) = @Password";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", userName);
                        cmd.Parameters.AddWithValue("@Password", userPass);

                        int count = Convert.ToInt32(cmd.ExecuteScalar());

                        if (count > 0)
                        {
                            DashboardWindow dash = new DashboardWindow();
                            dash.Show();
                            this.Close();
                        }
                        else
                        {
                            // 🔍 للتشخيص: شوف كم مستخدم موجود
                            string debugQuery = "SELECT COUNT(*) FROM Users";
                            using (SqlCommand debugCmd = new SqlCommand(debugQuery, conn))
                            {
                                int total = Convert.ToInt32(debugCmd.ExecuteScalar());
                                //SignStatus.Text = $"Not found. Total users in DB: {total}";

                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
        }
    }
}