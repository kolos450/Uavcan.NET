using CanardApp.Tools.BusMonitor.Presentation;
using CanardSharp;
using CanardSharp.Dsdl.DataTypes;
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

namespace CanardApp.Tools.BusMonitor
{
    /// <summary>
    /// Interaction logic for BusMonitorControl.xaml
    /// </summary>
    public partial class BusMonitorControl : UserControl
    {
        public BusMonitorControl()
        {
            InitializeComponent();

            dgFrames.ItemsSource = new FrameModel[]
            {
                new FrameModel
                {
                    Direction = FrameDirection.Rx,
                    Time = DateTime.Now,
                    CanId = new CanId(32423434),
                    Data = new byte[]{ 0x48, 0x69, 0x21, 0xff, 0xff, 0xff, 0xff, 0xff },
                    SourceNodeId = 1,
                    DestinationNodeId= 123,
                    DataType = new ServiceType{ Meta = new CanardSharp.Dsdl.UavcanTypeMeta{  Namespace ="dfsdf", Name = "sdffd"} },
                },
                new FrameModel
                {
                    Direction = FrameDirection.Tx,
                    Time = DateTime.Now,
                    CanId = new CanId(32423434),
                    Data = new byte[]{ 0x48, 0x69 },
                    SourceNodeId = 1,
                    DestinationNodeId= 123,
                    DataType = new ServiceType{ Meta = new CanardSharp.Dsdl.UavcanTypeMeta{  Namespace ="dfsdf", Name = "sdffd"} },
                }
            };
        }
    }
}
