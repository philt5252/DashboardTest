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
        private WidgetHost editWidgetHost;
        private Dictionary<WidgetHost, Rect> movedWidgetOrigins = new Dictionary<WidgetHost, Rect>(); 

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

            editWidgetHost.Opacity = 1;

            if(!widgetHosts.Contains(editWidgetHost))
            {
                WidgetHost host = editWidgetHost as WidgetHost;
                widgetHosts.Add(host);

                host.IsResizing += host_IsResizing;
                host.ResizingComplete += host_ResizingComplete;
            }
                

            editWidgetHost = null;
        }

        void GridSystem_DragOver(object sender, DragEventArgs e)
        {
            if (!IsEdit)
                return;

            if (editWidgetHost == null)
            {
                editWidgetHost = e.Data.GetData(e.Data.GetFormats()[0]) as WidgetHost;

                editWidgetHost.Opacity = 0.5;

                if (!mainCanvas.Children.Contains(editWidgetHost))
                {
                    editWidgetHost.Height = 100;
                    editWidgetHost.Width = 100;
                    mainCanvas.Children.Add(editWidgetHost);
                }

            }

            Point mousePos = e.GetPosition(mainCanvas);

            int xLoc = (int)((int)mousePos.X / 50) * 50;
            int yLoc = (int)((int)mousePos.Y / 50) * 50;

            int controlY = (int)Canvas.GetTop(editWidgetHost);
            int controlX = (int)Canvas.GetLeft(editWidgetHost);

            Rect controlBounds = GetRect(editWidgetHost);

            //if(Canvas.GetTop(editWidgetHost) != yLoc
           //     || Canvas.GetLeft(editWidgetHost) != xLoc)
           // {
                foreach(WidgetHost host in widgetHosts)
                {
                    if (editWidgetHost.Equals(host))
                        continue;

                    Rect hostBounds = GetRect(host);

                    if (movedWidgetOrigins.ContainsKey(host))
                    {
                        Rect tempHostBounds = GetRect(host);

                        int xOffset = 0;

                        do
                        {
                            if (movedWidgetOrigins[host].Left < tempHostBounds.Left)
                            {
                                xOffset -= 50;
                            }

                            tempHostBounds.Offset(xOffset, 0);

                            if (!tempHostBounds.IntersectsWith(controlBounds))
                            {
                                Canvas.SetTop(host, tempHostBounds.Top);
                                Canvas.SetLeft(host, tempHostBounds.Left);
                                break;
                            }

                            
                        } while (tempHostBounds.Left > movedWidgetOrigins[host].Left);

                        if (!movedWidgetOrigins[host].IntersectsWith(controlBounds))
                        {
                            Canvas.SetTop(host, movedWidgetOrigins[host].Top);
                            Canvas.SetLeft(host, movedWidgetOrigins[host].Left);
                            movedWidgetOrigins.Remove(host);
                            continue;
                        }
                    }

                    if (controlBounds.IntersectsWith(hostBounds))
                    {
                        int offsetX = 0;
                        int offsetY = 0;

                        bool offsetLocFound = false;

                        while (!offsetLocFound && hostBounds.Right <= Width)
                        {
                            offsetX += 50;
                            hostBounds.Offset(offsetX, 0);

                            if (!controlBounds.IntersectsWith(hostBounds))
                            {
                                offsetLocFound = true;
                            }
                        }

                        while (!offsetLocFound && hostBounds.Left > 0)
                        {
                            offsetX -= 50;
                            hostBounds.Offset(offsetX, 0);

                            if (!controlBounds.IntersectsWith(hostBounds))
                            {
                                offsetLocFound = true;
                            }
                        }

                        if (offsetLocFound)
                        {
                            if (!movedWidgetOrigins.ContainsKey(host))
                                movedWidgetOrigins[host] = GetRect(host);

                            Canvas.SetTop(host, hostBounds.Top);
                            Canvas.SetLeft(host, hostBounds.Left);
                        }
                        
                    }
                }
            //}

            Canvas.SetTop(editWidgetHost, yLoc);
            Canvas.SetLeft(editWidgetHost, xLoc);

        }

        Rect GetRect(WidgetHost host)
        {
            return new Rect(new Point(Canvas.GetLeft(host), Canvas.GetTop(host)), new Size(host.Width-1, host.Height-1));
        }

        void host_ResizingComplete(object sender, EventArgs e)
        {
            mainCanvas.MouseMove -= mainCanvas_MouseMove;
            SnapResize();
        }

        void host_IsResizing(object sender, EventArgs e)
        {
            editWidgetHost = sender as WidgetHost;
            mainCanvas.MouseMove += mainCanvas_MouseMove;
        }

        void mainCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed && editWidgetHost != null)
            {
                Point pos = e.GetPosition(editWidgetHost);

                //editWidgetHost.Height = (int)(((int)pos.Y / 50) + 1) * 50;
                //editWidgetHost.Width = (int)(((int)pos.X / 50) + 1) * 50;

                editWidgetHost.Height = pos.Y;
                editWidgetHost.Width = pos.X;
            }
            else if(e.LeftButton == MouseButtonState.Released && editWidgetHost != null)
            {
                SnapResize();
            }
        }

        private void SnapResize()
        {
            editWidgetHost.Height = (int)(((int)editWidgetHost.Height / 50)+1) * 50;
            editWidgetHost.Width = (int)(((int)editWidgetHost.Width / 50)+1) * 50;

            
            editWidgetHost = null;

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
