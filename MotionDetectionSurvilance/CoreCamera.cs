using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace MotionDetectionSurvilance
{
    internal class CoreCamera
    {
        internal MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings();

        internal CameraPreview cameraPreview;

        public ObservableCollection<CameraInformation> Cameras { get; private set; }

        internal async void ShowCameraListAsync()
        {
            Cameras = new ObservableCollection<CameraInformation>();
            var devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            foreach (var device in devices)
            {
                Cameras.Add(new CameraInformation() { deviceInformation = device });
            }
        }

        public CoreCamera(CaptureElement previewControl)
        {
            cameraPreview = new CameraPreview(previewControl);
            this.ShowCameraListAsync();
        }

        internal void StartPreview()
        {
            cameraPreview.StartPreviewAsync(settings);
        }
    }
}