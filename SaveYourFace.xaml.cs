using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using AForge.Video;
using AForge.Video.DirectShow;
using Bio_Athun_System.Views;
using System.Data.SqlClient;
using OpenCvSharp;
using OpenCvSharp.Face;
using OpenCvSharp.Extensions; 
using Window = System.Windows.Window; 


namespace Bio_Athun_System
{
    public partial class SaveYourFace : Window
    {
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;

        private int _currentUserId;
        private string _currentUserName;
        private string _currentUserRole;

        private Bitmap currentFrame;

        private string connectionString = @"Data Source=ENZO\SQLEXPRESS;Initial Catalog=BioAuthDB;Integrated Security=True;TrustServerCertificate=True";

        private int count = 0;             // عداد الصور الحالية
        private int maxImages = 30;        // عدد الصور المطلوب التقاطها
        private bool isBurstMode = false;  // حالة تفعيل الالتقاط السريع


        public SaveYourFace(int userID, string userName, string userRole)
        {
            InitializeComponent();
            _currentUserId = userID;
            _currentUserName = userName; 
            _currentUserRole = userRole;
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
                currentFrame = (Bitmap)eventArgs.Frame.Clone();

                if (isBurstMode && count < maxImages)
                {
                    using (Mat frameMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(currentFrame))
                    using (Mat grayFrame = new Mat())
                    {
                        Cv2.CvtColor(frameMat, grayFrame, ColorConversionCodes.BGR2GRAY);
                        Cv2.EqualizeHist(grayFrame, grayFrame);

                        // ✅ كشف الوجه أولاً قبل الحفظ
                        string cascadePath = System.IO.Path.Combine(
                            AppDomain.CurrentDomain.BaseDirectory,
                            "Resources",
                            "haarcascade_frontalface_default.xml");

                        using (var faceCascade = new CascadeClassifier(cascadePath))
                        {
                            var faces = faceCascade.DetectMultiScale(
                                grayFrame, 1.1, 5,
                                HaarDetectionTypes.ScaleImage,
                                new OpenCvSharp.Size(60, 60));

                            if (faces.Length > 0)
                            {
                                // ✅ نأخذ أول وجه فقط
                                var faceRect = faces[0];

                                using (Mat faceROI = new Mat(grayFrame, faceRect))
                                using (Mat resizedFace = new Mat())
                                {
                                    // ✅ توحيد الحجم للوجه فقط
                                    Cv2.Resize(faceROI, resizedFace, new OpenCvSharp.Size(100, 100));

                                    byte[] processedImgBytes = resizedFace.ToBytes(".jpg");
                                    SaveImageToDatabase(processedImgBytes, _currentUserId);
                                    count++;

                                    Dispatcher.BeginInvoke(new Action(() => {
                                        txtStatus.Text = $"✅ FACE CAPTURED: {count}/{maxImages}";
                                        txtStatus.Foreground = System.Windows.Media.Brushes.LimeGreen;

                                        // ✅ إيقاف تلقائي عند اكتمال الصور
                                        if (count >= maxImages)
                                        {
                                            isBurstMode = false;
                                            btnCapture.IsEnabled = true;
                                            txtStatus.Text = "✅ DONE! 30 FACE IMAGES SAVED SUCCESSFULLY";
                                            txtStatus.Foreground = System.Windows.Media.Brushes.Cyan;
                                            MessageBox.Show("تم حفظ 30 صورة بنجاح! يمكنك الآن تسجيل الدخول بالوجه.");
                                        }
                                    }));
                                }
                            }
                            else
                            {
                                // لم يكتشف وجه - أخبر المستخدم
                                Dispatcher.BeginInvoke(new Action(() => {
                                    txtStatus.Text = $"⚠️ NO FACE DETECTED - {count}/{maxImages} - قرّب وجهك";
                                    txtStatus.Foreground = System.Windows.Media.Brushes.Orange;
                                }));
                            }
                        }
                    }
                }

                // عرض الكاميرا (بدون تغيير)
                MemoryStream displayMs = new MemoryStream();
                currentFrame.Save(displayMs, System.Drawing.Imaging.ImageFormat.Bmp);
                displayMs.Seek(0, SeekOrigin.Begin);
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.StreamSource = displayMs;
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.EndInit();
                bi.Freeze();
                Dispatcher.BeginInvoke(new Action(() => { imgCamera.Source = bi; }));
            }
            catch (Exception ex) { Console.WriteLine("Error: " + ex.Message); }
        }





        // --- وظيفة التقاط الصورة وحفظها ---
        private void BtnCapture_Click(object sender, RoutedEventArgs e)
        {
            if (videoSource != null && videoSource.IsRunning)
            {
                // 1. تصفير العداد للبدء من جديد
                count = 0;

                // 2. تفعيل "وضع الانفجار" أو الالتقاط السريع
                isBurstMode = true;

                // 3. تحديث الواجهة لإعلام المستخدم
                txtStatus.Text = "STARTING AUTO-CAPTURE... PLEASE MOVE YOUR HEAD SLIGHTLY";
                txtStatus.Foreground = System.Windows.Media.Brushes.Orange;

                // تعطيل الزر مؤقتاً حتى ينتهي الالتقاط لمنع التداخل
                btnCapture.IsEnabled = false;
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
                    string query = "INSERT INTO Details ([UserId], [ImagePath], [CreatedAt]) VALUES (@uid, @img, @date)";

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