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
using System.Windows.Threading;
using System.IO.Ports;
using ZedGraph;
using Microsoft.Win32;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace SerialPortControlBoard
{

    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private int val_R, val_B, val_G, val_W, val_Y;
        int tickStart = 0;

        SerialPort mySerialPort = null;
        MidiData RxData = null;
        MidiData TxData = null;
        string fp = null;
        public int fp_status;
        JsonData js = new JsonData();

        public MainWindow()
        {
            InitializeComponent();
            val_R = 0;
            val_B = 0;
            val_G = 0;

            fp = null;
            fp_status = 0;

            RxData = new MidiData();
            TxData = new MidiData();
            SetGraph();
            Binding binding = new Binding("FrameMsg");
            binding.Source = RxData;
            //myBinding.Mode = BindingMode.TwoWay;

            /////myBinding.ElementName = "FrameMsg";
            //// Bind the new data source to the myText TextBlock control's Text dependency property.
            txtRxDataReal.SetBinding(TextBox.TextProperty, binding);

            //binding Txdata
            binding = new Binding("TxString");
            binding.Source = TxData;
            txtTxData.SetBinding(TextBox.TextProperty, binding);

            //波特率初始化
            comboBoxBaudRate.Items.Clear();
            comboBoxBaudRate.Items.Add("9600");
            comboBoxBaudRate.Items.Add("19200");
            comboBoxBaudRate.Items.Add("38400");
            comboBoxBaudRate.Items.Add("57600");
            comboBoxBaudRate.Items.Add("115200");
            comboBoxBaudRate.Items.Add("921600");
            comboBoxBaudRate.SelectedItem = comboBoxBaudRate.Items[2];
            Slider_White.AddHandler(Slider.MouseLeftButtonUpEvent, new MouseButtonEventHandler(Slider_MouseLeftButtonUp), true);
            Slider_Red.AddHandler(Slider.MouseLeftButtonUpEvent, new MouseButtonEventHandler(Slider_MouseLeftButtonUp), true);
            Slider_Green.AddHandler(Slider.MouseLeftButtonUpEvent, new MouseButtonEventHandler(Slider_MouseLeftButtonUp), true);
            Slider_Blue.AddHandler(Slider.MouseLeftButtonUpEvent, new MouseButtonEventHandler(Slider_MouseLeftButtonUp), true);
            Slider_Yellow.AddHandler(Slider.MouseLeftButtonUpEvent, new MouseButtonEventHandler(Slider_MouseLeftButtonUp), true);
        }

        private void Slider_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var slider = sender as Slider;
            switch (slider.Name)
            {
                case "Slider_White":
                    val_W = 255 - (int)slider.Value;
                    break;
                case "Slider_Red":
                    val_R = 255 - (int)slider.Value;
                    ColorBar_ColorChanged(sender, e);
                    break;
                case "Slider_Blue":
                    val_B = 255 - (int)slider.Value;
                    ColorBar_ColorChanged(sender, e);
                    break;
                case "Slider_Green":
                    val_G = 255 - (int)slider.Value;
                    ColorBar_ColorChanged(sender, e);
                    break;
                case "Slider_Yellow":
                    val_Y = 255 - (int)slider.Value;
                    break;
                default:
                    break;
            }
        }

        private void ComboBoxPortName_DropDownOpened(object sender, EventArgs e)
        {
            string[] portnames = SerialPort.GetPortNames();
            Console.WriteLine(portnames.Length);
            ComboBox x = sender as ComboBox;
            x.Items.Clear();
            foreach (string xx in portnames)
            {
                Console.WriteLine(xx);
                x.Items.Add(xx);
            }
        }

        private void ColorBar_Click(object sender, RoutedEventArgs e)
        {

        }



        private void ColorBar_ColorChanged(object sender, EventArgs e)
        {
            var colorBar = (Button)FindName("colorBar");
            Color color = Color.FromRgb((byte)val_R, (byte)val_B, (byte)val_G);
            SolidColorBrush brush = new SolidColorBrush(color);
            colorBar.Background = brush;
        }

        private void ButtonPortOpen_Click(object sender, RoutedEventArgs e)
        {
            if (comboBoxPortName.SelectedItem != null)
            {
                //若已有打开的串口，先关闭
                if (mySerialPort != null)
                {
                    mySerialPort.DataReceived -= new SerialDataReceivedEventHandler(DataReceivedHandler);
                    mySerialPort.Close();
                }
                mySerialPort = new SerialPort(comboBoxPortName.SelectedItem.ToString());

                //串口参数设置
                mySerialPort.BaudRate = int.Parse(comboBoxBaudRate.SelectedItem.ToString());
                mySerialPort.Parity = Parity.None;
                mySerialPort.StopBits = StopBits.One;
                mySerialPort.DataBits = 8;
                mySerialPort.Handshake = Handshake.None;
                mySerialPort.RtsEnable = false;
                mySerialPort.ReceivedBytesThreshold = 1;
                mySerialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

                mySerialPort.Open();

                Console.WriteLine("Open Selected Port:" + comboBoxPortName.SelectedItem);
            }
            else
            {
                Console.WriteLine("No Selected Serial Port:");
                MessageBox.Show("Please select serial port", "Warning");
            }
        }
        

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            if (mySerialPort == null)
            {
                return;
            }
            int numOfByte = mySerialPort.BytesToRead;
            for (int i = 0; i < numOfByte; i++)
            {

                int indata = mySerialPort.ReadByte();

                //取bit7判断该字节对应指令还是数值
                if ((indata & 0x80) != 0)
                {
                    Console.Write("\n 指令:");
                    RxData.DataIdx = 0;
                    RxData.SerialDatas[RxData.DataIdx] = (byte)indata;
                    RxData.DataIdx++;
                }
                else if (RxData.DataIdx < RxData.SerialDatas.Length)
                {
                    Console.Write("{0:X2} ", indata);
                    RxData.SerialDatas[RxData.DataIdx] = (byte)indata;
                    RxData.DataIdx++;
                }

                if (RxData.DataIdx >= 3)
                {
                    System.DateTime ctime = System.DateTime.Now;
                    js.time = String.Format("{0:G}-{1:G}-{2:G}-{3:G}-{4:G}-{5:G}", ctime.Year, ctime.Month, ctime.Day, ctime.Hour, ctime.Minute, ctime.Second);

                    Console.WriteLine("Fist Byte is:");
                    Console.WriteLine(RxData.SerialDatas[0]);
                    if ((byte)RxData.SerialDatas[0] == 0xE3)
                    {
                        int ADval = RxData.SerialDatas[1] * 8 + (RxData.SerialDatas[2] >> 5);

                        double Vcc = 5, R5 = 10000, Vref = 5, T0 = 25 + 273.15, B = 3435, R0 = 10000;
                        double Vtemp = ADval * (Vref / 1024);
                        double R9 = Vcc * (R5 / Vtemp) - R5;
                        double Tx = 1 / (Math.Log(R9 / R0) / B + 1 / T0);
                        double val = Tx - 273.15;

                        double _val = Math.Round(val, 2);

                        AddDataPoint(_val, 0);
                        setTextIn(txtBox_Temperature, _val.ToString());



                        js.str = "温度";
                        js.val = _val;
                        Console.WriteLine("接收温度");

                    }
                    //Light
                    else if ((byte)RxData.SerialDatas[0] == 0xE4)
                    {
                        int ADval = RxData.SerialDatas[1] * 8 + (RxData.SerialDatas[2] >> 5);
                        double val = Math.Round((double)((ADval * 5) / 1024), 2);
                        AddDataPoint(val, 1);
                        setTextIn(txtBox_Light, val.ToString());
                        js.str = "光强";
                        js.val = val;
                        Console.WriteLine("接收光强");
                    }

                    else
                    {
                        //满足接收满3字节时输出一条Midi命令
                        string msg = string.Format("\n 接收指令: {0:X2}-{1:X2}-{2:X2}, 实值 = 0x{3:X4}", RxData.SerialDatas[0],
                            RxData.SerialDatas[1],
                            RxData.SerialDatas[2],
                            RxData.RealData);
                        SetTextInTextBox(txtRxData, msg);
                        RxData.FrameMsg = string.Format("ADVal=0x{0:X4}={0:d}", RxData.RealData);

                        js.str = "ADVal：";
                        js.val = RxData.RealData;

                        //Temp
                    }

                    if(fp!=null && File.Exists(fp) && (fp_status == 1))
                    {
                        File.AppendAllText(fp, JsonConvert.SerializeObject(js));
                    }

                }
            }
        }

        private delegate void setText(TextBox textBox, string s);
        public void setTextIn(TextBox textBox, string s)
        {
            if (textBox.Dispatcher.CheckAccess())
            {
                textBox.Text = s;
            }
            else
            {
                setText set = new setText(setTextIn);
                Dispatcher.Invoke(set, new object[] { textBox, s });
            }
        }

        private void wirteJson(string name, double val)
        {
            JObject source = new JObject();

        }
        private delegate void SetTextCallback(TextBox control, string text);
        public void SetTextInTextBox(TextBox control, string msg)
        {
            if (txtRxData.Dispatcher.CheckAccess())
            {
                txtRxData.AppendText(msg);
                txtRxData.ScrollToEnd();
            }
            else
            {
                SetTextCallback d = new SetTextCallback(SetTextInTextBox);
                Dispatcher.Invoke(d, new object[] { control, msg });
            }
        }

        private void ButtonPortStop_Click(object sender, RoutedEventArgs e)
        {
            CloseSerialPort();
        }
        void CloseSerialPort()
        {
            if (mySerialPort != null)
            {
                mySerialPort.DataReceived -= new SerialDataReceivedEventHandler(DataReceivedHandler);
                mySerialPort.Close();
                Console.WriteLine("关闭串口:" + mySerialPort.ToString());
            }

        }

        private void ButtonSendCmd_Click(object sender, RoutedEventArgs e)
        {
            byte[] txdat = TxData.TxBytes;
            if (txdat != null && mySerialPort != null)
            {
                mySerialPort.Write(txdat, 0, txdat.Length);
            }
        }

        private void ButtonLogStart_Click(object sender, RoutedEventArgs e)
        {
            var stack = FindName("logPart") as StackPanel;
            stack.Visibility = Visibility.Visible;
            System.DateTime ctime = System.DateTime.Now;
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.InitialDirectory = System.Windows.Forms.Application.StartupPath;
            sfd.Filter = "json文件（*.json）|*.json|所有文件（*.*）|*.*";
            sfd.FilterIndex = 1;
            sfd.DefaultExt = ".json";
            sfd.Title = "保存文件";
            sfd.FileName = String.Format("{0:G}-{1:G}-{2:G}-{3:G}-{4:G}-{5:G}", ctime.Year, ctime.Month, ctime.Day, ctime.Hour, ctime.Minute, ctime.Second);
            sfd.RestoreDirectory = true;
            if(sfd.ShowDialog() == true)
            {
                fp = sfd.FileName;
            }
            textBoxJsonName.Text = fp;
            
            if(fp!= null && !File.Exists(fp))
            {
                FileStream fs1 = new FileStream(fp, FileMode.Create, FileAccess.ReadWrite);

                fs1.Close();
            }
        }

        private void ButtonConfig_Click(object sender, RoutedEventArgs e)
        {
            //开始写入数据
            fp_status = 1;
            MessageBox.Show("开始记录数据");

        }

        private void ButtonRefuse_Click(object sender, RoutedEventArgs e)
        {
            if(File.Exists(fp))
            {
                File.Delete(fp);
                MessageBox.Show("已取消记录");
            }
            fp_status = 0;
            fp = " ";
            var stack = FindName("logPart") as StackPanel;
            stack.Visibility = Visibility.Hidden;
        }

        private void ButtonLogStop_Click(object sender, RoutedEventArgs e)
        {
            if(fp != null && File.Exists(fp))
            {
                string temp = fp;
                fp = " ";
                MessageBox.Show("Json文件" + temp + "已保存");
                logPart.Visibility = Visibility.Hidden;
                
            }
            fp_status = 0;

        }

        private void ButtonLight_Click(object sender, RoutedEventArgs e)
        {
            set_LED(1, val_Y);
            set_LED(2, val_G);
            set_LED(3, val_B);
            set_LED(4, val_R);
            set_LED(5, val_W);
        }

        private void set_LED(int tag, int val)
        {
            byte[] tData = new byte[3];
            byte[] cmd = { 0xD3, 0xD5, 0xD6, 0xD9, 0xDA};
            tData[0] = cmd[tag - 1];
            tData[1] = (byte)(val << 1);
            tData[2] = (byte)(val << 7);
            if(mySerialPort!=null)
            {
                mySerialPort.Write(tData, 0, 3);
                Console.WriteLine("{0:X2}{1:X2}{2:X2}", tData[0], tData[1], tData[2]);
            }
        }
        private void SetGraph()
        {
            GraphPane pane1 = zedGraph_Light.GraphPane;
            GraphPane pane2 = zedGraph_Temperature.GraphPane;

            /// 设置标题
            pane1.Title.Text = "光照强度";
            pane1.XAxis.Title.Text = "时间/秒";
            pane1.YAxis.Title.Text = "光强";

            pane2.Title.Text = "温度";
            pane2.XAxis.Title.Text = "时间/秒";
            pane2.YAxis.Title.Text = "温度/°C";

            /// 设置数据列表包含1200个点，每50毫秒更新一次，恰好检测1分钟
            RollingPointPairList list1 = new RollingPointPairList(1200);
            RollingPointPairList list2 = new RollingPointPairList(1200);

            /// 根据数据列表添加折线
            LineItem curve1 = pane1.AddCurve("光强", list1, System.Drawing.Color.YellowGreen, SymbolType.None);
            LineItem curve2 = pane2.AddCurve("温度", list2, System.Drawing.Color.Red, SymbolType.None);

            ///坐标轴属性
            pane1.YAxis.IsVisible = true;
            pane1.YAxis.Scale.Min = -5.0;
            pane1.YAxis.Scale.Max = 5.0;
            pane1.YAxis.Scale.Align = AlignP.Inside;
            pane1.YAxis.Scale.MaxAuto = false;
            pane1.YAxis.Scale.MinAuto = false;

            pane2.YAxis.IsVisible = true;
            pane2.YAxis.Scale.Min = -5.0;
            pane2.YAxis.Scale.Max = 30.0;
            pane2.YAxis.Scale.Align = AlignP.Inside;
            pane2.YAxis.Scale.MaxAuto = false;
            pane2.YAxis.Scale.MinAuto = false;

            /// X 轴最小值 0  
            pane1.XAxis.Scale.Min = 0;
            pane1.XAxis.Scale.MaxGrace = 0.01;
            pane1.XAxis.Scale.MinGrace = 0.01;

            pane2.XAxis.Scale.Min = 0;
            pane2.XAxis.Scale.MaxGrace = 0.01;
            pane2.XAxis.Scale.MinGrace = 0.01;
            /// X轴最大30 
            pane1.XAxis.Scale.Max = 30;
            pane2.XAxis.Scale.Max = 30;

            /// X轴小步长1,也就是小间隔
            pane1.XAxis.Scale.MinorStep = 1;
            pane2.XAxis.Scale.MinorStep = 1;

            /// X轴大步长为5，也就是显示文字的大间隔
            pane1.XAxis.Scale.MajorStep = 5;
            pane2.XAxis.Scale.MajorStep = 5;

            ///保存开始时间
            tickStart = Environment.TickCount;
            /// Scale the axes 
            /// 改变轴的刻度
            zedGraph_Light.AxisChange();
            zedGraph_Temperature.AxisChange();
        }

        void AddDataPoint(double dataX, int tag)
        {
            ZedGraphControl graph = (tag == 0) ? zedGraph_Temperature : zedGraph_Light;

            //确保CurveList不为空
            if (graph.GraphPane.CurveList.Count <= 0) return;

            //取Graph第一个曲线，也就是第一步:在 GraphPane.CurveList 集合中查找 CurveItem 
            for (int idxList = 0; idxList < graph.GraphPane.CurveList.Count; idxList++)
            {
                LineItem curve = graph.GraphPane.CurveList[idxList] as LineItem;
                if (curve == null) return;

                //第二步:在CurveItem中访问PointPairList(或者其它的IPointList)，根据自己的需要增加新数据或修改已存在的数据
                IPointListEdit list = curve.Points as IPointListEdit;
                if (list == null) return;

                // 毫秒转秒 
                double time = (Environment.TickCount - tickStart) / 1000.0;
                list.Add(time, dataX);

                // 更改坐标轴左右端的值
                Scale xScale = graph.GraphPane.XAxis.Scale;
                if (time > xScale.Max - xScale.MajorStep)
                {
                    xScale.Max = time + xScale.MajorStep;
                    xScale.Min = xScale.Max - 30.0;
                }

            }
            //第三步:调用ZedGraphControl.AxisChange()方法更新X和Y轴的范围
            graph.AxisChange();  

            //第四步:调用Form.Invalidate()方法更新图表
            graph.Invalidate();
        }
    }
}
