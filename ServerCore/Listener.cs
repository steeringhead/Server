using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    public class Listener
    {
        //소켓 생성 코드를 분리시키기 위한 작업
        Socket _listenSocket;
        //Action은 반환값이 없는 인자만 존재하는 delegate
        Func<Session> _sessionFactory; 

        //소켓 생성을 위해 EndPoint가 필요하기 때문에 인자로 받는다
        public void Init(IPEndPoint endPoint, Func<Session> sessionFactory)
        {
            _listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _sessionFactory += sessionFactory;
            //소켓을 생성했으니 EndPoint와 연결(Bind) + 대기(Listen)진행
            _listenSocket.Bind(endPoint);
            _listenSocket.Listen(10);

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
            RegisterAccept(args);
        }

        //비동기 방식으로 Accept를 처리해줘야함. Blocking방식은 한계가 많음.
        //비동기 방식으로 Accept를 완료했을 때, 콜백함수를 실행시켜 그 후의 동작들을 설계하자.
        //그렇다면 SocketAsyncEventArgs는 무엇이냐 ? -> 비동기 방식으로 동작하는 소켓의 로직들을 이벤트 형식으로 사용할수 있게 도와주는 클래스 
        public void RegisterAccept(SocketAsyncEventArgs args)
        {
            //null로 밀어주는 이유는 그전의 통신으로 인해 AcceptSocket에 데이터가 이미 사용했던 저장되어 있기 때문!
            args.AcceptSocket = null;

                       
            bool pending = _listenSocket.AcceptAsync(args);
            if (pending == false) //pending이 true면 대기하다 Accept가 실행될때,이벤트를 통해 콜백을 실행시켜주겠단 뜻
            {
                OnAcceptCompleted(null,args);
            }
        }

        //OAC 함수가 실행될때 워커스레드에서 실행됨(따로 스레드를 생성해주지 않았음에도 불구하고). 주 스레드는 다른 코드를 실행중이다. 그렇다면 멀티스레드 환경에서의 경합조건을 잘 고려해서 코드를 짜야한다는것!
        public void OnAcceptCompleted(object sender, SocketAsyncEventArgs args)
        {
            //connect과 Accept가 성공했을 때, 하고 싶은 동작들을 여기에 구현하는것.
            if(args.SocketError == SocketError.Success)
            {
                //TODO
                //여기선 뭘해야하냐 -> Accept까지 이루어졌으니 Recv Send의 과정이 들어가야겠지.
                //Recv와 Send를 어떻게 구현할지 생각해보기.
                Session session = _sessionFactory.Invoke();
                session.Start(args.AcceptSocket);
                session.OnConnected(args.AcceptSocket.RemoteEndPoint);

                //onAcceptHandler에 Accept이후 해야 할 것들을 담아두고, 여기서 Invoke를 통해 동작시키기.
               // _onAcceptHandler.Invoke(); //onAcceptHandler가 Socket을 인자로 받으니까 AcceptSocket으로 Socket을 뱉어주고 있다.


            }
            else
            {
                Console.WriteLine(args.SocketError.ToString());
            }
            //하나의 통신이 성공했으니, 다음 통신을 위해 Accept를 다시 예약해줌.
            RegisterAccept(args);
        }
        

    }
}
