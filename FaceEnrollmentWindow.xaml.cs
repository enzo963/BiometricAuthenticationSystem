using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using OpenCvSharp.Face;

namespace Bio_Athun_System.Views
{
    public partial class FaceEnrollmentWindow : System.Windows.Window
    {
        private VideoCapture? capture;
        private Mat frame = new Mat();
        private DispatcherTimer timer;
        private CascadeClassifier? faceCascade;

        private LBPHFaceRecognizer? recognizer;
        private bool isTrained = false;
        private Dictionary<int, string> userNames = new Dictionary<int, string>();

        public FaceEnrollmentWindow()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            string cascadePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "haarcascade_frontalface_default.xml");
            if (System.IO.File.Exists(cascadePath))
                faceCascade = new CascadeClassifier(cascadePath);

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(33);
            timer.Tick += Timer_Tick;

            TrainModel();
        }

        private void TrainModel()
        {
            try
            {
                // 1. إعداد القوائم
                List<Mat> faceImages = new List<Mat>();
                List<int> faceLabels = new List<int>();
                userNames.Clear(); // تفريغ القاموس القديم

                // 2. سلسلة الاتصال (عدلها حسب اسم السيرفر عندك)
                string connString = @"Data Source=ENZO\SQLEXPRESS;Initial Catalog=BioAuthDB;Integrated Security=True;TrustServerCertificate=True;";

                using (var conn = new Microsoft.Data.SqlClient.SqlConnection(connString))
                {
                    conn.Open();

                    // أ. جلب أسماء المستخدمين لربط الـ ID بالاسم الظاهر على الشاشة
                    string nameQuery = "SELECT Id, FullName FROM Users WHERE IsActive = 1";
                    using (var cmd = new Microsoft.Data.SqlClient.SqlCommand(nameQuery, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            userNames.Add(reader.GetInt32(0), reader.GetString(1));
                        }
                    }

                    // ب. جلب مسارات الصور لتدريب المحرك (Recognizer)
                    string imgQuery = "SELECT UserId, ImagePath FROM Details WHERE IsActive = 1";
                    using (var cmd = new Microsoft.Data.SqlClient.SqlCommand(imgQuery, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int userId = reader.GetInt32(0);
                            string path = reader.GetString(1);

                            if (System.IO.File.Exists(path))
                            {
                                // تحميل الصورة وتحويلها لرمادي وتصغيرها لضمان سرعة المعالجة
                                Mat img = Cv2.ImRead(path, ImreadModes.Grayscale);
                                Cv2.Resize(img, img, new OpenCvSharp.Size(100, 100));

                                faceImages.Add(img);
                                faceLabels.Add(userId);
                            }
                        }
                    }
                }

                // 3. بدء تدريب المحرك إذا وجدت صور
                if (faceImages.Count > 0)
                {
                    recognizer = LBPHFaceRecognizer.Create();
                    // تحويل القوائم إلى مصفوفات ليفهمها OpenCV
                    recognizer.Train(faceImages, faceLabels);
                    isTrained = true;
                    // MessageBox.Show("تم تحميل بيانات المستخدمين وتدريب النظام بنجاح!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في الربط مع قاعدة البيانات: " + ex.Message);
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            try
            {
                if (capture == null || !capture.IsOpened()) return;
                if (!capture.Grab()) return;
                capture.Retrieve(frame);
                if (frame == null || frame.Empty()) return;

                using (Mat display = frame.Clone())
                {
                    if (faceCascade != null && !faceCascade.Empty())
                    {
                        using (Mat gray = new Mat())
                        {
                            Cv2.CvtColor(display, gray, ColorConversionCodes.BGR2GRAY);
                            Cv2.EqualizeHist(gray, gray);

                            // تحديد OpenCvSharp.Size لحل مشكلة التداخل
                            var faces = faceCascade.DetectMultiScale(gray, 1.1, 5, HaarDetectionTypes.ScaleImage, new OpenCvSharp.Size(60, 60));

                            foreach (var faceRect in faces)
                            {
                                Cv2.Rectangle(display, faceRect, Scalar.FromRgb(53, 141, 230), 3);

                                if (isTrained && recognizer != null)
                                {
                                    using (Mat faceRegion = new Mat(gray, faceRect))
                                    {
                                        Cv2.Resize(faceRegion, faceRegion, new OpenCvSharp.Size(100, 100));

                                        // حل مشكلة Predict للحصول على ID و Distance
                                        int outLabel = -1;
                                        double outConfidence = 0;
                                        recognizer.Predict(faceRegion, out outLabel, out outConfidence);

                                        string label = "Unknown";
                                        if (outConfidence < 100) // العتبة
                                        {
                                            label = userNames.ContainsKey(outLabel) ? userNames[outLabel] : "Authorized";
                                        }

                                        Cv2.PutText(display, $"{label} ({Math.Round(outConfidence)})",
                                            new OpenCvSharp.Point(faceRect.X, faceRect.Y - 10),
                                            HersheyFonts.HersheyComplex, 0.6, Scalar.Yellow, 1);
                                    }
                                }
                            }
                        }
                    }
                    Dispatcher.Invoke(() => { CameraPreview.Source = display.ToBitmapSource(); });
                }
            }
            catch (Exception) { /* Handle error */ }
        }


        private void btnStartCapture_Click(object sender, RoutedEventArgs e)
        {
            capture = new VideoCapture(0, VideoCaptureAPIs.DSHOW);
            if (capture.IsOpened())
            {
                btnStartCapture.IsEnabled = false;
                timer.Start();
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            StopCamera();
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            StopCamera();
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        private void StopCamera()
        {
            timer.Stop();
            capture?.Release();
            capture?.Dispose();
            capture = null;
            CameraPreview.Source = null;
            btnStartCapture.IsEnabled = true;
        }

        protected override void OnClosed(EventArgs e)
        {
            StopCamera();
            base.OnClosed(e);
        }
    }
}