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

        // متغير لحفظ الإطار الحالي للتقاطه عند الطلب
        private Bitmap currentFrame;

        // سلسلة الاتصال بقاعدة البيانات (قم بتغييرها حسب بياناتك)
        private string connectionString = @"Data Source=ENZO\SQLEXPRESS;Initial Catalog=BioAuthDB;Integrated Security=True;TrustServerCertificate=True";

        public SaveYourFace()
        {
            InitializeComponent();
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
                        currentFrame.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                        imageBytes = ms.ToArray();
                    }

                    // 2. حفظ الصورة في قاعدة البيانات (مثال لمستخدم معين بـ ID = 1)
                    SaveImageToDatabase(imageBytes, 1);

                    txtStatus.Text = "FACE CAPTURED & SAVED!";
                    txtStatus.Foreground = System.Windows.Media.Brushes.LimeGreen;

                    MessageBox.Show("تم التقاط الصورة وحفظها بنجاح في النظام");
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
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                // افترضنا أن اسم الجدول Users وحقل الصورة UserImage
                string query = "UPDATE Users SET UserImage = @img WHERE UserId = @id";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@img", imageContent);
                cmd.Parameters.AddWithValue("@id", userId);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // --- أزرار التنقل ---
        private void BtnBackToDashboard_Click(object sender, RoutedEventArgs e)
        {
            StopCamera();
            DashboardWindow dashboard = new DashboardWindow(1, "User Name", "Admin");
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