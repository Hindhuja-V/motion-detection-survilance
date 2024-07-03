using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace MotionDetectionSurvilance
{
    internal class MotionResult
    {
        public int Difference { get; set; }
        public SoftwareBitmap Image { get; set; }
    }

    internal class MotionDetector
    {

        private int SmoothHeight;
        private int SmoothWidth;

        internal MotionResult ComputeDifference(SoftwareBitmap newImage, SoftwareBitmap oldImage, int threshold, int smooth)
        {
            SmoothHeight = smooth;
            SmoothWidth = smooth;

            var newImgdata = new ImageData(newImage);
            var oldImgdata = new ImageData(oldImage);

            var difference = 0;
            for (int i = 0; i < newImgdata.blue.Length; i++)
            {
                var pixeldifferenceR = newImgdata.red[i] - oldImgdata.red[i];
                var pixeldifferenceG = newImgdata.green[i] - oldImgdata.green[i];
                var pixeldifferenceB = newImgdata.blue[i] - oldImgdata.blue[i];

                var pixeldifference = (pixeldifferenceR + pixeldifferenceG + pixeldifferenceB) / 3;
                difference += Math.Abs(pixeldifference) - threshold <= 0 ? 0 : pixeldifference - threshold;
            }

            //formatting to make it visible for preview
            oldImage = SoftwareBitmap.Convert(oldImage, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            difference = Math.Abs(difference / smooth);

            return new MotionResult() { Difference = difference, Image = oldImage };
        }

        private SoftwareBitmap SmoothImage(SoftwareBitmap softwareBitmap)
        {
            var pixels = new ImageData(softwareBitmap);

            for (int height = 0; height < softwareBitmap.PixelHeight; height += SmoothHeight)
            {
                for (int width = 0; width < softwareBitmap.PixelWidth; width += SmoothWidth)
                {
                    var r = new Task(() => pixels.red = getAvg(ref pixels.red, height, width, softwareBitmap));
                    var g = new Task(() => pixels.green = getAvg(ref pixels.green, height, width, softwareBitmap));
                    var b = new Task(() => pixels.blue = getAvg(ref pixels.blue, height, width, softwareBitmap));

                    r.Start();
                    g.Start();
                    b.Start();

                    r.Wait();
                    g.Wait();
                    b.Wait();
                }
            }

            softwareBitmap.CopyFromBuffer(pixels.ToBuffer().AsBuffer());

            return softwareBitmap;
        }

        private byte[] getAvg(ref byte[] arr, int height, int width, SoftwareBitmap softwareBitmap)
        {
            var startRow = height * softwareBitmap.PixelWidth;
            var startCol = width;
            var startIndex = startRow + startCol;

            var avgPLocation = new List<int>();
            var avgP = new List<int>();
            for (int h = 0; h < SmoothHeight; h++)
            {
                for (int w = 0; w < SmoothWidth; w++)
                {
                    var index = startIndex + (h * softwareBitmap.PixelHeight) + w;
                    avgPLocation.Add(index);
                }
            }

            for (int x = 0; x < avgPLocation.Count; x++)
            {
                avgP.Add(arr[avgPLocation[x]]);
            }

            var avg = avgP.Average();

            for (int x = 0; x < avgPLocation.Count; x++)
            {
                arr[avgPLocation[x]] = Convert.ToByte(avg);
            }
            return arr;
        }
    }

    internal class ImageData
    {
        public byte[] red;
        public byte[] green;
        public byte[] blue;
        public byte[] alpha;

        internal byte[] ToBuffer()
        {
            List<byte> buffer = new List<byte>();

            for (int i = 0; i < red.Length; i++)
            {
                buffer.Add(red[i]);
                buffer.Add(green[i]);
                buffer.Add(blue[i]);
                buffer.Add(alpha[i]);
            }
            return buffer.ToArray();
        }

        public ImageData(SoftwareBitmap img)
        {
            GetImageData(img);
        }

        private void GetImageData(SoftwareBitmap img)
        {
            byte[] buffer = new byte[4 * img.PixelHeight * img.PixelWidth];

            img.CopyToBuffer(buffer.AsBuffer());

            red = new byte[buffer.Length / 4];
            green = new byte[buffer.Length / 4];
            blue = new byte[buffer.Length / 4];
            alpha = new byte[buffer.Length / 4];

            int index = 0;
            for (int i = 0; i < buffer.Length; i += 4)
            {
                red[index] = buffer[i];
                green[index] = buffer[i + 1];
                blue[index] = buffer[i + 2];
                alpha[index] = buffer[i + 3];
                index++;
            }
        }
    }
}
