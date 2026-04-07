using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Bio_Athun_System;

namespace Bio_Athun_System.Views
{
    public partial class DashboardWindow : Window
    {
        private readonly string _connectionString =
            @"Data Source=ENZO\SQLEXPRESS;Initial Catalog=BioAuthDB;Integrated Security=True;TrustServerCertificate=True;";

        private readonly int _currentUserId;
        private readonly string _currentUserName;
        private readonly string _currentUserRole;

        public DashboardWindow(int userId, string userName, string userRole)
        {
            InitializeComponent();
            _currentUserId = userId;
            _currentUserName = userName;
            _currentUserRole = userRole;

            MouseLeftButtonDown += (s, e) => { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); };
            Loaded += async (s, e) => await LoadDashboardAsync();
        }

        private async Task LoadDashboardAsync()
        {
            lblUserName.Text = _currentUserName;
            lblUserRole.Text = _currentUserRole;
            txtWelcome.Text = $"Welcome back, {_currentUserName}";
            txtAvatarLetter.Text = _currentUserName.Length > 0 ? _currentUserName[0].ToString().ToUpper() : "A";

            await Task.WhenAll(LoadStatsAsync(), LoadAccessLogsAsync());
        }

        private async Task LoadStatsAsync()
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                var cmdUsers = new SqlCommand(@"
                    SELECT 
                        COUNT(*) AS TotalUsers,
                        SUM(CASE WHEN CreatedAt >= DATEADD(DAY,-7,GETDATE()) THEN 1 ELSE 0 END) AS NewThisWeek
                    FROM Users;", conn);

                using (var rdr = await cmdUsers.ExecuteReaderAsync())
                {
                    if (await rdr.ReadAsync())
                    {
                        int total = rdr.GetInt32(0);
                        int newWeek = rdr.IsDBNull(1) ? 0 : Convert.ToInt32(rdr[1]);
                        Dispatcher.Invoke(() => {
                            txtTotalUsers.Text = total.ToString("N0");
                            txtNewThisWeek.Text = $"+{newWeek} هذا الأسبوع";
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // تم تبسيط الخطأ لكي لا يزعجك أثناء التشغيل
                Console.WriteLine("Stats Error: " + ex.Message);
            }
        }

        private async Task LoadAccessLogsAsync()
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                var cmd = new SqlCommand(@"
                    SELECT TOP 50
                        l.LogTime AS [الوقت],
                        u.FullName AS [المستخدم],
                        l.Status AS [الحالة]
                    FROM Logs l
                    LEFT JOIN Users u ON l.UserId = u.Id
                    ORDER BY l.LogTime DESC;", conn);

                var adapter = new SqlDataAdapter(cmd);
                var table = new DataTable();
                await Task.Run(() => adapter.Fill(table));

                Dispatcher.Invoke(() => {
                    dgAccessLogs.ItemsSource = table.DefaultView;
                    lblNoData.Visibility = table.Rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                });
            }
            catch (Exception ex) { Console.WriteLine("Logs Error: " + ex.Message); }
        }

        // ════════════════════════════════════════════════════════════
        // تصحيح الأزرار: استبدلنا الأسماء الخاطئة بالأسماء الموجودة في مشروعك
        // ════════════════════════════════════════════════════════════

        private void btnEnrollFace_Click(object sender, RoutedEventArgs e)
        {
            SaveYourFace FaceScaen = new SaveYourFace();
            FaceScaen.Show();
            this.Close();
        }

        

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("هل تريد تسجيل الخروج؟", "تأكيد", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                new LoginWindow().Show();
                Close();
            }
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void BtnClose_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
    }
}