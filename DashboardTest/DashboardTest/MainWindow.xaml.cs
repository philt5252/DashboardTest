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

namespace DashboardTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool isDragging = false;
        private Point startPoint;

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainWindowVM();

            sourceLbx.PreviewMouseDown += sourceLbx_PreviewMouseDown;
            sourceLbx.PreviewMouseUp += sourceLbx_PreviewMouseUp;
            sourceLbx.PreviewMouseMove += sourceLbx_PreviewMouseMove;
        }

        void sourceLbx_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !isDragging)
            {
                Point position = e.GetPosition(null);

                if (Math.Abs(position.X - startPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(position.Y - startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    StartDrag(e);

                }
            }   
        }

        private void StartDrag(MouseEventArgs e)
        {
            isDragging = true;
            WidgetHost host = new WidgetHost();
            host.Child = new CircleControl();

            DataObject data = new DataObject(host);
            DragDropEffects de = DragDrop.DoDragDrop(sourceLbx, data, DragDropEffects.Move);
            isDragging = false;
        }

        void sourceLbx_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
        }

        void sourceLbx_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(null);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            
        }

        private void editChk_Click(object sender, RoutedEventArgs e)
        {
            gridSystem.IsEdit = editChk.IsChecked.Value;
        }
    }
}
