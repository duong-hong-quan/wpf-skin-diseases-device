using Emgu.CV;
using Emgu.CV.Structure;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WPF.SkinDiseaseDevice.Model;
using WPF.SkinDiseaseDevice.Utility;
using WPF.SkinDiseaseDevice.ViewModel.Command;
using static System.Net.WebRequestMethods;
using File = System.IO.File;

namespace WPF.SkinDiseaseDevice.ViewModel
{
    public class SkinScannerVM : INotifyPropertyChanged, IDisposable
    {
        private readonly CameraModel cameraModel;
        public CaptureImageCommand CaptureImageCommand { get; set; }
        public DeleteAllmageCommand DeleteImageCommand { get; set; }
        public UploadImageCommand UploadImageCommand { get; set; }

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
        private ObservableCollection<Prediction> _predictions;
        public ObservableCollection<Prediction> Predictions
        {
            get { return _predictions; }
            set
            {
                _predictions = value;
                OnPropertyChanged(nameof(Predictions));
            }
        }

        private string _predictedAgeRange;
        public string PredictedAgeRange
        {
            get { return _predictedAgeRange; }
            set
            {
                _predictedAgeRange = value;
                OnPropertyChanged(nameof(PredictedAgeRange));
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
            DeleteImageCommand = new DeleteAllmageCommand(this);
            UploadImageCommand = new UploadImageCommand(this);
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
                        int numOffaces = CountFace(savePath);
                        if (numOffaces <= 0)
                        {
                            MessageBox.Show("There is no face in the captured picture");
                            return;
                        }
                        else if (numOffaces > 1)
                        {
                            MessageBox.Show($"There is {numOffaces} faces in the captured picture");
                            return;
                        }
                        await MakeSkinDePredictionAsync(savePath);
                        await MakeAgePredictionAsync(savePath);
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
                    fs.Close();
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

        public void DeleteAllImageInFolder()
        {
            string imagesFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images");
            imageUtility.DeleteFilesInFolder(imagesFolderPath);
            Images = new();
        }




        private void AddImage(string imagePath)
        {
            try
            {
                var bitmapImage = new BitmapImage();
                using (var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
                {
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = stream;
                    bitmapImage.EndInit();
                }

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
        public async Task UploadImage()
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string imagePath = openFileDialog.FileName;
                int numOffaces = CountFace(imagePath);
                if (numOffaces <= 0)
                {
                    MessageBox.Show("There is no face in the captured picture");
                    return;
                }
                else if (numOffaces > 1)
                {
                    MessageBox.Show($"There is {numOffaces} faces in the captured picture");
                    return;
                }
                await MakeSkinDePredictionAsync(imagePath);
                await MakeAgePredictionAsync(imagePath);

            }
        }

        public async Task CallOpenAI(List<Prediction> list)
        {
            using (HttpClient client = new HttpClient())
            {
                var apiKey = ChatGPTAI.SecretKey;
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                var listDisease = " ";
                foreach (var item in list.ToList())
                {
                    listDisease += item.tagName;
                }
                var message = $"Đóng vai là 1 bác sĩ da liễu hãy đưa ra lời khuyên cho các bệnh sau: {listDisease}. Hãy trả lời bằng tiếng anh";
                // JSON content for the API call
                var jsonContent = new
                {
                    prompt = message,
                    model = "gpt-3.5-turbo-instruct",
                    max_tokens = 1000
                };

                // Make the API call
                var responseContent = await client.PostAsync("https://api.openai.com/v1/completions", new StringContent(JsonConvert.SerializeObject(jsonContent), Encoding.UTF8, "application/json"));

                // Read the response as a string
                var resContext = await responseContent.Content.ReadAsStringAsync();

                // Deserialize the response into a dynamic object
                var data = JsonConvert.DeserializeObject<dynamic>(resContext);
                if(data !=null)
                    MessageBox.Show($"{data.choices[0].text}", "Advice for you");
                await Task.CompletedTask;
            }


        }

        private async Task MakeSkinDePredictionAsync(string fileName)
        {
            //IConfigurationRoot config = GetConfig();
            string url = ConfigSkinDeAI.Url;
            string key = ConfigSkinDeAI.ApiKey;
            string contentType = ConfigSkinDeAI.ContentType;
            var file = File.ReadAllBytes(fileName);

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Prediction-Key", key);
                using (var content = new ByteArrayContent(file))
                {
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                    var response = await client.PostAsync(url, content);

                    var responseString = await response.Content.ReadAsStringAsync();

                    List<Prediction> predictions = (JsonConvert.DeserializeObject<CustomVision>(responseString)).Predictions;
                    await CallOpenAI(predictions.ToList());
                    RefineResult(predictions);
                    TranslateSymptomList(predictions);
                    Predictions = new ObservableCollection<Prediction>(predictions);

                }
            }
        }

        private async Task MakeAgePredictionAsync(string fileName)
        {
            //IConfigurationRoot config = GetConfig();
            string url = ConfigAgeAI.Url;
            string key = ConfigAgeAI.ApiKey;
            string contentType = ConfigAgeAI.ContentType;
            var file = File.ReadAllBytes(fileName);

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Prediction-Key", key);
                using (var content = new ByteArrayContent(file))
                {
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                    var response = await client.PostAsync(url, content);

                    var responseString = await response.Content.ReadAsStringAsync();

                    List<Prediction> predictions = (JsonConvert.DeserializeObject<CustomVision>(responseString)).Predictions;
                    PredictedAgeRange = GetHighest(predictions);
                }
            }
        }

        private int CountFace(string fileName)
        {
            try
            {
                CascadeClassifier faceCascade = new CascadeClassifier("haarcascades/haarcascade_frontalface_default.xml");
                // Load your image
                Image<Bgr, byte> image = new Image<Bgr, byte>(fileName);

                // Convert the image to grayscale
                Image<Gray, byte> grayImage = image.Convert<Gray, byte>();

                // Detect faces
                var faces = faceCascade.DetectMultiScale(grayImage, 1.3, 5);

                // Count the number of faces detected
                int numberOfFaces = faces.Length;
                return numberOfFaces;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error counting faces in image: {ex.Message}");
            }
            return -1;
        }
        private List<Prediction> TranslateSymptomList(List<Prediction> predictions)
        {
            foreach (Prediction prediction in predictions)
            {
                prediction.tagName = TranslateSymptom(prediction.tagName);
            }
            return predictions;
        }
        private void RefineResult(List<Prediction> predictions)
        {
            bool isNormalSkin = true;
            Prediction containsNormal = null;
            foreach (Prediction prediction in predictions.ToList())
            {
                if (!prediction.tagName.Equals("Normal"))
                {
                    if (isNormalSkin && prediction.Probability > 0.25)
                        isNormalSkin = false;
                    if (prediction.Probability <= 0.05)
                        predictions.Remove(prediction);
                }
                else
                {
                    if (containsNormal == null)
                        containsNormal = prediction;
                }
            }
            if (!isNormalSkin && containsNormal != null)
            {
                predictions.Remove(containsNormal);
            }
        }

        private string GetHighest(List<Prediction> predictions)
        {
            string ageRange = predictions.First().tagName;
            double highestProbability = predictions.First().Probability;
            foreach (Prediction prediction in predictions.ToList())
            {
                if (prediction.Probability > highestProbability)
                {
                    ageRange = prediction.tagName;
                    highestProbability = prediction.Probability;
                }
            }
            return ageRange;

        }

        private string TranslateSymptom(string predictionName)
        {
            if (predictionName.Equals("Acne and Rosacea"))
                return "Mụn và da ửng đỏ";
            if (predictionName.Equals("Eczema"))
                return "Viêm da cơ địa";
            if (predictionName.Equals("Normal"))
                return "Bình thường";
            if (predictionName.Equals("Melanoma Skin Cancer Nevi and Moles"))
                return "Ung thư da học mô và nốt ruồi";
            if (predictionName.Equals("Psoriasis Lichen Planus and related diseases"))
                return "Vẩy nến và Lichen phẳng";
            if (predictionName.Equals("Tinea Ringworm Candidiasis and other Fungal Infections"))
                return "Nấm da Candida và các bệnh \n nhiễm trùng nấm khác";
            if (predictionName.Equals("Vitiligo"))
                return "Bạch biến";
            return "Các bệnh khác";
        }
    }

}
