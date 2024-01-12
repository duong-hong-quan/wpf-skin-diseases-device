using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using WPF.SkinDiseaseDevice.Model;
using WPF.SkinDiseaseDevice.ViewModel.Command;

namespace WPF.SkinDiseaseDevice.ViewModel
{
    public class SkinScannerVM : INotifyPropertyChanged
    {
        private readonly CameraModel cameraModel;
        public CaptureImageCommand CaptureImageCommand { get; set; }
        private readonly ImageUtility imageUtility;
        private CancellationTokenSource cancellationTokenSource;

        private ObservableCollection<BitmapImage> _images;
        public ObservableCollection<BitmapImage> Images
        {
            get { return _images; }
            set
            {
                _images = value;
                OnPropertyChanged(nameof(Images));
            }
        }

        public SkinScannerVM()
        {
            cameraModel = new CameraModel();
            cameraModel.FrameCaptured += OnFrameCaptured;
            StartCamera();
            imageUtility = new();
            _images = new ObservableCollection<BitmapImage>();
            CaptureImageCommand = new CaptureImageCommand(this);
            GetAllImageInFolder();
            cancellationTokenSource = new CancellationTokenSource();

        }

        private BitmapImage _imageSource;
        public BitmapImage ImageSource
        {
            get { return _imageSource; }
            set
            {
                _imageSource = value;
                OnPropertyChanged(nameof(ImageSource));
            }
        }

        private async void OnFrameCaptured(object sender, byte[] frameData)
        {
            if (System.Windows.Application.Current != null)
            {
                try
                {
                    // Use Task.Run to offload the work to a background thread
                    await Task.Run(() =>
                    {
                        if (cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            Console.WriteLine("Frame capture operation was canceled.");
                            return;
                        }

                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            ImageSource = ConvertToBitmapImage(frameData);
                        });
                    }, cancellationTokenSource.Token);
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine("Frame capture operation was canceled.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error capturing frame: {ex.Message}");
                }
            }
        }


        private BitmapImage ConvertToBitmapImage(byte[] imageData)
        {
            try
            {
                if (imageData == null)
                {
                    // Handle the null case appropriately
                    return null;
                }

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = new MemoryStream(imageData);
                bitmapImage.EndInit();

                return bitmapImage;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating BitmapImage: {ex.Message}");
                return null;
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void StartCamera()
        {
            cameraModel.StartCamera();
        }

        public void StopCamera()
        {
            cameraModel.StopCamera();
        }

        public async Task CaptureImage()
        {
            try
            {
                // Use CancellationTokenSource to create a cancellation token
                using (var cts = new CancellationTokenSource())
                {
                    // Set a timeout for the operation (adjust the timeout value as needed)
                    int timeoutMilliseconds = 5000; // 5 seconds
                    cts.CancelAfter(timeoutMilliseconds);

                    // Pass the cancellation token to CaptureImage method
                    byte[] imageData = await Task.Run(() => cameraModel.CaptureImage(cts.Token));

                    if (imageData != null)
                    {
                        string imagesFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images");
                        string savePath = $"{imagesFolderPath}\\{DateTime.Now:yyyyMMddHHmmssfff}.jpg";
                        await SaveImageAsync(imageData, savePath);
                        GetAllImageInFolder();
                    }
                    else
                    {
                        Console.WriteLine("Captured image data is null.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error capturing image: {ex.Message}");
            }
        }


        private async Task SaveImageAsync(byte[] imageData, string filePath)
        {
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Create))
                {
                    await fs.WriteAsync(imageData, 0, imageData.Length);
                }

                Console.WriteLine("Image saved successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving image: {ex.Message}");
            }
        }

        public void GetAllImageInFolder()
        {
            string imagesFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images");
            List<string> images = imageUtility.GetImagesFromFolder(imagesFolderPath);

            foreach (string image in images)
            {
                AddImage(image);
            }
        }




        private void AddImage(string imagePath)
        {
            try
            {
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.UriSource = new Uri(imagePath);
                bitmapImage.EndInit();

                Images.Add(bitmapImage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading image: {ex.Message}");
            }
        }
    }

}
