using Bio_Athun_System.Views;
using System.Windows;

namespace Bio_Athun_System
{
    public partial class DashboardWindow : Window
    {
        public DashboardWindow()
        {
            InitializeComponent();
        }

        private void EnrollFace_Click(object sender, RoutedEventArgs e)
        {
            var enrollWindow = new FaceEnrollmentWindow();
            enrollWindow.Show();
            this.Close();
        }

        private void FaceAuth_Click(object sender, RoutedEventArgs e)
        {
            var authWindow = new FaceEnrollmentWindow();
            authWindow.Show();
            this.Close();
        }
    }
}
