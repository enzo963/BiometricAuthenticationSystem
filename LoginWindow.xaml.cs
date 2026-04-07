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
            string username = txtUser.Text; // قراءة الاسم من الحقل x:Name="txtUser"

            if (string.IsNullOrEmpty(username))
            {
                SignStatus.Text = "Please enter your username first!";
                return;
            }

            try
            {
                string connString = @"Data Source=ENZO\SQLEXPRESS;Initial Catalog=BioAuthDB;Integrated Security=True;TrustServerCertificate=True;";
                using (var conn = new Microsoft.Data.SqlClient.SqlConnection(connString))
                {
                    conn.Open();
                    // البحث عن الـ ID والاسم الكامل
                    string query = "SELECT Id FROM Users WHERE FullName = @name OR Username = @name";
                    using (var cmd = new Microsoft.Data.SqlClient.SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", username);
                        object result = cmd.ExecuteScalar();

                        if (result != null)
                        {
                            int userId = Convert.ToInt32(result);

                            // إرسال الـ ID الحقيقي للنافذة التالية
                            loginFaceWindow window2 = new loginFaceWindow(userId);
                            window2.Show();
                            this.Close();
                        }
                        else
                        {
                            SignStatus.Text = "User not found!";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Connection Error: " + ex.Message);
            }
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