using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using ServerCore;

namespace DummyClient
{
	class ServerSession : Session
    {
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"Onconnected : {endPoint}");

            C_PlayerInfoReq packet = new C_PlayerInfoReq() { playerId = 1001 , name ="ABCD" };
			var skill = new C_PlayerInfoReq.Skill() { id = 101, level = 1, duration = 3.0f };
			skill.attributes.Add(new C_PlayerInfoReq.Skill.Attribute() { att = 77 });
			packet.skills.Add(skill);

            packet.skills.Add(new C_PlayerInfoReq.Skill() { id = 101, level = 1, duration = 3.0f });
            packet.skills.Add(new C_PlayerInfoReq.Skill() { id = 201, level = 2, duration = 4.0f });
            packet.skills.Add(new C_PlayerInfoReq.Skill() { id = 301, level = 3, duration = 5.0f });
            packet.skills.Add(new C_PlayerInfoReq.Skill() { id = 401, level = 4, duration = 6.0f });

            //for (int i = 1; i <= 5; i++)
            {
                ArraySegment<byte> s = packet.Write();

                if (s != null)
                    Send(s); 

                // 위 작업을 세션내부에서 sendbuff를 만들어두고 Send로 내용을 보내서
                // 내부의 Send함수에서 처리하는건 안될까 ? -> 되기는하는데, 성능적 이슈가 발생함
                // 계속 복사를해야해서!
            }

        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnDisconnected : {endPoint}");
        }

        public override int OnRecv(ArraySegment<byte> buffer)
        {
            string recvData = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
            Console.WriteLine($"[From Server : {recvData}");
            return buffer.Count;
        }

        public override void OnSend(int numOfbytes)
        {
            Console.WriteLine($"Transferredbytes : {numOfbytes}");
        }

    }
}
