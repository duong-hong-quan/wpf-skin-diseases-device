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
    public class CameraModel : IDisposable
    {
        private VideoCaptureDevice videoSource;
        private Bitmap currentFrame;
        private readonly object frameLock = new object();

        public event EventHandler<byte[]> FrameCaptured;

        public CameraModel()
        {

        }

        public void StartCamera()
        {
            try
            {
                FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

                VideoCaptureDevice bestVideoSource = null;
                int bestQuality = 0; // Điểm chất lượng, có thể sử dụng độ phân giải, tỷ lệ khung hình, hoặc các yếu tố khác.

                foreach (FilterInfo device in videoDevices)
                {
                    VideoCaptureDevice videoSource = new VideoCaptureDevice(device.MonikerString);

                    // Kiểm tra xem camera có VideoCapabilities hay không
                    if (videoSource.VideoCapabilities.Length > 0)
                    {
                        // Đánh giá chất lượng của camera, ở đây mình sử dụng tổng số pixel của độ phân giải
                        int quality = videoSource.VideoCapabilities.Sum(vc => vc.FrameSize.Width * vc.FrameSize.Height);

                        // So sánh chất lượng với camera tốt nhất hiện tại
                        if (quality > bestQuality)
                        {
                            bestQuality = quality;
                            bestVideoSource = videoSource;
                        }
                    }
                }

                if (bestVideoSource != null)
                {
                    // Sử dụng camera có chất lượng tốt nhất
                    bestVideoSource.NewFrame += VideoSource_NewFrame;
                    bestVideoSource.Start();
                }
                else
                {
                    // Không tìm thấy camera nào có tín hiệu
                    // Log hoặc hiển thị thông báo nếu cần
                }
            }
            catch (Exception ex)
            {
                // Log hoặc hiển thị thông báo nếu có lỗi
                Console.WriteLine(ex.Message);
            }
        }



        private async void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                using (System.Drawing.Bitmap bitmap = (System.Drawing.Bitmap)eventArgs.Frame.Clone())
                {
                    // Use a lock to ensure thread safety when updating currentFrame
                    lock (frameLock)
                    {
                        currentFrame?.Dispose();
                        currentFrame = new System.Drawing.Bitmap(bitmap);
                    }

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
                Bitmap localFrame;

                // Use a lock to ensure thread safety when accessing currentFrame
                lock (frameLock)
                {
                    localFrame = currentFrame; // Store a reference to currentFrame
                }

                if (localFrame != null)
                {
                    // Check if cancellation is requested before capturing the image
                    cancellationToken.ThrowIfCancellationRequested();

                    // Directly return the localFrame without additional processing
                    return ConvertBitmapToByteArray(localFrame);
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
       

        public void Dispose()
        {
            lock (frameLock)
            {
                currentFrame?.Dispose();
            }

            videoSource?.SignalToStop();
            videoSource?.WaitForStop();
        }

    }
}