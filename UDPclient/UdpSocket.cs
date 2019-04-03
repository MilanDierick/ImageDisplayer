using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace UDPclient
{
    public class UdpSocket
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

        public void Send(byte[] data)
        {
            _socket.Send(data);

            //_socket.BeginSend(data, 0, data.Length, SocketFlags.None, (ar) =>
            //{
            //    State so = (State)ar.AsyncState;
            //    int bytes = _socket.EndSend(ar);
            //    Console.WriteLine("{0} - {1}", bytes, text);
            //}, state);
        }
    }
}