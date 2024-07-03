using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.System.Display;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace MotionDetectionSurvilance
{
    internal class CameraPreview
    {
        private MediaCapture mediaCapture;
        private bool isPreviewing;

        private DisplayRequest displayRequest;

        private CaptureElement previewControl;
        private MediaCaptureInitializationSettings settings;
        private LowLagPhotoCapture lowLagCapture;

        internal event EventHandler<bool> PreviewStatusChanged;

        public CameraPreview(CaptureElement previewControl)
        {
            this.previewControl = previewControl;
        }

        internal async void SaveImage()
        {
            try
            {
                var myPictures = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Pictures);
                var motionPictures = await myPictures.SaveFolder.CreateFolderAsync("MotionPictures", CreationCollisionOption.OpenIfExists);
                StorageFile file = await motionPictures.CreateFileAsync("motionPhoto.jpg", CreationCollisionOption.GenerateUniqueName);

                var stream = new InMemoryRandomAccessStream();

                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);

                encoder.SetSoftwareBitmap(MainPage.oldImg);
                await encoder.FlushAsync();

                using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var decoder = await BitmapDecoder.CreateAsync(stream);
                    var encoder1 = await BitmapEncoder.CreateForTranscodingAsync(fileStream, decoder);

                    var properties = new BitmapPropertySet { { "System.Photo.Orientation", new BitmapTypedValue(PhotoOrientation.Normal, PropertyType.UInt16) } };
                    await encoder1.BitmapProperties.SetPropertiesAsync(properties);

                    await encoder1.FlushAsync();
                }

                //ms.Dispose();
                stream.Dispose();
            }
            catch (Exception)
            {
                //throw;
            }
        }

        internal async Task<SoftwareBitmap> CaptureImage()
        {
            if (isPreviewing)
            {
                CapturedPhoto capturedPhoto = null;

                capturedPhoto = await lowLagCapture.CaptureAsync();

                var softwareBitmap = capturedPhoto.Frame.SoftwareBitmap;


                return softwareBitmap;
            }

            return null;
        }

        internal async void StartPreviewAsync(MediaCaptureInitializationSettings settings)
        {
            this.settings = settings;
            if (!isPreviewing)
            {
                await startPreviewAsync();
            }
            else
            {
                await CleanupCameraAsync();
            }

            PreviewStatusChanged?.Invoke(this, isPreviewing);
        }

        private async Task startPreviewAsync()
        {
            try
            {
                mediaCapture = new MediaCapture();
                await mediaCapture.InitializeAsync(settings);


                displayRequest = new DisplayRequest();
                displayRequest.RequestActive();
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;
                lowLagCapture = await mediaCapture.PrepareLowLagPhotoCaptureAsync(ImageEncodingProperties.CreateUncompressed(MediaPixelFormat.Bgra8));
            }
            catch (UnauthorizedAccessException)
            {
                MainPage.ShowMessage("Unable to start");
                return;
            }

            try
            {
                previewControl.Source = mediaCapture;
                await mediaCapture.StartPreviewAsync();
                isPreviewing = true;
            }
            catch (System.IO.FileLoadException)
            {
                mediaCapture.CaptureDeviceExclusiveControlStatusChanged += MediaCapture_CaptureDeviceExclusiveControlStatusChanged;
            }
        }

        private async void MediaCapture_CaptureDeviceExclusiveControlStatusChanged(MediaCapture sender, MediaCaptureDeviceExclusiveControlStatusChangedEventArgs args)
        {
            if (args.Status == MediaCaptureDeviceExclusiveControlStatus.SharedReadOnlyAvailable)
            {
                MainPage.ShowMessage("The camera preview can't be displayed because another app has exclusive access");
            }
            else if (args.Status == MediaCaptureDeviceExclusiveControlStatus.ExclusiveControlAvailable && !isPreviewing)
            {
                await MainPage.runOnUIThread(async () => { await startPreviewAsync(); });
            }
        }

        private async Task CleanupCameraAsync()
        {
            if (mediaCapture != null)
            {
                if (isPreviewing)
                {
                    await mediaCapture.StopPreviewAsync();
                }

                await MainPage.runOnUIThread(async () =>
                {
                    if (lowLagCapture != null)
                    {
                        await lowLagCapture.FinishAsync();
                    }
                    previewControl.Source = null;
                    if (displayRequest != null)
                    {
                        displayRequest.RequestRelease();
                    }

                    mediaCapture.Dispose();
                });
            }
            isPreviewing = false;
        }
    }
}
