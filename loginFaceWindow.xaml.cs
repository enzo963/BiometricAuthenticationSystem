using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using OpenCvSharp.Face;

namespace Bio_Athun_System.Views
{
    public partial class loginFaceWindow : System.Windows.Window
    {
        private VideoCapture? capture;
        private Mat frame = new Mat();
        private DispatcherTimer timer;
        private CascadeClassifier? faceCascade;
        private int _loggedUserId;
        private LBPHFaceRecognizer? recognizer;
        private bool isTrained = false;
        private Dictionary<int, string> userNames = new Dictionary<int, string>();

        public loginFaceWindow(int userId)
        {
            InitializeComponent();
            this._loggedUserId = userId; // حفظ الرقم
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            string cascadePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "haarcascade_frontalface_default.xml");
            if (System.IO.File.Exists(cascadePath))
                faceCascade = new CascadeClassifier(cascadePath);

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(33);
            timer.Tick += Timer_Tick;

            // الآن نمرر الرقم للدالة ولن يظهر خطأ
            TrainModel(_loggedUserId);
        }



        private void TrainModel(int targetUserId)
        {
            try
            {
                List<Mat> faceImages = new List<Mat>();
                List<int> faceLabels = new List<int>();
                userNames.Clear();

                string connString = @"Data Source=ENZO\SQLEXPRESS;Initial Catalog=BioAuthDB;Integrated Security=True;TrustServerCertificate=True;";

                using (var conn = new Microsoft.Data.SqlClient.SqlConnection(connString))
                {
                    conn.Open();

                    // أ. جلب اسم المستخدم (لبقاء العرض صحيحاً على الشاشة)
                    string nameQuery = "SELECT Id, FullName FROM Users WHERE Id = @uid AND IsActive = 1";
                    using (var cmd = new Microsoft.Data.SqlClient.SqlCommand(nameQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", targetUserId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                userNames.Add(reader.GetInt32(0), reader.GetString(1));
                            }
                        }
                    }

                    // ب. جلب الصور ومعالجتها برمجياً (هنا السر في خفض الـ Confidence)
                    string imgQuery = "SELECT UserId, ImagePath FROM Details WHERE UserId = @uid";
                    using (var cmd = new Microsoft.Data.SqlClient.SqlCommand(imgQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", targetUserId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int userId = reader.GetInt32(0);
                                byte[] imageBytes = (byte[])reader["ImagePath"];

                                if (imageBytes != null && imageBytes.Length > 0)
                                {
                                    Mat img = Cv2.ImDecode(imageBytes, ImreadModes.Grayscale);
                                    if (img != null && !img.Empty())
                                    {
                                        // --- الإضافة الضرورية 1: موازنة الإضاءة ---
                                        // هذا السطر يقلل الفارق بين إضاءة وقت التسجيل ووقت الدخول
                                        Cv2.EqualizeHist(img, img);

                                        // --- الإضافة الضرورية 2: توحيد الحجم بدقة ---
                                        Cv2.Resize(img, img, new OpenCvSharp.Size(100, 100));

                                        faceImages.Add(img);
                                        faceLabels.Add(userId);
                                    }
                                }
                            }
                        }
                    }
                }

                if (faceImages.Count > 0)
                {
                    // إنشاء المحرك وتدريبه
                    recognizer = LBPHFaceRecognizer.Create();
                    recognizer.Train(faceImages, faceLabels);
                    isTrained = true;
                }
                else
                {
                    MessageBox.Show("لم يتم العثور على صور مسجلة لهذا المستخدم.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في التدريب المخصص: " + ex.Message);
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

                            // ✅ EqualizeHist مرة واحدة فقط
                            Cv2.EqualizeHist(gray, gray);

                            var faces = faceCascade.DetectMultiScale(
                                gray, 1.1, 5,
                                HaarDetectionTypes.ScaleImage,
                                new OpenCvSharp.Size(60, 60));

                            foreach (var faceRect in faces)
                            {
                                Cv2.Rectangle(display, faceRect, Scalar.FromRgb(53, 141, 230), 3);

                                if (isTrained && recognizer != null)
                                {
                                    // ✅ الإصلاح الرئيسي - نسخ منفصلة
                                    using (Mat faceROI = new Mat(gray, faceRect))
                                    using (Mat faceRegion = new Mat())
                                    {
                                        Cv2.Resize(faceROI, faceRegion, new OpenCvSharp.Size(100, 100));

                                        int outLabel = -1;
                                        double outConfidence = 0;
                                        recognizer.Predict(faceRegion, out outLabel, out outConfidence);

                                        // ✅ رفع الحد لأن صورك 30 صورة
                                        string displayLabel = outConfidence < 100
                                            ? (userNames.ContainsKey(outLabel) ? userNames[outLabel] : "User")
                                            : "Unknown";

                                        Cv2.PutText(display,
                                            $"{displayLabel} ({Math.Round(outConfidence)})",
                                            new OpenCvSharp.Point(faceRect.X, faceRect.Y - 10),
                                            HersheyFonts.HersheyComplex, 0.6, Scalar.Yellow, 1);

                                        if (outConfidence < 100 && outLabel == _loggedUserId)
                                        {
                                            Dispatcher.Invoke(() => {
                                                StopCamera();
                                                var userData = GetUserData(outLabel);
                                                DashboardWindow dash = new DashboardWindow(
                                                    userData.Id, userData.Name, userData.Role);
                                                dash.Show();
                                                this.Close();
                                            });
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    Dispatcher.Invoke(() => { CameraPreview.Source = display.ToBitmapSource(); });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Timer error: " + ex.Message);
            }
        }

        // 2. أضف الدالة هنا (خارج حدود القوس الخاص بالتايمر)
        private (int Id, string Name, string Role) GetUserData(int userId)
        {
            try
            {
                string connString = @"Data Source=ENZO\SQLEXPRESS;Initial Catalog=BioAuthDB;Integrated Security=True;TrustServerCertificate=True;";
                using (var conn = new Microsoft.Data.SqlClient.SqlConnection(connString))
                {
                    conn.Open();
                    string query = "SELECT Id, FullName, Role FROM Users WHERE Id = @id";
                    using (var cmd = new Microsoft.Data.SqlClient.SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", userId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // نرجع البيانات كـ Tuple (ثلاث قيم معاً)
                                return (reader.GetInt32(0), reader.GetString(1), reader.GetString(2));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Database Error: " + ex.Message);
            }

            // إذا لم يجد المستخدم أو حدث خطأ، نرجع قيم افتراضية حتى لا يتوقف البرنامج
            return (userId, "Unknown User", "User");
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