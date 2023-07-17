using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerCore
{
    public abstract class PacketSession: Session
    {
        public static readonly short HeaderSize = 2;
        //데이터 파싱할때 , 넘어온 패킷의 필수값(size,packetId)이 ushort형이라 [2바이트][2바이트][...] 형태로 넘어옴.
        public sealed override int OnRecv(ArraySegment<byte> buffer)
        {
            int processLen = 0;

            while (true)
            {
                if (buffer.Count < HeaderSize)
                    break; // 최소한 헤더는 파싱이 가능한지 먼저 확인.

                //패킷이 완전체로 도착했는지 확인 (ToUInt16이란건 바이트 배열을 16비트 부호없는 정수로 변환해주는 함수)
                ushort dataSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
                if (buffer.Count < dataSize) // 패킷이 완전체로 안오고 덜왔다는 뜻!
                    break;

                //여기까지 왔으면 패킷을 조립 가능
                OnRecvPacket(new ArraySegment<byte>(buffer.Array,buffer.Offset,dataSize));

                processLen += dataSize;
                buffer = new ArraySegment<byte>(buffer.Array, buffer.Offset + dataSize, buffer.Count - dataSize);

            }

            return processLen;
        }

        public abstract void OnRecvPacket(ArraySegment<byte> buffer);
    }
    //세션이란 소켓통신을 시작하고 클라와 서버가 데이터를 주고받는 과정을 담당하는 클래스라고 생각하면됨.
    public abstract class Session
    {
        Socket _socket; //이 소켓에는 Accept가 성공한 소켓이 들어가겠지.
        int _disconnected = 0;

        RecvBuffer _recvBuffer = new RecvBuffer(1024);

        //Send는 Recv와 구현 방법이 다름. Send는 Queue를 사용.
        Queue<ArraySegment<byte>> _sendQueue = new Queue<ArraySegment<byte> >();
        object _lock = new object();
        List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>();
        SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();
        SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();

        //결국 서버 컨텐츠에서 인터페이스화된 기능들을 사용할것이기 때문에 상속을 위해 추상화작업진행.
        public abstract void OnConnected(EndPoint endPoint);
        public abstract int OnRecv(ArraySegment<byte> buffer);
        public abstract void OnSend(int numOfbytes);
        public abstract void OnDisconnected(EndPoint endPoint);

        public void Start(Socket socket)
        {
            _socket = socket;
            _recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);
            _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

            //받기 예약
            RegisterRecv();            
        }

        public void Send(ArraySegment<byte> sendBuff)
        {
            lock (_lock)
            {
                _sendQueue.Enqueue(sendBuff);
                if (_pendingList.Count == 0)
                {
                    RegisterSend();
                }
            }
            
        }

        public void Disconnect()
        {
            if (Interlocked.Exchange(ref _disconnected, 1) == 1)
                return;

            OnDisconnected(_socket.RemoteEndPoint);
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }
        //Recv와 Send 둘 역시 비동기로 처리해야하기 때문에, 관련 함수를 두단계로 나누어 처리.
        
        void RegisterSend()
        {
            while (_sendQueue.Count > 0) 
            {
                ArraySegment<byte> buff = _sendQueue.Dequeue();
                _pendingList.Add(buff);
            }

            _sendArgs.BufferList = _pendingList;

            bool pending = _socket.SendAsync(_sendArgs);
            if (pending == false)
            {
                OnSendCompleted(null, _sendArgs);
            }
        }

        void OnSendCompleted(object sender, SocketAsyncEventArgs args)
        {
            //콜백으로 인해 실행 될 수 있기 때문에 lock처리.
            lock (_lock)
            {
                // TODO
                if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
                {
                    // TODO
                    try
                    {
                        _sendArgs.BufferList = null; //사실 null을 안해도 되긴함. 여기까지 왔다면 비어있을거임 BufferList는
                        _pendingList.Clear();

                        OnSend(_sendArgs.BytesTransferred);
                       
                        if (_sendQueue.Count > 0)
                        {
                            RegisterSend();
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"OnSendCompleted Failed {e}");
                    }
                }
                else
                {
                    Disconnect();
                }
            }
        }

        void RegisterRecv()
        {
            _recvBuffer.Clean();
            ArraySegment<byte> segment = _recvBuffer.WriteSegment;
            _recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count); 

            //이전의 AcceptAsync와 비슷한 맥락. 클라의 connect가 서버의 accept로 연결된 것처럼 클라의 send가 recv로 연결된다고 생각하자
            bool pending = _socket.ReceiveAsync(_recvArgs);
            if (pending == false)
            {
                OnRecvCompleted(null, _recvArgs);
            }
        }

        void OnRecvCompleted(object sender, SocketAsyncEventArgs args)
        {
            //Recv를 성공해서 이 코드로 넘어온것이고, 클라에서 send로 보낸 데이터는 args가 갖고있겠지.
            if(args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                try
                {
                    //Write커서이동
                    if (_recvBuffer.OnWrite(args.BytesTransferred) == false)
                    {
                        Disconnect();
                        return;
                    }

                    //컨텐츠 쪽으로 데이터를 넘겨주고 얼마나 처리했는지 받는다
                    int processLen = OnRecv(_recvBuffer.ReadSegment);
                    if (processLen < 0 || _recvBuffer.DataSize < processLen)
                    {
                        Disconnect();
                        return;
                    }

                    //Read커서이동
                    if(_recvBuffer.OnRead(processLen) == false)
                    {
                        Disconnect();
                        return;
                    }

                    RegisterRecv();
                    //여기서 궁금한점. args로 다시 RegisterRecv할 때, args를 초기화 안시켜줘도 되나?
                }
                catch (Exception e)
                {
                    Console.WriteLine($"OnRecvCompleted Failed {e}");
                }
            }
            else
            {
                // Disconnect
                Disconnect();
            }
        }

    }
}
