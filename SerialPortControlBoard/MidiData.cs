using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SerialPortControlBoard
{
    class MidiData:INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        //使用调用者的名称或属性作为参数  
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            //属性改变时对参数进行监听
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //串口传来的数据
        private byte[] serialdatas;
        public byte[] SerialDatas
        {
            get
            {
                return serialdatas;
            }

            set
            {
                serialdatas = value;
                NotifyPropertyChanged();
            }
        }

        //标注当前读取字节在一条指令中的位置
        public int DataIdx { get; set; }

        //框架信息？
        private string framemsg;
        public string FrameMsg
        {
            get
            {
                return framemsg;
            }

            set
            {
                if (framemsg != value)
                {
                    framemsg = value;
                    NotifyPropertyChanged();
                }
            }
        }

        //Midi指令中存储的实际数值
        public int RealData
        {
            get
            {
                return (SerialDatas[1] * 8) + (SerialDatas[2] >> 5);
            }
        }

        public MidiData()
        {
            serialdatas = new byte[3];
            serialdatas[0] = 0;
            serialdatas[1] = 0;
            serialdatas[2] = 0;
            DataIdx = 0;
        }

        //输出的指令
        private string txString;

        public string TxString
        {
            get
            {
                return txString;
            }

            set
            {
                if (txString != value)
                {
                    txString = value;
                    NotifyPropertyChanged();
                }
            }
        }

        //输出指令的Bytes形式
        public byte[] TxBytes
        {
            get
            {
                if (string.IsNullOrEmpty(txString))
                {
                    return null;
                }
                string[] splited = txString.Split(new Char[] { ' ', ',', '.', ':', '\t' });
                byte[] txDataBuf = new byte[splited.Length];
                for (int i = 0; i < splited.Length; i++)
                {
                    if (!(byte.TryParse(splited[i], NumberStyles.HexNumber, null, out txDataBuf[i])))
                    {
                        txDataBuf[i] = 0;
                    }
                }
                return txDataBuf;
            }
        }
    }
}
