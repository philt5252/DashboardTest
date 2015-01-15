using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
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
        private Rect editWidgetHostOrigin;
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

            Task task = new Task(() =>
            {
                int increase = 0;
                int diff =-1;

                while (true)
                {

                    if (movedWidgetOrigins.Count == 0)
                    {
                        diff = -1;
                        increase = 0;
                        continue;
                    }

                    increase += diff;

                    if (increase % 10 == 0)
                    {
                        diff *= -1;
                        increase = 0;
                    }
                        
                    

                    Thread.Sleep(25);
                    foreach (WidgetHost widgetHost in movedWidgetOrigins.Keys)
                    {
                        widgetHost.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            widgetHost.Width += diff;
                            widgetHost.Height += diff;
                        }));

                    }
                }
            });

            task.Start();

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

            var tempDict = new Dictionary<WidgetHost, Rect>(movedWidgetOrigins);

            movedWidgetOrigins.Clear();

            foreach (WidgetHost widgetHost in tempDict.Keys)
            {
                widgetHost.Width = tempDict[widgetHost].Width+1;
                widgetHost.Height = tempDict[widgetHost].Height+1;
            }
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
                else
                {
                    int controlY = (int)Canvas.GetTop(editWidgetHost);
                    int controlX = (int)Canvas.GetLeft(editWidgetHost);

                    editWidgetHostOrigin = new Rect(new Point(controlX, controlY), 
                                                    new Size(editWidgetHost.Width, editWidgetHost.Height));
                }

            }

            TryToMoveMovedWidgetsToOrigins();

            Dictionary<WidgetHost, PointDistance> calculateWidgetMovements = CalculateWidgetMovements(editWidgetHost);

            foreach (var widgetHost in calculateWidgetMovements.Keys)
            {
                Canvas.SetTop(widgetHost, calculateWidgetMovements[widgetHost].Y);
                Canvas.SetLeft(widgetHost, calculateWidgetMovements[widgetHost].X);
            }

            Point mousePos = e.GetPosition(mainCanvas);

            int xLoc = (int)((int)mousePos.X / 50) * 50;
            int yLoc = (int)((int)mousePos.Y / 50) * 50;

            Canvas.SetTop(editWidgetHost, yLoc);
            Canvas.SetLeft(editWidgetHost, xLoc);

        }

        private void TryToMoveMovedWidgetsToOrigins()
        {
            foreach (WidgetHost widgetHost in movedWidgetOrigins.Keys)
            {
                Rect origHostBounds = movedWidgetOrigins[widgetHost];
                var shortestDistance = ShortestDistancesToFreeAreas(widgetHost);

                double distance = Math.Sqrt(Math.Pow((Canvas.GetLeft(widgetHost) - origHostBounds.X), 2)
                                            + Math.Pow(Canvas.GetTop(widgetHost) - origHostBounds.Y, 2));

                if (distance > shortestDistance.Distance)
                {
                    if (!movedWidgetOrigins.ContainsKey(widgetHost))
                    {
                        movedWidgetOrigins[widgetHost] = GetRect(widgetHost);
                        widgetHost.Width -= 5;
                        widgetHost.Height -= 5;
                    }

                    Canvas.SetTop(widgetHost, shortestDistance.Y);
                    Canvas.SetLeft(widgetHost, shortestDistance.X);
                }
            }
        }

        private PointDistance ShortestDistancesToFreeAreas(WidgetHost widgetHost)
        {
            Rect hostBounds = GetRect(widgetHost);
            Rect origHostBounds = movedWidgetOrigins.ContainsKey(widgetHost)
                ? movedWidgetOrigins[widgetHost]
                : GetRect(widgetHost);

            List<PointDistance> distancesToFreeAreas = new List<PointDistance>();

            for (int y = 0; y < mainCanvas.ActualHeight; y += 50)
            {
                for (int x = 0; x < mainCanvas.ActualWidth; x += 50)
                {
                    if (hostBounds.X == x && hostBounds.Y == y)
                        continue;

                    Rect newRect = new Rect(x, y, origHostBounds.Width, origHostBounds.Height);

                    if (!IntersectsOtherHosts(widgetHost, newRect, new[] { widgetHost }))
                    {
                        double distance = Math.Sqrt(Math.Pow((newRect.X - origHostBounds.X), 2)
                                                    + Math.Pow(newRect.Y - origHostBounds.Y, 2));

                        distancesToFreeAreas.Add(new PointDistance(x, y, distance));
                    }
                }
            }
            return distancesToFreeAreas.OrderBy(pd => pd.Distance).First();
        }

        private Dictionary<WidgetHost, PointDistance> CalculateWidgetMovements(WidgetHost sourceWidget)
        {
            Rect controlBounds = GetRect(sourceWidget);
            Dictionary<WidgetHost, PointDistance> returnWidgetMovements = new Dictionary<WidgetHost, PointDistance>();
            foreach (WidgetHost widgetHost in widgetHosts)
            {
                if (sourceWidget.Equals(widgetHost))
                    continue;

                Rect hostBounds = GetRect(widgetHost);

                if (!controlBounds.IntersectsWith(hostBounds) || returnWidgetMovements.ContainsKey(widgetHost))
                    continue;

                var widgetMovements = GetOptimalMovementsFor(widgetHost);

                bool offsetLocFound = widgetMovements.Any();

                if (offsetLocFound)
                {
                    foreach (var movedWidget in widgetMovements.Keys)
                    {
                        if (!movedWidgetOrigins.ContainsKey(movedWidget))
                        {
                            movedWidgetOrigins[movedWidget] = GetRect(widgetHost);
                            widgetHost.Width -= 5;
                            widgetHost.Height -= 5;
                        }

                        returnWidgetMovements[movedWidget] = widgetMovements[movedWidget];
                    }

                    
                }
            }

            return returnWidgetMovements;
        }

        

        private Dictionary<WidgetHost, PointDistance> GetOptimalMovementsFor(WidgetHost widgetHost)
        {
            Rect hostBounds = GetRect(widgetHost);
            Rect origHostBounds = movedWidgetOrigins.ContainsKey(widgetHost)
                ? movedWidgetOrigins[widgetHost]
                : GetRect(widgetHost);

            Dictionary<WidgetHost, PointDistance> optimalMovements = new Dictionary<WidgetHost, PointDistance>();
            PointDistance shortestDistance = new PointDistance(hostBounds.X, hostBounds.Y, double.MaxValue);

            for (int y = 0; y < mainCanvas.ActualHeight; y += 50)
            {
                for (int x = 0; x < mainCanvas.ActualWidth; x += 50)
                {
                    if (hostBounds.X == x && hostBounds.Y == y)
                        continue;

                    Rect newRect = new Rect(x, y, origHostBounds.Width, origHostBounds.Height);

                    double distance = Math.Sqrt(Math.Pow((newRect.X - origHostBounds.X), 2)
                                                    + Math.Pow(newRect.Y - origHostBounds.Y, 2));

                    if (distance >= shortestDistance.Distance)
                        continue;

                    if (!IntersectsOtherHosts(widgetHost, newRect, new[] {widgetHost}))
                    {
                        shortestDistance.Distance = distance;
                        shortestDistance.X = x;
                        shortestDistance.Y = y;

                        optimalMovements[widgetHost] = shortestDistance;
                        continue;
                    }

                    optimalMovements[widgetHost] = shortestDistance;
                }
            }

            return optimalMovements;
        }

        private IOrderedEnumerable<PointDistance> OrderedDistancesToOccupiedAreas(WidgetHost widgetHost, double terminalDistance)
        {
            Rect hostBounds = GetRect(widgetHost);
            Rect origHostBounds = movedWidgetOrigins.ContainsKey(widgetHost)
                ? movedWidgetOrigins[widgetHost]
                : GetRect(widgetHost);

            List<PointDistance> distancesToFreeAreas = new List<PointDistance>();

            for (int y = 0; y < mainCanvas.ActualHeight; y += 50)
            {
                for (int x = 0; x < mainCanvas.ActualWidth; x += 50)
                {
                    if (hostBounds.X == x && hostBounds.Y == y)
                        continue;

                    Rect newRect = new Rect(x, y, origHostBounds.Width, origHostBounds.Height);

                    double distance = Math.Sqrt(Math.Pow((newRect.X - origHostBounds.X), 2)
                                                   + Math.Pow(newRect.Y - origHostBounds.Y, 2));

                    if (distance < terminalDistance)
                    {
                        foreach (WidgetHost intersectedHost in GetIntersectingHosts(widgetHost, newRect, new WidgetHost[0]))
                        {
                            CalculateWidgetMovements(intersectedHost);
                        }
                    }

                    if (!IntersectsOtherHosts(widgetHost, newRect, new[] { widgetHost }))
                    {
                       

                        distancesToFreeAreas.Add(new PointDistance(x, y, distance));
                    }
                }
            }
            return distancesToFreeAreas.OrderBy(pd => pd.Distance);
        }

        private bool IntersectsOtherHosts(WidgetHost host, Rect newHostLoc, IEnumerable<WidgetHost> exclusions)
        {
            return GetIntersectingHosts(host, newHostLoc, exclusions).Any();
        }

        private IEnumerable<WidgetHost> GetIntersectingHosts(WidgetHost host, Rect newHostLoc, IEnumerable<WidgetHost> exclusions)
        {
            return widgetHosts
                .Union(new []{editWidgetHost})
                .Except(exclusions.Union(new[] {host}))
                .Where(h => GetRect(h).IntersectsWith(newHostLoc));
        }

        private void TryMoveLeftTowardsPreviousSpot(WidgetHost host, Rect controlBounds)
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

        private struct PointDistance
        {
            public double X;
            public double Y;

            public double Distance;

            public PointDistance(double x, double y, double distance)
            {
                X = x;
                Y = y;
                Distance = distance;
            }
        }
    }
}
