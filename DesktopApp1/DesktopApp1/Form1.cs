using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopApp1
{
    

    public partial class Form1 : Form
    {
        Queue<Pointx> points = new Queue<Pointx>();


        private SerialPort port = new SerialPort("COM8",
     115200, Parity.None, 8, StopBits.One);

        byte[] start = new byte[1] { 0xA5 };
        byte[] getInfo = new byte[1] { 0x50 };
        byte[] getHealth = new byte[1] { 0x52 };
        byte[] scan = new byte[1] { 0x20 };
        byte[] getSampleRate = new byte[1] { 0x59 };
        byte[] stop = new byte[1] { 0x25 };

        bool constant = false;

        Queue<List<byte>> queue = new Queue<List<byte>>();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            port.DataReceived += new
SerialDataReceivedEventHandler(port_DataReceived);

            Thread thread = new Thread(DoWork);
            thread.IsBackground = true;
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Begin communications
            port.Open();

        }
        byte[] datax = new byte[5];

        private void port_DataReceived(object sender,
      SerialDataReceivedEventArgs e)
        {
            if(port.BytesToRead>0)
            {
                List<byte> bytes = new List<byte>();
                bytes.Add((byte)port.ReadByte());
                bytes.Add((byte)port.ReadByte());

                bytes.Add((byte)port.ReadByte());
                bytes.Add((byte)port.ReadByte());
                bytes.Add((byte)port.ReadByte());
                bytes.Add((byte)port.ReadByte());
                bytes.Add((byte)port.ReadByte());

                int length = 0;
                length += (bytes[2]);
                length += (bytes[3] << 8);
                length += (bytes[4] << 16);
                length += ((bytes[5] >> 2) << 24);

                int mode = 0;
                mode += (bytes[5] & 0b0000_0011);

                byte dataType = bytes[6];

                if (mode==2220)
                {
                    byte[] data = new byte[length];
                    port.Read(data, 0, data.Length);
                    bytes.AddRange(data);

                    queue.Enqueue(bytes);
                }else 
                {
                    constant = true;
                    while(constant)
                    {                                           
                        port.Read(datax, 0, 5);
                        ParseScanData(datax);
                    }                  
                }
            }
        }

        private void ParseScanData(byte[] datax)
        {
        //    int newOne = (datax[0] & 0b00000001);
           

          //  int quality = (datax[0] >> 2);
        //    int angle = (datax[1]>>1);
            int eee = datax[1]& 0b00000001;
          //  angle += (datax[2]<<7);
         //   int distance = (datax[3]<<0);
       //     distance += (datax[4] << 8);
            /* if (angle > 0 && angle<500)
             {
                 Console.WriteLine("Scan: [Quality:{0}] [Angle:{1}] [Distance:{2}]", quality, angle, distance);
             }*/
            Console.WriteLine(eee);
       //     points.Enqueue(new Pointx() {distance=distance,angle=angle});
              
        }

        private void button2_Click(object sender, EventArgs e)
        {           
            port.Write(start, 0, 1);
            port.Write(getInfo, 0, 1);
        }
        
        private void button3_Click(object sender, EventArgs e)
        {           

        }
        
        void analyzeCommand(List<byte> bytes)
        {
            switch(bytes[6])
            {
                case 4:
                    AnGetInfo(bytes);
                    break;
                case 6:
                    AnGetHealth(bytes);
                    break;
                case 21:
                    AnGeSampleRate(bytes);
                    break;
                default:
                    Console.WriteLine("Uknown message: "+bytes[7]);
                    break;
            }        
        }

        private void AnGeSampleRate(List<byte> bytes)
        {
            int std = bytes[0 + 7];
            std +=( bytes[1 + 7]<<8);
            int exp = bytes[2 + 7];
            exp +=( bytes[3 + 7]<<8);


            Console.WriteLine("SystemInfo: [Standard={0}] [Express={1}]", std, exp);
        }



        private void AnGetInfo(List<byte> bytes)
        {
            byte model = bytes[0 + 7];
            byte fwMinor = bytes[1 + 7];
            byte fwMajor = bytes[2 + 7];
            byte hardware = bytes[3 + 7];

            Console.WriteLine("SystemInfo: [MODEL={0}] [FirmwareMinor={1}] [FirmwareMajor={2}] [HardwareVersion={3}]", model, fwMinor, fwMinor, hardware);
        }

        private void AnGetHealth(List<byte> bytes)
        {
            byte status = bytes[0 + 7];
            byte[] error =new byte[] { bytes[1 + 7], bytes[2 + 7] };
            Status st = (Status)(status);

            Console.WriteLine("Health: [Status={0}] [ErrorCode={1}]",st.ToString(),Encoding.ASCII.GetString(error));
        }

        void DoWork()
        {
            while(true)
            {
                if(queue.Count>0)
                {
                    analyzeCommand(queue.Dequeue());
                }
            }
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            port.Write(start, 0, 1);
            port.Write(getHealth, 0, 1);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            port.Write(start, 0, 1);
            port.Write(getSampleRate, 0, 1);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            port.Write(start, 0, 1);
            port.Write(scan, 0, 1);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            port.Write(start, 0, 1);
            port.Write(stop, 0, 1);
            constant = false;
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            while(points.Count>0)
            {
                Pointx pp = points.Dequeue();
                if(pp!=null && pp.angle!=null && pp.distance!=null && pp.distance!=0)
                {
                    int x = (int)((pp.distance / 1500.0) * Math.Cos(((pp.angle * 64.0) / 360) * (2 * Math.PI)));
                    int y = (int)((pp.distance / 1500.0) * Math.Sin(((pp.angle * 64.0) / 360) * (2 * Math.PI)));
                    g.DrawRectangle(Pens.Red, new Rectangle(x+300, y+200, 1, 1));
                }
             
            }

            g.DrawRectangle(Pens.Red, new Rectangle(200, 200, 1, 1));
           
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            pictureBox1.Invalidate();
        }
    }

    public class Pointx
    {
        public int angle;
        public int distance;
    }

    public enum Status
    {
        Good,
        Warning,
        Error,
    }



}
