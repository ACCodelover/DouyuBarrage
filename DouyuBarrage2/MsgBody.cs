using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DouyuBarrage2
{
    class MsgBody
    {
        private string msg;//这里是否需要初始化,否则后面误调用toByte方法的时候容易报错
        private static int msgType = 689;//客户端消息标识
        private int msgLength1;
        //private int msgLength2;
        private byte[] byteMsgLength;
        private byte[] byteMsg;
        private byte[] byteMsgType;

        public MsgBody(string msg)
        {
            this.msg = msg;
        }
        //是否保留该构造方法?
        public MsgBody()
        {



        }

        public byte[] toByte()
        {
            //消息类型转byte
            byteMsgType = BitConverter.GetBytes(msgType);
            //经过验证后 689转成byte数字后为177 22 0 0 符合斗鱼弹幕协议规则
            /*
            Console.WriteLine(byteMsgLength.Length);
            foreach (byte b in byteMsgLength)
            {
                Console.Write(b + " ");
            }*/

            byteMsg = Encoding.UTF8.GetBytes(msg);
            //msgLength1 += byteMsg.Length;
            msgLength1 = 4 + 1 + byteMsg.Length + byteMsgType.Length;
            byteMsgLength = BitConverter.GetBytes(msgLength1);
            byte[] result = new byte[msgLength1 + 4];
            Array.Copy(byteMsgLength, 0, result, 0, 4);
            Array.Copy(byteMsgLength, 0, result, 4, 4);
            Array.Copy(byteMsgType, 0, result, 8, 4);
            Array.Copy(byteMsg, 0, result, 12, byteMsg.Length);

            return result;
        }


    }
}