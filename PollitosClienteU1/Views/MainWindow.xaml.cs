using PollitosClienteU1.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace PollitosClienteU1
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        //Movimiento
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext is MainViewModel vm && vm.IsConnected)
            {
                //Movimiento de pollito (Arriba,Abajo,Izquierda y derecha)
                //Tambien puedes utilizar (W,A,S,D)
                if (e.Key == Key.Up || e.Key == Key.W)
                {
                    vm.EnviarMovimiento(1);
                }
                if (e.Key == Key.Down || e.Key == Key.S)
                {
                    vm.EnviarMovimiento(2);
                }
                if (e.Key == Key.Left || e.Key == Key.A)
                {
                    vm.EnviarMovimiento(3);
                }
                if (e.Key == Key.Right || e.Key == Key.D)
                {
                    vm.EnviarMovimiento(4);
                }
            }
        }
    }
}
