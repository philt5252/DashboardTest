using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Interaction logic for GridSystem.xaml
    /// </summary>
    public partial class GridSystem : UserControl
    {
        private List<Shape> corners = new List<Shape>();
        private List<WidgetHost> widgetHosts = new List<WidgetHost>();
        private Brush cornerBrush = new SolidColorBrush(Color.FromRgb(255, 0, 0));
        private Control previewControl;

        private bool isEdit = false;

        public bool IsEdit
        {
            get { return isEdit; }
            set
            {
                isEdit = value;

                if (isEdit)
                {
                    AllowDrop = true;

                    foreach (Shape corner in corners)
                    {
                        corner.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    AllowDrop = false;

                    foreach (Shape corner in corners)
                    {
                        corner.Visibility = Visibility.Hidden;
                    }
                }

                foreach (WidgetHost host in widgetHosts)
                {
                    host.IsEdit = isEdit;
                }
            }
        }

        public GridSystem()
        {
            InitializeComponent();

            Loaded += GridSystem_Loaded;
            SizeChanged += GridSystem_SizeChanged;
            DragOver += GridSystem_DragOver;
            Drop += GridSystem_Drop;
        }

        void GridSystem_Drop(object sender, DragEventArgs e)
        {
            if (!IsEdit)
                return;

            previewControl.Opacity = 1;

            if(!widgetHosts.Contains(previewControl))
            {
                WidgetHost host = previewControl as WidgetHost;
                widgetHosts.Add(host);

                host.IsResizing += host_IsResizing;
                host.ResizingComplete += host_ResizingComplete;
            }
                

            previewControl = null;
        }

        void host_ResizingComplete(object sender, EventArgs e)
        {
            mainCanvas.MouseMove -= mainCanvas_MouseMove;
            SnapResize();
        }

        void host_IsResizing(object sender, EventArgs e)
        {
            previewControl = sender as WidgetHost;
            mainCanvas.MouseMove += mainCanvas_MouseMove;
        }

        void mainCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed)
            {
                Point pos = e.GetPosition(previewControl);

                //previewControl.Height = (int)(((int)pos.Y / 50) + 1) * 50;
                //previewControl.Width = (int)(((int)pos.X / 50) + 1) * 50;

                previewControl.Height = pos.Y;
                previewControl.Width = pos.X;
            }
            else if(e.LeftButton == MouseButtonState.Released && previewControl != null)
            {
                SnapResize();
            }
        }

        private void SnapResize()
        {
            previewControl.Height = (int)(((int)previewControl.Height / 50)+1) * 50;
            previewControl.Width = (int)(((int)previewControl.Width / 50)+1) * 50;

            
            previewControl = null;

        }

        void GridSystem_DragOver(object sender, DragEventArgs e)
        {
            if (!IsEdit)
                return;

            if (previewControl == null)
            {
                previewControl = e.Data.GetData(e.Data.GetFormats()[0]) as Control;
                
                previewControl.Opacity = 0.5;

                if(!mainCanvas.Children.Contains(previewControl))
                {
                    previewControl.Height = 100;
                    previewControl.Width = 100;
                    mainCanvas.Children.Add(previewControl);
                }
                    
            }

            Point mousePos = e.GetPosition(mainCanvas);

            int xLoc = (int)((int)mousePos.X / 50) * 50;
            int yLoc = (int)((int)mousePos.Y / 50) * 50;

            Canvas.SetTop(previewControl, yLoc);
            Canvas.SetLeft(previewControl, xLoc);

        }

        void GridSystem_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RefreshCorners();
        }

        void GridSystem_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshCorners();
        }

        private void RefreshCorners()
        {
            foreach (Shape corner in corners)
            {
                mainCanvas.Children.Remove(corner);
            }

            corners.Clear();

            for (int y = 0; y < Height; y += 50)
            {
                for (int x = 0; x < Width; x += 50)
                {
                    AddCorner(x, y);
                }
            }
        }

        private void AddCorner(double x, double y)
        {
            Shape corner = CreateGridCorner();
            corners.Add(corner);
            mainCanvas.Children.Add(corner);
            corner.Visibility = IsEdit ? Visibility.Visible : Visibility.Hidden;

            Canvas.SetTop(corner, y - 5);
            Canvas.SetLeft(corner, x - 5);
        }

        Shape CreateGridCorner()
        {
            Path path = new Path();

            string sData = "M3.875,0 L5.125,0 5.125,3.875 9,3.875 9,5.125 5.125,5.125 5.125,9 3.875,9 3.875,5.125 0,5.125 0,3.875 3.875,3.875 3.875,0 z";
            var converter = TypeDescriptor.GetConverter(typeof(Geometry));
            path.Data = (Geometry)converter.ConvertFrom(sData);
            path.Fill = cornerBrush;

            return path;
        }
    }
}
