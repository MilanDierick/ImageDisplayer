using System;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
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

        private const int BitmapCount = 3;

        private readonly ConcurrentQueue<int> _producerQueue = new ConcurrentQueue<int>();
        private readonly ConcurrentQueue<int> _consumerQueue = new ConcurrentQueue<int>();

        private readonly Bitmap[] _rgbBitmaps = new Bitmap[BitmapCount];

        private YuvBuffer _yuvBuffer0 = new YuvBuffer {Index = 0};
        private YuvBuffer _yuvBuffer1 = new YuvBuffer {Index = 1};

        private readonly byte[] _packetBuffer = new byte[PacketSize];

        private bool _firstFrame = true;

        private Thread _socketThread;

        class YuvBuffer
        {
            public int Index;
            public int Length;
            public byte[] Data;

            public YuvBuffer()
            {
                Data = new byte[FrameSize];
            }
        }


        public ImageView()
        {
            InitializeComponent();

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.Opaque, true);
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.OptimizedDoubleBuffer, false);

            for (int i = 0; i < _rgbBitmaps.Length; ++i)
            {
                _rgbBitmaps[i] = new Bitmap(YFrameWidth, YFrameHeight, PixelFormat.Format24bppRgb);
                _producerQueue.Enqueue(i);
            }

            _socketThread = new Thread(SocketThread)
            {
                Priority = ThreadPriority.Highest
            };

            _socketThread.Start();
        }

        private void SocketThread()
        {
            var repaint = new Action(Invalidate);

            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
                socket.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 27000));

                var sw = new Stopwatch();
                sw.Start();

                int lastBitmapIndex = -1;

                // We allow two consecutive frames to be received, if a packet of the third frame arrives, drop the first frame.
                while (Thread.CurrentThread.IsAlive)
                {
                    int count = socket.Receive(_packetBuffer);

                    if (count > PacketHeaderSize)
                    {
                        using (var packetStream = new MemoryStream(_packetBuffer))
                        using (var packetReader = new BinaryReader(packetStream))
                        {
                            var index = packetReader.ReadInt32();
                            var offset = packetReader.ReadInt32();
                            var length = packetReader.ReadInt32();

                            YuvBuffer yuvBuffer = null;

                            if (_yuvBuffer0.Index == index)
                            {
                                yuvBuffer = _yuvBuffer0;
                            }
                            else if (_yuvBuffer1.Index == index)
                            {
                                yuvBuffer = _yuvBuffer1;
                            }
                            else
                            {
                                yuvBuffer = _yuvBuffer0;

                                // Drop oldest frame, unless it was fully received.
                                if (yuvBuffer.Length != FrameSize)
                                {
                                    Console.WriteLine($"Dropping frame {yuvBuffer.Index}");
                                }

                                _yuvBuffer0 = _yuvBuffer1;
                                _yuvBuffer1 = yuvBuffer;
                                yuvBuffer.Index = index;
                                yuvBuffer.Length = 0;
                            }


                            if (count - PacketHeaderSize >= length)
                            {
                                packetReader.Read(yuvBuffer.Data, offset, length);
                                yuvBuffer.Length += length;
                            }

                            if (yuvBuffer.Length == FrameSize)
                            {
                                // Console.WriteLine($"Buffer {yuvBuffer.Index} received at {sw.Elapsed.TotalMilliseconds:0000.0}ms");

                                // Don't paint buffers that arrive too late / out of order
                                if (lastBitmapIndex < yuvBuffer.Index)
                                {
                                    lastBitmapIndex = yuvBuffer.Index;

                                    // Buffer is completely received, paint it
                                    if (_producerQueue.TryDequeue(out var bitmapIndex))
                                    {
                                        CopyFrameToBitmap(yuvBuffer.Data, _rgbBitmaps[bitmapIndex]);
                                        _consumerQueue.Enqueue(bitmapIndex);
                                        BeginInvoke(repaint);
                                    }
                                    else
                                    {
                                        Console.WriteLine("PC: No bitmap available");
                                    }
                                }

                            }
                        }
                    }
                }

            }
        }

        private static unsafe void CopyFrameToBitmap(byte[] yuvBuffer, Bitmap rgbBitmap)
        {
            var data = rgbBitmap.LockBits(new Rectangle(0, 0, YFrameWidth, YFrameHeight), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

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
                    var C = yuvBuffer[yOffset + x] - 16;
                    var D = yuvBuffer[uOffset + (x >> 1)] - 128;
                    var E = yuvBuffer[vOffset + (x >> 1)] - 128;
                    rgb[rgbOffset + t + 0] = AsByte((298 * C + 409 * E + 128) >> 8);
                    rgb[rgbOffset + t + 1] = AsByte((298 * C - 100 * D - 208 * E + 128) >> 8);
                    rgb[rgbOffset + t + 2] = AsByte((298 * C + 516 * D + 128) >> 8);
                }
            }

            rgbBitmap.UnlockBits(data);
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

            if (_consumerQueue.TryDequeue(out var bitmapIndex))
            {
                e.Graphics.DrawImage(_rgbBitmaps[bitmapIndex], 0, 0);
                _producerQueue.Enqueue(bitmapIndex);
            }

            base.OnPaint(e);
        }

        private void ImageView_Load(object sender, EventArgs e)
        {

        }
    }
}
