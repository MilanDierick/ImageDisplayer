using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Runtime.CompilerServices;
using System.Drawing;
namespace UDPclient
{
    class Program
    {
        private const int YFrameWidth = 256;
        private const int YFrameHeight = 192;
        private const int YFrameSize = YFrameWidth * YFrameHeight;

        private const int CFrameWidth = YFrameWidth / 2;
        private const int CFrameHeight = YFrameHeight / 2;
        private const int CFrameSize = CFrameWidth * CFrameHeight;

        private const int FrameSize = YFrameSize + 2 * CFrameSize;

        private const int PacketHeaderSize = 4 * 3;
        private const int PacketPayloadSize = 1024;
        private const int PacketSize = PacketHeaderSize + PacketPayloadSize;

        private readonly byte[] _yuvBuffer = new byte[FrameSize];

        private MemoryStream _yuvStream;
        private MemoryStream _packetStream;
        private BinaryWriter _packetWriter;

        public void Run()
        {
            UdpSocket c = new UdpSocket();
            c.Client("127.0.0.1", 27000);

            _yuvStream = new MemoryStream(File.ReadAllBytes(Environment.CurrentDirectory + "/image.yuv"));

            // HACK: Wait for server to be ready to receive, we don't have a hand-shake yet.
            Thread.Sleep(1000);

            _packetStream = new MemoryStream(PacketSize);
            _packetWriter = new BinaryWriter(_packetStream);

            int frameIndex = 0;

            while (!Console.KeyAvailable || Console.ReadKey(false).Key != ConsoleKey.Escape)
            {
                if (_yuvStream.Read(_yuvBuffer, 0, FrameSize) <= 0)
                {
                    _yuvStream.Position = 0;
                }

                for (int offset = 0; offset < FrameSize; ++offset)
                {
                    _packetWriter.Seek(0, SeekOrigin.Begin);

                    _packetWriter.Write(frameIndex);

                    _packetWriter.Write(offset);

                    var length = Math.Min(PacketPayloadSize, FrameSize - offset);
                    _packetWriter.Write(length);

                    _packetWriter.Write(_yuvBuffer, offset, length);

                    c.Send(_packetStream.GetBuffer());
                }

                ++frameIndex;

                // TODO: This is not precise, Windows timers often have a resolution of 18ms...
                Thread.Sleep(1000 / 60);
            }


            Console.ReadLine();
        }


        static void Main(string[] args)
        {
            var prg = new Program();
            prg.Run();

        }
    }
}
