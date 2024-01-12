using AForge.Video.DirectShow;
using AForge.Video;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Drawing.Imaging;
using System.Windows.Media;

namespace WPF.SkinDiseaseDevice.Model
{
    public class CameraModel
    {
        private VideoCaptureDevice videoSource;
        private Bitmap currentFrame;

        public event EventHandler<byte[]> FrameCaptured;

        public void StartCamera()
        {
            try
            {
                FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

                if (videoDevices.Count > 0)
                {
                    videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
                    videoSource.NewFrame += VideoSource_NewFrame;
                    videoSource.Start();
                }
                else
                {
                    // Log or display a message indicating that no video devices are available.
                }
            }
            catch (Exception ex)
            {
                // Log or display the exception message.
                Console.WriteLine(ex.Message);
            }
        }

        private async void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                using (System.Drawing.Bitmap bitmap = (System.Drawing.Bitmap)eventArgs.Frame.Clone())
                {
                    // Dispose of the currentFrame if it's not null
                    currentFrame?.Dispose();

                    // Set the currentFrame to the new frame
                    currentFrame = bitmap;

                    // Notify subscribers that a new frame is captured
                    await Task.Run(() => FrameCaptured?.Invoke(this, ConvertBitmapToByteArray(currentFrame)));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error capturing frame: {ex.Message}");
            }
        }



        public void StopCamera()
        {
            videoSource?.SignalToStop();
            videoSource?.WaitForStop();
        }

        public async Task<byte[]> CaptureImage(CancellationToken cancellationToken = default)
        {
            try
            {
                if (currentFrame != null)
                {
                    // Check if cancellation is requested before capturing the image
                    cancellationToken.ThrowIfCancellationRequested();

                    // Simulate capturing image data (replace this with your actual capture logic)
                    await Task.Delay(1000); // Replace with your capture logic

                    return ConvertBitmapToByteArray(currentFrame);
                }
                else
                {
                    Console.WriteLine("No frame available for capture.");
                    return null;
                }
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Capture operation was canceled.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error capturing image: {ex.Message}");
                return null;
            }
        }


        private byte[] ConvertBitmapToByteArray(System.Drawing.Bitmap bitmap)
        {
            try
            {
                using (MemoryStream stream = new MemoryStream())
                using (System.Drawing.Bitmap copyBitmap = new System.Drawing.Bitmap(bitmap))
                {
                    copyBitmap.Save(stream, ImageFormat.Jpeg);
                    return stream.ToArray();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting bitmap to byte array: {ex.Message}");
                return null;
            }
        }


    }
}
