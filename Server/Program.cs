using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServerCore;

namespace Server
{
    
    class Program
    {
        //static 메서드에서 일반 변수에 접근하려면 일단 객체를 생성하고 나서 가능하다.
        static Listener _listener = new Listener();


        static void Main(string[] args)
        {

            //Socket은 인자를 넘겨줘야함. IP주소 , TCP인지UDP인지 정해줘야함
            //Dns사용 
            string host = Dns.GetHostName(); //내 IP 추출
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddr = ipHost.AddressList[0];
            IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);

            _listener.Init(endPoint, () => { return new ClientSession(); });
            Console.WriteLine("Listening...");


            while (true)
            {
                ;
            }



        }
    }
}
