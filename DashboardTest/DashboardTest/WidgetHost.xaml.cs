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
    /// Interaction logic for WidgetHost.xaml
    /// </summary>
    public partial class WidgetHost : UserControl
    {
        private Control child;
        private bool isEdit;
        private bool isDragging = false;
        private Point startPoint;
        private List<Rectangle> handles = new List<Rectangle>();
        private bool isResizing;

        public event EventHandler IsResizing;
        public event EventHandler ResizingComplete;

        public Control Child 
        {
            get { return child; }
            set 
            {
                if(child != null)
                    mainGrid.Children.Remove(child);

                child = value;
                mainGrid.Children.Add(child);

                mainGrid.Children.Remove(overlayRect);
                mainGrid.Children.Add(overlayRect);

                foreach (Rectangle handle in handles)
                {
                    mainGrid.Children.Remove(handle);
                    mainGrid.Children.Add(handle);
                }
            }
        }

        public bool IsEdit
        {
            get { return isEdit; }
            set
            {
                isEdit = value;

                if (isEdit)
                {
                    overlayRect.Visibility = Visibility.Visible;

                    foreach (Rectangle handle in handles)
                    {
                        handle.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    overlayRect.Visibility = Visibility.Hidden;

                    foreach (Rectangle handle in handles)
                    {
                        handle.Visibility = Visibility.Hidden;
                    }
                }
                    
            }
        }

        public WidgetHost()
        {
            InitializeComponent();
            IsEdit = true;

            handles.Add(bottomRightRect);

            overlayRect.PreviewMouseDown += overlayRect_PreviewMouseDown;
            overlayRect.PreviewMouseUp += overlayRect_PreviewMouseUp;
            overlayRect.PreviewMouseMove += overlayRect_PreviewMouseMove;

            bottomRightRect.PreviewMouseDown += bottomRightRect_PreviewMouseDown;
            bottomRightRect.PreviewMouseUp += bottomRightRect_PreviewMouseUp;
            bottomRightRect.PreviewMouseMove += bottomRightRect_PreviewMouseMove;
        }

        void bottomRightRect_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!isResizing)
                return;

            Point pos = e.GetPosition(this);

            this.Height = pos.Y;
            this.Width = pos.X;
        }

        void bottomRightRect_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            isResizing = false;
            OnResizingComplete();
        }

        void bottomRightRect_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            isResizing = true;
            OnIsResizing();
        }

        protected virtual void OnIsResizing()
        {
            if (IsResizing == null)
                return;

            IsResizing(this, null);
        }

        protected virtual void OnResizingComplete()
        {
            if (ResizingComplete == null)
                return;

            ResizingComplete(this, null);
        }

        void overlayRect_PreviewMouseMove(object sender, MouseEventArgs e)
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

        void overlayRect_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
        }

        void overlayRect_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(null);
        }

        private void StartDrag(MouseEventArgs e)
        {
            isDragging = true;
            WidgetHost host = this;

            DataObject data = new DataObject(host);
            DragDropEffects de = DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
            isDragging = false;
        }

        
    }
}
