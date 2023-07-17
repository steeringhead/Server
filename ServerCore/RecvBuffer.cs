using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    public class RecvBuffer
    {
        ArraySegment<byte> _buffer;
        int _readPos;
        int _writePos;

        public RecvBuffer(int bufferSize)
        {
            //ArraySegment는 배열의 특정 범위를 참조해주는 구조체다. byte가 들어있는 배열을 참조하겠단 뜻
            _buffer = new ArraySegment<byte>(new byte[bufferSize], 0, bufferSize);

        }

        public int DataSize { get { return _writePos - _readPos; } }    

        public int FreeSize { get { return _buffer.Count - _writePos; } }

        public ArraySegment<byte> ReadSegment
        {
            get { return new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _readPos, DataSize);  }
        }

        public ArraySegment<byte> WriteSegment
        {
            get { return new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _writePos, FreeSize); }
        }

        public void Clean() //버퍼를 한번씩 다시 앞으로 땡겨줘야함. 계속 끝까지 가면 범위를 넘어서서 사용할수가 없으니까 !
        {
            int dataSize = DataSize;
            if(dataSize == 0)
            {
                //남은 데이터가 없으니까 복사 필요없이 커서위치만 옮기면됨
                _readPos = _writePos = 0;
            }
            else
            {
                //데이터가 남아있으면 시작위치로 복사해야함
                Array.Copy(_buffer.Array, _buffer.Offset + _readPos, _buffer.Array, _buffer.Offset, dataSize);
                _readPos = 0;
                _writePos = dataSize;
            }
        }

        public bool OnRead(int numOfBytes)
        {
            if (numOfBytes > DataSize)
                return false;
            _readPos += numOfBytes;
            return true;
        }

        public bool OnWrite(int numOfBytes)
        {
            if (numOfBytes > FreeSize)
                return false;
            _writePos += numOfBytes;
            return true;
        }
    }
}
