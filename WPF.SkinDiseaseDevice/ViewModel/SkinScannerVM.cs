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
    public class SkinScannerVM : INotifyPropertyChanged, IDisposable
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

        private void OnFrameCaptured(object sender, byte[] frameData)
        {
            if (System.Windows.Application.Current != null)
            {
                Task.Run(() =>
                {
                    if (cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        Console.WriteLine("Frame capture operation was canceled.");
                        return;
                    }

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            ImageSource = ConvertToBitmapImage(frameData);
                        }
                        catch (ArgumentException ex)
                        {
                            Console.WriteLine($"Error creating BitmapImage: {ex.Message}");
                        }
                    });
                }, cancellationTokenSource.Token)
                .ContinueWith(task =>
                {
                    if (task.Exception != null)
                    {
                        Console.WriteLine($"Error capturing frame: {task.Exception.InnerException?.Message}");
                    }
                }, TaskContinuationOptions.OnlyOnFaulted);
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
                using (var cts = new CancellationTokenSource())
                {
                    int timeoutMilliseconds = 5000; // 5 seconds
                    cts.CancelAfter(timeoutMilliseconds);

                    byte[] imageData = await cameraModel.CaptureImage(cts.Token).ConfigureAwait(true);

                    if (imageData != null)
                    {
                        string imagesFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images");

                        if (!Directory.Exists(imagesFolderPath))
                        {
                            Directory.CreateDirectory(imagesFolderPath);
                        }

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
            catch (OperationCanceledException)
            {
                Console.WriteLine("Capture operation was canceled due to timeout.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error capturing image: {ex.Message}");
                // Log the exception details or handle it accordingly
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

        public void Dispose()
        {
            cameraModel?.Dispose();
        }
    }

}