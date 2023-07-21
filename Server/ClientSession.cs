using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using ServerCore;
using System.Net;


namespace Server
{
   
    class ClientSession : PacketSession
    {
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"Onconnected : {endPoint}");


            //Packet packet = new Packet() { size = 100, packetId = 10 };  
            //
            ////고작 여기서는 8바이트 데이터를 보내는데, 우리가 데이터를 얼마나 보낼지 처음엔 모르기때문에
            ////크게 잡는것이 메모리의 낭비가된다. -> 그래서 처음에 버퍼를 크게 잡고 짤라서 쓰는 방법을 채택.
            //
            //ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);
            //
            //byte[] buffer = BitConverter.GetBytes(packet.size);
            //byte[] buffer2 = BitConverter.GetBytes(packet.packetId);
            //Array.Copy(buffer, 0, openSegment.Array, openSegment.Offset, buffer.Length);
            //Array.Copy(buffer2, 0, openSegment.Array, openSegment.Offset + buffer.Length, buffer2.Length);
            //// 위 작업을 세션내부에서 sendbuff를 만들어두고 Send로 내용을 보내서
            //// 내부의 Send함수에서 처리하는건 안될까 ? -> 되기는하는데, 성능적 이슈가 발생함
            //// 계속 복사를해야해서!
            //ArraySegment<byte> sendBuff = SendBufferHelper.Close(buffer.Length + buffer2.Length);

            //Send(sendBuff);
            Thread.Sleep(5000);
            Disconnect();
        }

        public override void OnRecvPacket(ArraySegment<byte> buffer)
        {
			PacketManager.Instance.OnRecvPacket(this, buffer);

        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnDisconnected : {endPoint}");
        }



        public override void OnSend(int numOfbytes)
        {
            Console.WriteLine($"Transferredbytes : {numOfbytes}");
        }
    }

}
