using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF.SkinDiseaseDevice.Utility
{
    public class ImageUtility
    {
        public  List<string> GetImagesFromFolder(string folderPath)
        {
            List<string> imagePaths = new List<string>();

            try
            {
                // Kiểm tra xem thư mục có tồn tại không
                if (!Directory.Exists(folderPath))
                {
                    Console.WriteLine("Thư mục không tồn tại.");
                    return imagePaths;
                }

                // Lấy danh sách tất cả các tệp trong thư mục
                string[] files = Directory.GetFiles(folderPath);

                foreach (var filePath in files)
                {
                    try
                    {
                        // Kiểm tra xem tệp có phải là hình ảnh không
                        if (IsImageFile(filePath))
                        {
                            imagePaths.Add(filePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Lỗi khi kiểm tra tệp {filePath}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi đọc thư mục {folderPath}: {ex.Message}");
            }

            return imagePaths;
        }

        private  bool IsImageFile(string filePath)
        {
            try
            {
                using (var img = Image.FromFile(filePath))
                {
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
       public  void DeleteFilesInFolder(string folderPath)
        {
            try
            {
                // Check if the folder exists
                if (Directory.Exists(folderPath))
                {
                    // Get all files in the folder
                    string[] files = Directory.GetFiles(folderPath);

                    // Delete each file
                    foreach (string file in files)
                    {
                        File.Delete(file);
                        Console.WriteLine($"Deleted file: {file}");
                    }

                    Console.WriteLine("All files deleted successfully.");
                }
                else
                {
                    Console.WriteLine("The specified folder does not exist.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}
