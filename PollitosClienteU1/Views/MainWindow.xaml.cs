using PollitosClienteU1.ViewModels;
using System;
using System.Windows;

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

        private void Window_Closed(object sender, EventArgs e)
        {
            var vm = DataContext as MainViewModel;
            vm?.CerrarConexion();
        }
    }
}
