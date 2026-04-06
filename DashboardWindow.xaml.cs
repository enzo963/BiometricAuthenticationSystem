using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Bio_Athun_System.Views
{
    public partial class DashboardWindow : Window
    {
        // ════════════════════════════════════════════════════════════
        //  ✅ غيّر اسم السيرفر إذا احتجت
        //     مثال: "Server=DESKTOP-XXX\\SQLEXPRESS;..."
        // ════════════════════════════════════════════════════════════
        private readonly string _connectionString =
            @"Data Source=ENZO\SQLEXPRESS;Initial Catalog=BioAuthDB;Integrated Security=True;TrustServerCertificate=True;";

        private readonly int _currentUserId;
        private readonly string _currentUserName;
        private readonly string _currentUserRole;

        // ════════════════════════════════════════════════════════════
        //  Constructor
        // ════════════════════════════════════════════════════════════
        public DashboardWindow(int userId, string userName, string userRole)
        {
            InitializeComponent();

            _currentUserId = userId;
            _currentUserName = userName;
            _currentUserRole = userRole;

            // سحب النافذة بالماوس
            MouseLeftButtonDown += (s, e) =>
            {
                if (e.ButtonState == MouseButtonState.Pressed) DragMove();
            };

            // تحميل البيانات بعد ظهور النافذة
            Loaded += async (s, e) => await LoadDashboardAsync();
        }

        // ════════════════════════════════════════════════════════════
        //  تحميل كل البيانات
        // ════════════════════════════════════════════════════════════
        private async Task LoadDashboardAsync()
        {
            // عرض بيانات المستخدم في الـ Sidebar
            lblUserName.Text = _currentUserName;
            lblUserRole.Text = _currentUserRole;
            txtWelcome.Text = $"Welcome back, {_currentUserName}";
            txtAvatarLetter.Text = _currentUserName.Length > 0
                                   ? _currentUserName[0].ToString().ToUpper()
                                   : "A";

            // تحميل الإحصائيات والسجلات بالتوازي
            await Task.WhenAll(
                LoadStatsAsync(),
                LoadAccessLogsAsync()
            );
        }

        // ════════════════════════════════════════════════════════════
        //  البطاقات الثلاث
        // ════════════════════════════════════════════════════════════
        private async Task LoadStatsAsync()
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                // ── بطاقة 1: إجمالي المستخدمين + الجدد هذا الأسبوع ──
                var cmdUsers = new SqlCommand(@"
                    SELECT
                        COUNT(*) AS TotalUsers,
                        SUM(CASE WHEN RegisteredAt >= DATEADD(DAY,-7,GETDATE())
                                 THEN 1 ELSE 0 END) AS NewThisWeek
                    FROM Users
                    WHERE IsActive = 1;", conn);

                using (var rdr = await cmdUsers.ExecuteReaderAsync())
                {
                    if (await rdr.ReadAsync())
                    {
                        int total = rdr.GetInt32(0);
                        int newWeek = rdr.GetInt32(1);
                        Dispatcher.Invoke(() =>
                        {
                            txtTotalUsers.Text = total.ToString("N0");
                            txtNewThisWeek.Text = $"+{newWeek} هذا الأسبوع";
                        });
                    }
                }

                // ── بطاقة 2: دقة التعرف (آخر 1000 محاولة) ──────────
                var cmdAcc = new SqlCommand(@"
                    SELECT CAST(
                        100.0 * SUM(CASE WHEN IsSuccess=1 THEN 1 ELSE 0 END)
                        / NULLIF(COUNT(*),0)
                    AS DECIMAL(5,1)) AS AccuracyRate
                    FROM (
                        SELECT TOP 1000 IsSuccess
                        FROM AccessLogs
                        ORDER BY AttemptTime DESC
                    ) AS Recent;", conn);

                var accObj = await cmdAcc.ExecuteScalarAsync();
                decimal acc = (accObj != null && accObj != DBNull.Value)
                              ? Convert.ToDecimal(accObj) : 0m;
                Dispatcher.Invoke(() => txtAccuracy.Text = $"{acc}%");

                // ── بطاقة 3: المرفوضات والمشبوهة (آخر 24 ساعة) ──────
                var cmdRej = new SqlCommand(@"
                    SELECT
                        COUNT(*) AS RejectedTotal,
                        SUM(CASE WHEN IsSuspicious=1 THEN 1 ELSE 0 END) AS SuspiciousCount
                    FROM AccessLogs
                    WHERE IsSuccess = 0
                      AND AttemptTime >= DATEADD(HOUR,-24,GETDATE());", conn);

                using (var rdr = await cmdRej.ExecuteReaderAsync())
                {
                    if (await rdr.ReadAsync())
                    {
                        int rejected = rdr.GetInt32(0);
                        int suspicious = rdr.GetInt32(1);
                        Dispatcher.Invoke(() =>
                        {
                            txtRejected.Text = rejected.ToString();
                            txtSuspicious.Text = $"تم حظر {suspicious} محاولات مشبوهة";
                        });
                    }
                }
            }
            catch (SqlException ex)
            {
                Dispatcher.Invoke(() =>
                    MessageBox.Show($"خطأ في تحميل الإحصائيات:\n{ex.Message}",
                                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning));
            }
        }

        // ════════════════════════════════════════════════════════════
        //  جدول السجلات
        // ════════════════════════════════════════════════════════════
        private async Task LoadAccessLogsAsync()
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                var cmd = new SqlCommand(@"
                    SELECT TOP 50
                        FORMAT(al.AttemptTime,'yyyy-MM-dd  HH:mm:ss') AS [الوقت],
                        ISNULL(u.FullName, N'— مجهول —')              AS [المستخدم],
                        ISNULL(al.DeviceName, N'—')                   AS [الجهاز],
                        CASE WHEN al.IsSuccess = 1
                             THEN N'✔  ناجح'
                             ELSE N'✘  مرفوض' END                     AS [الحالة],
                        CAST(ROUND(al.ConfidenceScore*100,1)
                             AS VARCHAR(10)) + ' %'                   AS [التطابق],
                        CASE WHEN al.IsSuspicious = 1
                             THEN N'⚠ مشبوه'
                             ELSE N'' END                             AS [تنبيه]
                    FROM  AccessLogs al
                    LEFT  JOIN Users u ON al.UserId = u.UserId
                    ORDER BY al.AttemptTime DESC;", conn);

                var adapter = new SqlDataAdapter(cmd);
                var table = new DataTable();
                await Task.Run(() => adapter.Fill(table));

                Dispatcher.Invoke(() =>
                {
                    dgAccessLogs.ItemsSource = table.DefaultView;
                    lblNoData.Visibility = table.Rows.Count == 0
                                           ? Visibility.Visible
                                           : Visibility.Collapsed;
                });
            }
            catch (SqlException ex)
            {
                Dispatcher.Invoke(() =>
                    MessageBox.Show($"خطأ في تحميل السجلات:\n{ex.Message}",
                                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning));
            }
        }

        // ════════════════════════════════════════════════════════════
        //  زر Enroll New Face
        // ════════════════════════════════════════════════════════════
        private void btnEnrollFace_Click(object sender, RoutedEventArgs e)
        {
            // ── إذا أنشأت EnrollFaceWindow، فعّل هذا الكود ──
            // var win = new EnrollFaceWindow(_connectionString) { Owner = this };
            // win.Closed += async (s, args) => await LoadStatsAsync();
            // win.ShowDialog();

            // مؤقتاً حتى تجهز نافذة التسجيل
            MessageBox.Show("ميزة تسجيل الوجه قيد التطوير.",
                            "Enroll New Face",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
        }

        // ════════════════════════════════════════════════════════════
        //  زر Sign Out
        // ════════════════════════════════════════════════════════════
        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show(
                "هل تريد تسجيل الخروج؟",
                "تأكيد الخروج",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm == MessageBoxResult.Yes)
            {
                new LoginWindow().Show();
                Close();
            }
        }

        // ════════════════════════════════════════════════════════════
        //  أزرار التحكم بالنافذة
        // ════════════════════════════════════════════════════════════
        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void BtnClose_Click(object sender, RoutedEventArgs e)
            => Application.Current.Shutdown();

        private void btnEnrollFace_Checked(object sender, RoutedEventArgs e)
        {
            Face_Enrollmen face_Enrollmen = new Face_Enrollmen();
            face_Enrollmen.Show();  
            this.Close();
        }
    }
}