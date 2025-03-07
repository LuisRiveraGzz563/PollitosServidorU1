using PollitosClienteU1.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PollitosClienteU1.Views
{
    /// <summary>
    /// Lógica de interacción para CorralView.xaml
    /// </summary>
    public partial class CorralView : UserControl
    {        
        // Obtener el ViewModel desde el DataContext
      
        public CorralView()
        {
            InitializeComponent();
        }

        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {     
            var vm = this.DataContext as MainViewModel;
            if (e.Key == Key.Up)
            {
                vm.EnviarMovimiento(1);
            }
            if (e.Key == Key.Down)
            {
                vm.EnviarMovimiento(2);
            }
            if (e.Key == Key.Left)
            {
                vm.EnviarMovimiento(3);
            }
            if (e.Key == Key.Right)
            {
                vm.EnviarMovimiento(4);
            }
        }

    }
}
