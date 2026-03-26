using System;
using System.Diagnostics;
using System.Resources;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;
using BioAthunSystem.Views;
using Emgu.CV.Face;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;

namespace Bio_Athun_System
{
    public partial class Window2 : System.Windows.Window
    {
        private VideoCapture? capture;
        private Mat frame = new Mat();
        private DispatcherTimer timer;
        private CascadeClassifier? faceCascade;

        public Window2()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            // تحميل الـ cascade بأمان
            string cascadePath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Resources", "haarcascade_frontalface_default.xml");

            if (System.IO.File.Exists(cascadePath))
            {
                faceCascade = new CascadeClassifier(cascadePath);
            }
            else
            {
                MessageBox.Show($"ملف الكشف عن الوجوه غير موجود:\n{cascadePath}", "تحذير");
            }

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(33);
            timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            try
            {
                if (capture == null || !capture.IsOpened()) return;

                // استخدام Grab + Retrieve بدلاً من Read (أكثر موثوقية)
                bool grabbed = capture.Grab();
                if (!grabbed) return;

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

                            var faces = faceCascade.DetectMultiScale(
                                gray,
                                scaleFactor: 1.1,
                                minNeighbors: 5,
                                flags: HaarDetectionTypes.ScaleImage,
                                minSize: new OpenCvSharp.Size(60, 60)
                            );

                            foreach (var faceRect in faces)
                            {
                                Cv2.Rectangle(display, faceRect, Scalar.FromRgb(53, 141, 230), 3);
                            }
                        }
                    }

                    Dispatcher.Invoke(() =>
                    {
                        CameraPreview.Source = display.ToBitmapSource();
                    });
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                MessageBox.Show($"خطأ في الكاميرا: {ex.Message}");
            }
        }

        private void btnStartCapture_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                capture = new VideoCapture(0, VideoCaptureAPIs.DSHOW); // DSHOW أفضل على Windows
                capture.Set(VideoCaptureProperties.FrameWidth, 640);
                capture.Set(VideoCaptureProperties.FrameHeight, 480);

                if (capture.IsOpened())
                {
                    btnStartCapture.IsEnabled = false;
                    timer.Start();
                }
                else
                {
                    MessageBox.Show("لم يتم العثور على الكاميرا!", "خطأ");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"فشل تشغيل الكاميرا: {ex.Message}");
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            StopCamera();
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {

            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            StopCamera();
            this.Close();
        }

        private void StopCamera()
        {
            timer.Stop();
            capture?.Release();
            capture?.Dispose();
            capture = null;
            frame?.Dispose();
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
