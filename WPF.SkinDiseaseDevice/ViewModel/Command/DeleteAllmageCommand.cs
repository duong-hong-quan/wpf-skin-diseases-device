using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WPF.SkinDiseaseDevice.ViewModel.Command
{
    public class DeleteAllmageCommand : ICommand
    {
        public SkinScannerVM Camera {get; set;}
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
        public DeleteAllmageCommand(SkinScannerVM camera)
        {
            Camera = camera;
        }

        public bool CanExecute(object? parameter)
        {
            return true;
        }

        public void Execute(object? parameter)
        {
         Camera.DeleteAllImageInFolder();
        }
    }
}
