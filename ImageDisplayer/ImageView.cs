using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ImageDisplayer
{
    public partial class ImageView : Form
    {
        private const int ImageWidth = 256;
        private const int ImageHeight = 192;
        private readonly int _counterY = 0;
        private readonly int _counterU = 0;
        private readonly int _counterV = 0;
        private readonly bool _isFirstCycle = true;

        private readonly byte[] _buffer = File.ReadAllBytes(Environment.CurrentDirectory + "/image.yuv");

        private readonly byte[] _y;
        private readonly byte[] _u;
        private readonly byte[] _v;
        private readonly byte[] _r;
        private readonly byte[] _g;
        private readonly byte[] _b;

        private Bitmap bmp;

        public ImageView()
        {
            InitializeComponent();

            _r = new byte[ImageWidth * ImageHeight];
            _g = new byte[ImageWidth * ImageHeight];
            _b = new byte[ImageWidth * ImageHeight];

            _y = _buffer.Skip(0).Take(ImageWidth * ImageHeight).ToArray();
            _u = _buffer.Skip(ImageWidth * ImageHeight).Take(ImageWidth * ImageHeight / 4).ToArray();
            _v = _buffer.Skip(ImageWidth * ImageHeight + ImageWidth * ImageHeight / 4).Take(ImageWidth * ImageHeight).ToArray();

            for (int y = 0; y < ImageHeight; y++)
            {
                for (int x = 0; x < ImageWidth; x++)
                {
                    int i = x + y * ImageWidth;
                    int j = (x >> 1) + (y >> 1) * (ImageWidth >> 1);

                    var Y = _y[i] - 16;
                    var U = _u[j] - 128;
                    var V = _v[j] - 128;

                    var R = 1.164F * Y + 1.596F * V;
                    var G = 1.164F * Y - 0.813F * V - 0.391F * U;
                    var B = 1.164F * Y + 2.018F * U;

                    _r[i] = AsByte(R);
                    _g[i] = AsByte(G);
                    _b[i] = AsByte(B);

                    //_r[i] = AsByte((298 * c + 409 * e + 128) >> 8);
                    //_g[i] = AsByte((298 * c - 100 * d - 208 * e + 128) >> 8);
                    //_b[i] = AsByte((298 * c + 516 * d + 128) >> 8);
                }
            }

            // https://docs.microsoft.com/en-us/previous-versions/aa917087(v=msdn.10)
            //while (_counterY < ImageWidth * ImageHeight && _counterU < ImageWidth * ImageHeight / 4 && _counterV < ImageWidth * ImageHeight / 4)
            //{
            //    c = _y[_counterY];

            //    if (_counterY % 3 == 0 || _isFirstCycle)
            //    {
            //        d = _u[_counterU];
            //        e = _v[_counterV];
            //    }

            //    _r[_counterY] = AsByte((298 * c + 409 * e + 128) >> 8);
            //    _g[_counterY] = AsByte((298 * c - 100 * d - 208 * e + 128) >> 8);
            //    _b[_counterY] = AsByte((298 * c + 516 * d + 128) >> 8);

            //    ++_counterY;
            //}

            bmp = new Bitmap(ImageWidth, ImageHeight, PixelFormat.Format24bppRgb);

            for (int y = 0; y < ImageHeight; y++)
            {
                for (int x = 0; x < ImageWidth; x++)
                {
                    int i = x + y * ImageWidth;
                    Color c = Color.FromArgb(_r[i], _g[i], _b[i]);
                    bmp.SetPixel(x, y, c);
                }
            }

            Invalidate();

            // TODO: Convert r, g, b arrays to bitmap
        }

        private static byte AsByte(float value)
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
            e.Graphics.DrawImage(bmp, 0, 0);
            base.OnPaint(e);
        }

        private void ImageView_Load(object sender, EventArgs e)
        {

        }
    }
}
