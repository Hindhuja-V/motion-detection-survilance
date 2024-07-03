using MotionDetectionSurvilance.Web;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace MotionDetectionSurvilance
{
    public sealed partial class MainPage : Page
    {
        private CoreCamera Camera;
        internal static SoftwareBitmap oldImg;
        private MotionDataCollection MotionDataCollection;
        private int threshold;
        private int smooth;

        private MotionDetectorFactory MotionDetectorFactory;

        private NetworkManager NetworkManager;

        TextBox[] subEmail;


        public MainPage()
        {
            this.InitializeComponent();
            myDispatcher = Dispatcher;
            myStatus = Status;

            threshold = 25;
            smooth = 10;

            Camera = new CoreCamera(PreviewControl);

            CamerasList.SelectedIndex = 0;
            Camera.cameraPreview.PreviewStatusChanged += CameraPreview_PreviewStatusChanged;

            MotionDataCollection = new MotionDataCollection(20);
            MotionChart.DataContext = MotionDataCollection.MotionValue;

            NetworkManager = new NetworkManager();

            MotionDetectorFactory = new MotionDetectorFactory(Camera, MotionDataCollection);
            MotionDetectorFactory.ImageCaptured += UpdateUI;
            MotionDetectorFactory.ImageCaptured += SaveImage;
            MotionDetectorFactory.ImageCaptured += SendNotification;
            MotionDetectorFactory.ImageCaptured += SendEmail;

            Task.Factory.StartNew(() => NetworkManager.Start());
            NetworkManager.UpdateSettings += NetworkManager_UpdateSettings;

            subEmail = new TextBox[] { subEmail1, subEmail2, subEmail3, subEmail4 };
            ShowSubEmail();

            ShowMessage($"Web portal at: {GetLocalIp()}:8081");
        }

        private void ShowSubEmail()
        {
            EmailData[] x = EmailData.EmailList;
            for (int i = 0; i < subEmail.Length && i < x.Length; i++)
            {
                subEmail[i].Text = x[i].EmailTo;
            }
        }

        private void SendEmail(object sender, MotionResult e) => new Task(async () =>
                                                               {
                                                                   if (await ShouldSendNotification(e.Difference))
                                                                   {
                                                                       //big movement occured
                                                                       EmailData.SendEmailToAll();
                                                                       MotionDetectorFactory.ImageCaptured -= SendEmail;
                                                                       Task.Delay(10000).Wait();
                                                                       MotionDetectorFactory.ImageCaptured += SendEmail;
                                                                   }
                                                               }).Start();

        private async void SaveImage(object sender, MotionResult e)
        {
            if (await ShouldSendNotification(e.Difference))
            {
                MotionDetectorFactory.SaveImage();
                Debug.WriteLine("Image Captured");
            };
        }


        private void SendNotification(object sender, MotionResult e) => new Task(async () =>
                                                                      {
                                                                          if (await ShouldSendNotification(e.Difference))
                                                                          {
                                                                              //big movement occured
                                                                              SubscribeNotificationData.sendNotificationToAll();
                                                                              MotionDetectorFactory.ImageCaptured -= SendNotification;
                                                                              Task.Delay(5000).Wait();
                                                                              MotionDetectorFactory.ImageCaptured += SendNotification;
                                                                          }
                                                                      }).Start();

        private async Task<bool> ShouldSendNotification(int value)
        {
            int notification = 999999;
            bool? isNotification = false;
            await runOnUIThread(() =>
            {
                notification = (int)NotificationAt.Value;
                isNotification = NotificationEnable.IsChecked;
            });

            return value > notification && isNotification == true;
        }

        private async void UpdateUI(object sender, MotionResult e)
        {
            UpdatePrevToResult(e.Image);
            ShowMessage(e.Difference.ToString());
            CaptureImage();

            await runOnUIThread(() =>
            {
                if (e.Difference > NotificationAt.Value)
                {
                    NotificationControl.Background = new SolidColorBrush(Color.FromArgb(255, 244, 217, 66));
                }
                else
                {
                    NotificationControl.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                }
            });
        }

        private async void UpdatePrevToResult(SoftwareBitmap image)
        {
            await runOnUIThread(async () =>
            {
                var softwareBitmap = image;
                if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 ||
                            softwareBitmap.BitmapAlphaMode == BitmapAlphaMode.Straight)
                {
                    softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                }

                var source = new SoftwareBitmapSource();
                await source.SetBitmapAsync(softwareBitmap);

                // Set the source of the Image control
                ImgPreview.Source = source;
            });
        }

        private async void NetworkManager_UpdateSettings(object sender, Settings e)
        {
            await runOnUIThread(() =>
            {
                switch (e.SettingName)
                {
                    case SettingName.Noise:
                        Noise.Value = e.Value;
                        break;
                    case SettingName.Multiplier:
                        Multiplier.Value = e.Value;
                        break;
                    case SettingName.NotificationAt:
                        NotificationAt.Value = e.Value;
                        break;
                    case SettingName.NotificationEnable:
                        NotificationEnable.IsChecked = e.Value == 0 ? false : true;
                        break;
                }
            });
        }

        private void CameraPreview_PreviewStatusChanged(object sender, bool preview)
        {
            BtnCapture.IsEnabled = preview;
            btnCredentials.IsEnabled = !preview;
        }

        private void CamerasList_DoubleTapped(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            var selectedCamera = CamerasList.SelectedItem as CameraInformation;
            if (selectedCamera == null)
            {
                Status.Text = "No camera selected/found";
                return;
            }
            Camera.settings.VideoDeviceId = selectedCamera.deviceInformation.Id;

            Camera.StartPreview();
        }

        private void BtnCapture_Click(object sender, RoutedEventArgs e)
        {
            isMonitoring = !isMonitoring;

            if (isMonitoring)
            {
                CaptureImage();
            }
        }

        private bool isMonitoring = false;

        private async void CaptureImage()
        {
            try
            {
                if (isMonitoring)
                {
                    await Task.Factory.StartNew(() => MotionDetectorFactory.CaptureImage(threshold, smooth));
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return;
            }
        }



        private static CoreDispatcher myDispatcher;

        public static async Task runOnUIThread(DispatchedHandler d)
        {
            await myDispatcher.RunAsync(CoreDispatcherPriority.Normal, d);
        }

        private static TextBlock myStatus;

        public static async void ShowMessage(String message)
        {
            await runOnUIThread(() => myStatus.Text = message);
        }

        private async void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            await ApplicationData.Current.ClearAsync();
        }

        private async void UpdateEmailSubs(object sender, RoutedEventArgs e)
        {
            await runOnUIThread(() =>
            {
                btnCredentials.Flyout.Hide();
            });

            var x = new List<EmailData>();

            foreach (var sub in subEmail)
            {
                if (isValidMail(sub.Text))
                {
                    x.Add(new EmailData(sub.Text));
                }
                else
                {
                    sub.Text = "";
                }
            }

            EmailData.EmailList = x.ToArray();

            bool isValidMail(string email)
            {
                try
                {
                    new MailAddress(email);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                    throw;
                }
            }
        }

        private string GetLocalIp()
        {
            var icp = NetworkInformation.GetInternetConnectionProfile();

            if (icp?.NetworkAdapter == null) return "";
            try
            {
                var hostname =
                        NetworkInformation.GetHostNames()
                            .FirstOrDefault(
                                hn =>
                                    hn.Type == Windows.Networking.HostNameType.Ipv4 &&
                                    hn.IPInformation?.NetworkAdapter != null &&
                                    hn.IPInformation.NetworkAdapter.NetworkAdapterId == icp.NetworkAdapter.NetworkAdapterId);

                // the ip address
                return hostname?.CanonicalName;
            }
            catch (Exception)
            {
                return "";
            }
        }
    }
}
