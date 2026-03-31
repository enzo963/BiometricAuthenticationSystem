using Bio_Athun_System;
using Microsoft.Data.SqlClient;
using System.Windows;

namespace BioAthunSystem.Views
{
    public partial class RegisterWindow : Window
    {
        // سلسلة الاتصال بقاعدة البيانات (عدلها حسب سيرفرك)
        private string connectionString = "Server=.; Database=BioAuthDB; Trusted_Connection=True; TrustServerCertificate=True;";

        public RegisterWindow()
        {
            InitializeComponent();
        }

        // حدث الضغط على زر إنشاء الحساب
        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            string fullName = txtFullName.Text.Trim();
            string username = txtUser.Text.Trim();
            string password = txtPass.Password;

            // 1. التحقق من إدخال كافة البيانات
            if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please fill in all fields!", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // 2. التحقق من أن اسم المستخدم غير موجود مسبقاً
                    string checkUserQuery = "SELECT COUNT(*) FROM Users WHERE Username = @User";
                    using (SqlCommand checkCmd = new SqlCommand(checkUserQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@User", username);
                        int userCount = (int)checkCmd.ExecuteScalar();

                        if (userCount > 0)
                        {
                            MessageBox.Show("Username already exists! Please choose another one.", "Registration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    // 3. إدراج المستخدم الجديد في جدول Users
                    // ملاحظة: في المشاريع الحقيقية يفضل تشفير كلمة المرور (Hashing)
                    string insertQuery = @"INSERT INTO Users (FullName, Username, PasswordHash, IsActive, CreatedAt) 
                                         VALUES (@Name, @User, @Pass, 1, GETDATE());
                                         SELECT SCOPE_IDENTITY();"; // لجلب الـ ID الذي تم إنشاؤه للتو

                    using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                    {
                        insertCmd.Parameters.AddWithValue("@Name", fullName);
                        insertCmd.Parameters.AddWithValue("@User", username);
                        insertCmd.Parameters.AddWithValue("@Pass", password);

                        // جلب الـ ID الخاص بالمستخدم الجديد لاستخدامه لاحقاً في ربط الصور
                        object result = insertCmd.ExecuteScalar();

                        if (result != null)
                        {
                            MessageBox.Show("Account created successfully! Now let's set up your Face ID.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                            // الانتقال لصفحة التقاط الوجه (Window2) وتمرير الـ ID
                            // ملاحظة: يمكنك تمرير الـ ID عبر Constructor أو متغير static
                            int newUserId = Convert.ToInt32(result);

                            // هنا نفتح صفحة الوجه
                            Window2 faceEnrollment = new Window2();
                            // يمكنك إضافة خاصية في Window2 لاستقبال الـ newUserId
                            faceEnrollment.Show();

                            this.Close(); // إغلاق صفحة التسجيل
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Database Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // العودة لصفحة تسجيل الدخول
        private void BtnBackToLogin_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow login = new LoginWindow();
            login.Show();
            this.Close();
        }

        // إغلاق النافذة
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow login = new LoginWindow();
            login.Show();
            this.Close();
        }
    }
}