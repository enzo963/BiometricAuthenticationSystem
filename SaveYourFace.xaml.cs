using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using AForge.Video;
using AForge.Video.DirectShow;
using Bio_Athun_System.Views;
using System.Data.SqlClient; // تأكد من إضافة هذا للتعامل مع قاعدة البيانات

namespace Bio_Athun_System
{
    public partial class SaveYourFace : Window
    {
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;

        private int _currentUserId;
        private string _currentUserName;
        private string _currentUserRole;

        // متغير لحفظ الإطار الحالي للتقاطه عند الطلب
        private Bitmap currentFrame;

        // سلسلة الاتصال بقاعدة البيانات (قم بتغييرها حسب بياناتك)
        private string connectionString = @"Data Source=ENZO\SQLEXPRESS;Initial Catalog=BioAuthDB;Integrated Security=True;TrustServerCertificate=True";

        public SaveYourFace(int userID)
        {
            InitializeComponent();
            _currentUserId = userID;
        }

        private void BtnStartCamera_Click(object sender, RoutedEventArgs e)
        {
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            if (videoDevices.Count > 0)
            {
                videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
                videoSource.NewFrame += VideoSource_NewFrame;
                videoSource.Start();

                cameraPlaceholder.Visibility = Visibility.Collapsed;
                imgCamera.Visibility = Visibility.Visible;
                scanCanvas.Visibility = Visibility.Visible;

                Storyboard scanAnim = (Storyboard)this.Resources["ScanLineAnim"];
                scanAnim.Begin();

                txtStatus.Text = "SCANNING ACTIVE...";
                txtStatus.Foreground = System.Windows.Media.Brushes.Cyan;
            }
            else
            {
                MessageBox.Show("لم يتم العثور على كاميرا متصلة!");
            }
        }

        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                // حفظ الإطار الحالي في المتغير لغرض الالتقاط
                currentFrame = (Bitmap)eventArgs.Frame.Clone();

                MemoryStream ms = new MemoryStream();
                currentFrame.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                ms.Seek(0, SeekOrigin.Begin);

                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.StreamSource = ms;
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.EndInit();

                bi.Freeze();
                Dispatcher.BeginInvoke(new Action(() => {
                    imgCamera.Source = bi;
                }));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        // --- وظيفة التقاط الصورة وحفظها ---
        private void BtnCapture_Click(object sender, RoutedEventArgs e)
        {
            if (currentFrame != null)
            {
                try
                {
                    // 1. تحويل الصورة إلى مصفوفة بايتات (Bytes) لقاعدة البيانات
                    byte[] imageBytes;
                    using (MemoryStream ms = new MemoryStream())
                    {
                        // نحفظ الصورة بصيغة Jpeg لتقليل الحجم داخل القاعدة
                        currentFrame.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                        imageBytes = ms.ToArray();
                    }

                    // 2. إرسال البايتات إلى قاعدة البيانات
                    // ملاحظة: رقم 1 هو ID تجريبي للمستخدم
                    SaveImageToDatabase(imageBytes, _currentUserId);

                    txtStatus.Text = "FACE SAVED SUCCESSFULLY!";
                    MessageBox.Show("تم حفظ بصمة الوجه بنجاح");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("خطأ أثناء الحفظ: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("يرجى تشغيل الكاميرا أولاً");
            }
        }

        private void SaveImageToDatabase(byte[] imageContent, int userId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    // نستخدم INSERT لإضافة سجل جديد في جدول Details
                    string query = "INSERT INTO Details (UserId, ImagePath, CreatedAt) " +
                                   "VALUES (@uid, @img, @date)";

                    SqlCommand cmd = new SqlCommand(query, conn);

                    // تحديد الأنواع بدقة يضمن عدم حدوث أخطاء تحويل مرة أخرى
                    cmd.Parameters.Add("@uid", System.Data.SqlDbType.Int).Value = userId;
                    cmd.Parameters.Add("@img", System.Data.SqlDbType.VarBinary).Value = imageContent;
                    cmd.Parameters.Add("@date", System.Data.SqlDbType.DateTime).Value = DateTime.Now;

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    // لإعلامك بنجاح العملية في نافذة الـ Output أثناء البرمجة
                    System.Diagnostics.Debug.WriteLine("Done! Image saved to SQL.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل الحفظ في قاعدة البيانات: " + ex.Message);
            }
        }

        // --- أزرار التنقل ---
        private void BtnBackToDashboard_Click(object sender, RoutedEventArgs e)
        {
            StopCamera();
            DashboardWindow dashboard = new DashboardWindow(_currentUserId , _currentUserName, _currentUserRole);
            dashboard.Show();
            this.Close();
        }

        private void BtnBackToLogin_Click(object sender, RoutedEventArgs e)
        {
            StopCamera();
            LoginWindow login = new LoginWindow();
            login.Show();
            this.Close();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            StopCamera();
            this.Close();
        }

        private void StopCamera()
        {
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
                videoSource.NewFrame -= VideoSource_NewFrame;
                videoSource = null;
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            StopCamera();
            base.OnClosing(e);
        }
    }
}