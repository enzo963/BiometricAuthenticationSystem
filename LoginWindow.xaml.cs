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

        private string connectionString = @"Data Source=ENZO\SQLEXPRESS;Initial Catalog=BioAuthDB;Integrated Security=True;TrustServerCertificate=True;";

        public LoginWindow()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // فتح نافذة التعرف على الوجه
            loginFaceWindow window2 = new loginFaceWindow();
            window2.Show();
            this.Close();
        }


        private void BtnSignIn_Click(object sender, RoutedEventArgs e)
        {
            string connString = @"Data Source=ENZO\SQLEXPRESS;Initial Catalog=BioAuthDB;Integrated Security=True;TrustServerCertificate=True";

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
                    // نطلب المعرف والاسم (وأي عمود آخر تحتاجه النافذة التالية)
                    string query = @"SELECT Id, FullName FROM Users 
                        WHERE LTRIM(RTRIM(Username)) = @Username 
                        AND LTRIM(RTRIM(Password)) = @Password";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", userName);
                        cmd.Parameters.AddWithValue("@Password", userPass);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read()) // إذا وجد مستخدم مطابق
                            {
                                // جلب البيانات من قاعدة البيانات
                                int id = reader.GetInt32(0);
                                string name = reader.GetString(1);
                                string userRole = "User"; // أو اجلبها من القارئ إذا كانت موجودة في الجدول

                                // تمرير البيانات للنافذة كما تطلب (UserID, Username, Role)
                                DashboardWindow dash = new DashboardWindow(id, name, userRole);

                                App.Current.MainWindow = dash;
                                dash.Show();
                                this.Close();
                            }
                            else
                            {
                                MessageBox.Show("Invalid Username or Password.");
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