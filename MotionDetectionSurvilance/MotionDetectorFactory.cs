using MotionDetectionSurvilance.Web;
using System;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Media.Imaging;


namespace MotionDetectionSurvilance
{
    internal class MotionDetectorFactory
    {
        private readonly CoreCamera Camera;
        private readonly MotionDetector MotionDetector;
        private readonly MotionDataCollection MotionDataCollection;

        public MotionDetectorFactory(CoreCamera cameraSettings,
            MotionDataCollection motionDataCollection)
        {
            Camera = cameraSettings;
            MotionDetector = new MotionDetector();
            MotionDataCollection = motionDataCollection;
        }

        public void SaveImage()
        {
            Camera.cameraPreview.SaveImage();
        }

        internal async void CaptureImage(int threshold, int smooth)
        {
            //capture new image
            SoftwareBitmap newImage = await Camera.cameraPreview.CaptureImage();

            if (newImage != null)
            {
                if (MainPage.oldImg == null)
                {
                    MainPage.oldImg = newImage;
                }

                var result = await Task.Factory.StartNew(() => MotionDetector.ComputeDifference(MainPage.oldImg, newImage, threshold, smooth));

                await MainPage.runOnUIThread(() =>
                MotionDataCollection.AddMotion(result.Difference));

                MainPage.oldImg = newImage;

                ImageCaptured?.Invoke(this, result);
            }
        }
        public event EventHandler<MotionResult> ImageCaptured;
    }
}
