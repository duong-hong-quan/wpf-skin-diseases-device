using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WPF.SkinDiseaseDevice.ViewModel.Command
{
    public class UploadImageCommand : ICommand
    {
        public SkinScannerVM Camera { get; set; }
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
        public UploadImageCommand(SkinScannerVM camera)
        {
            Camera = camera;
        }

        public bool CanExecute(object? parameter)
        {
            return true;
        }

        public async void Execute(object? parameter)
        {
            // Implement image upload logic using OpenFileDialog
          await  Camera.UploadImage();
        }

    }

}
