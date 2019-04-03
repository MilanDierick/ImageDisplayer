using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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


        private Bitmap _rgbBitmap = new Bitmap(YFrameWidth, YFrameHeight, PixelFormat.Format24bppRgb);

        private MemoryStream _yuvStream;

        private Timer _timer = new Timer();

        private readonly byte[] _y = new byte[YFrameSize];
        private readonly byte[] _u = new byte[YFrameSize];
        private readonly byte[] _v = new byte[YFrameSize];


        public ImageView()
        {
            InitializeComponent();

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.Opaque, true);
            SetStyle(ControlStyles.DoubleBuffer|ControlStyles.OptimizedDoubleBuffer, false);

            _yuvStream = new MemoryStream(File.ReadAllBytes(Environment.CurrentDirectory + "/image.yuv"));

            var displayer = new Action(DisplayNextFrame);
            _timer.Interval = 1000/60;
            _timer.Tick += delegate { Invoke(displayer); };
            _timer.Start();

            Invalidate();
            Update();
        }

        private unsafe void DisplayNextFrame()
        {
            if (_yuvStream.Read(_y, 0, YFrameSize) <= 0 ||
                _yuvStream.Read(_u, 0, CFrameSize) <= 0 ||
                _yuvStream.Read(_v, 0, CFrameSize) <= 0)
            {
                _yuvStream.Position = 0;
                return;
            }

            var data = _rgbBitmap.LockBits(new Rectangle(0, 0, YFrameWidth, YFrameHeight), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            var rgb = (byte*)data.Scan0.ToPointer();

            for (int y = 0; y < YFrameHeight; y++)
            {
                var yOffset = y * YFrameWidth;
                var cOffset = (y>>1) * CFrameWidth;

                var rgbOffset = y * data.Stride;

                for (int x = 0, t = 0; x < YFrameWidth; x++, t += 3)
                {
                    var C = _y[yOffset + x] - 16;
                    var D = _u[cOffset + (x >> 1)] - 128;
                    var E = _v[cOffset + (x >> 1)] - 128;
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
            if (_yuvStream.Position == 0)
            {
                e.Graphics.Clear(Color.SkyBlue);
            }

            e.Graphics.DrawImage(_rgbBitmap, 0, 0);

            base.OnPaint(e);
        }

        private void ImageView_Load(object sender, EventArgs e)
        {

        }
    }
}
