using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashboardTest
{
    public class MainWindowVM
    {
        public List<string> Shapes { get; set; }

        public MainWindowVM()
        {
            Shapes = new List<string> { "Square", "Triangle", "Circle" };
        }
    }
}
