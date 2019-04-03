using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Forms;

namespace ImageDisplayer
{
    public partial class ImageView : Form
    {
        private const int YFrameWidth = 256;
        private const int YFrameHeight = 192;
        private const int YFrameSize = YFrameWidth * YFrameHeight;

        private const int CFrameWidth = YFrameWidth / 2;
        private const int CFrameHeight = YFrameHeight / 2;
        private const int CFrameSize = CFrameWidth * CFrameHeight;

        private const int FrameSize = YFrameSize + 2 * CFrameSize;

        private const int PacketHeaderSize = 3 * 4;
        private const int PacketPayloadSize = 1024;
        private const int PacketSize = PacketHeaderSize + PacketPayloadSize;

        private readonly Bitmap _rgbBitmap = new Bitmap(YFrameWidth, YFrameHeight, PixelFormat.Format24bppRgb);
        private readonly byte[] _yuvBuffer = new byte[FrameSize];

        private readonly byte[] _packetBuffer = new byte[PacketSize];

        private Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private bool _firstFrame = true;

        public ImageView()
        {
            InitializeComponent();

            _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
            _socket.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 27000));

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.Opaque, true);
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.OptimizedDoubleBuffer, false);

            // Start receiving first frame
            int frameIndex = 0;

            while (true)
            {
                int count = _socket.Receive(_packetBuffer);

                if (count > PacketHeaderSize)
                {
                    using (var packetStream = new MemoryStream(_packetBuffer))
                    using (var packetReader = new BinaryReader(packetStream))
                    {
                        var index = packetReader.ReadInt32();
                        var offset = packetReader.ReadInt32();
                        var length = packetReader.ReadInt32();

                        if (count - PacketHeaderSize >= length)
                        {
                            packetReader.Read(_yuvBuffer, offset, length);
                        }

                        if (index != frameIndex)
                        {
                            DisplayNextFrame();
                            Update();
                            Application.DoEvents();
                        }
                    }
                }
            }
        }

        private unsafe void DisplayNextFrame()
        {
            var data = _rgbBitmap.LockBits(new Rectangle(0, 0, YFrameWidth, YFrameHeight), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            var rgb = (byte*)data.Scan0.ToPointer();

            for (int y = 0; y < YFrameHeight; y++)
            {
                var yOffset = y * YFrameWidth;
                var cOffset = (y >> 1) * CFrameWidth;
                var uOffset = YFrameSize + cOffset;
                var vOffset = CFrameSize + uOffset;

                var rgbOffset = y * data.Stride;

                for (int x = 0, t = 0; x < YFrameWidth; x++, t += 3)
                {
                    var C = _yuvBuffer[yOffset + x] - 16;
                    var D = _yuvBuffer[uOffset + (x >> 1)] - 128;
                    var E = _yuvBuffer[vOffset + (x >> 1)] - 128;
                    rgb[rgbOffset + t + 0] = AsByte((298 * C + 409 * E + 128) >> 8);
                    rgb[rgbOffset + t + 1] = AsByte((298 * C - 100 * D - 208 * E + 128) >> 8);
                    rgb[rgbOffset + t + 2] = AsByte((298 * C + 516 * D + 128) >> 8);
                }
            }

            _rgbBitmap.UnlockBits(data);

            Invalidate();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte AsByte(int value)
        {
            //return (byte)value;
            if (value > 255)
            {
                return 255;
            }

            if (value < 0)
            {
                return 0;
            }

            return (byte)value;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_firstFrame)
            {
                e.Graphics.Clear(Color.SkyBlue);
                _firstFrame = false;
            }

            e.Graphics.DrawImage(_rgbBitmap, 0, 0);

            base.OnPaint(e);
        }

        private void ImageView_Load(object sender, EventArgs e)
        {

        }
    }
}
