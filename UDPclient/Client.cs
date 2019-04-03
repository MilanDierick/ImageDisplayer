using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Text;
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

        private readonly byte[] _yuvBuffer = new byte[FrameSize];

        private MemoryStream _yuvStream;

        public void Run()
        {
            UDPSocket c = new UDPSocket();
            c.Client("127.0.0.1", 27000);

            _yuvStream = new MemoryStream(File.ReadAllBytes(Environment.CurrentDirectory + "/image.yuv"));

            Thread.Sleep(1000);

            c.Send("TEST");
            Console.ReadLine();
        }


        static void Main(string[] args)
        {
            var prg = new Program();
            prg.Run();
           
        }
    }


    public class UDPSocket
    {
        private Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private const int bufsize = 6 * 245;
        private State state = new State();
        private EndPoint epFrom = new IPEndPoint(IPAddress.Any, 0);
        private AsyncCallback recv = null;

        public class State
        {
        public byte[] buffer = new byte[bufsize];
        }

        private void Receive()
        {
            _socket.BeginReceiveFrom(state.buffer, 0, bufsize, SocketFlags.None, ref epFrom, recv = (ar) =>
            {
                State so = (State)ar.AsyncState;
                int bytes = _socket.EndReceiveFrom(ar, ref epFrom);
                _socket.BeginReceiveFrom(so.buffer, 0, bufsize, SocketFlags.None, ref epFrom, recv, so);
                Console.WriteLine("RECV: {0}: {1}, {2}", epFrom.ToString(), bytes, Encoding.ASCII.GetString(so.buffer, 0, bytes));
            }, state);
        }

        public void Client(string address, int port)
        {
            _socket.Connect(IPAddress.Parse(address), port);
            //Receive();
        }

        public void Send(string text)
        {
            byte[] data = Encoding.ASCII.GetBytes(text);
            _socket.BeginSend(data, 0, data.Length, SocketFlags.None, (ar) =>
            {
                State so = (State)ar.AsyncState;
                int bytes = _socket.EndSend(ar);
                Console.WriteLine("{0} - {1}", bytes, text);
            }, state);
        }
    }

}
