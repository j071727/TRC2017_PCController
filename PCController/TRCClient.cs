using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PCController
{
    class TRCClient
    {
        public const string DEFAULT_IP = "127.0.0.1";
        public const int DEFAULT_PORT = 5001;

        private static string address;
        private static int port;

        private static TcpClient tcpClient = null;
        private static StreamReader tcpReader = null;
        private static StreamWriter tcpWriter = null;

        public static int src, dst;
      //  public static int[] step = new int[3];
      //  public static int[] time = new int[3];
        public static string cassNo = "";
       // public static int[,] record_stage = new int[6,3];
        public static int[,] record_stages = new int[6,6];
        public static int[,] record_times = new int [6,6];
       // public static int[,] record_time = new int[6,3];
        public static int[] record_wafer = new int[6];
        public static int[] record_waferb = new int[6];
        public static int now = 0;
        
        public static bool isConnected()
        {
            if (tcpClient == null)
                return false;

            return tcpClient.Connected;
        }

        public static void connect(string ip_in,int port_in)
        {
            address = ip_in;
            port = port_in;

            disconnect();

            try
            {
                tcpClient = new TcpClient(address, port);
                NetworkStream stream = tcpClient.GetStream();
                tcpReader = new StreamReader(stream);
                tcpWriter = new StreamWriter(stream);

            }
            catch
            {
                mesPrintln("TRCClient: Cannnot connect to TRC server.");
            }


        }

        public static void disconnect()
        {

            if (tcpReader != null)
            {
                tcpReader.Close();
                tcpReader= null;
            }

            if (tcpWriter != null)
            {
                tcpWriter.Close();
                tcpWriter= null;
            }

            if (tcpClient != null)
            {
                tcpClient.Close();
                tcpClient = null;
            }

        }


        private static string getCmd()
        {
            if(!isConnected())
            {
                connectionErrorHandler();
                return null;
            }

            char[] buf = new char[100];
            int count = 0;
            
            try
            {
                count = tcpReader.Read(buf, 0, 100);
            }
            catch
            {
            }

            return new string(buf, 0, count);

        }

        private static int sentCmd(string str)
        {
            if(!isConnected())
            {
                connectionErrorHandler();
                return -1;
            }
            tcpWriter.WriteLine(str);
            tcpWriter.Flush();
            return 0;
        }

        public static void handShake()
        {
            string command, answer = "~Ack";
            string[] para, para2,para3,para4,para5;
    

            // first handShake
            command = getCmd();
            mesPrintln(command);
            para = command.Split(',');
            para2 = para[3].Split(';');
            para3 = para2[1].Split('|'); //
            src = (para2[0].ElementAt(6) - '0') * 10
                        + (para2[0].ElementAt(7) - '0');
            record_wafer[now] = src;
            
            cassNo += (src / 10);
            cassNo += (src % 10);
            if (para2.Length == 5)
            {
                para4 = para2[2].Split('|');
                para5 = para2[3].Split('|');
                
                // initial src, dst, cass number, & steps
                /* src = (para2[0].ElementAt(6) - '0') * 10
                         + (para2[0].ElementAt(7) - '0');
                 record_wafer[now] = src;*/
                 dst = (para2[4].ElementAt(6) - '0') * 10
                         + (para2[4].ElementAt(7) - '0');
                 record_waferb[now] = dst;
                 /*cassNo += (src / 10);
                 cassNo += (src % 10);*/

                int time_tmp = para3[(para3.Length) - 1].ElementAt(2) - '0';
                if (para3[(para3.Length) - 1].Length == 4)
                {
                    time_tmp *= 10;
                    time_tmp += para3[(para3.Length) - 1].ElementAt(3) - '0';
                }
                for (int i = 0; i < para3.Length; i++)
                {
                    int tmp = para3[i].ElementAt(0) - 'A';
                    record_stages[now, tmp] = 1;
                    record_times[now, tmp] = time_tmp;
                }

                time_tmp = para4[(para4.Length) - 1].ElementAt(2) - '0';
                if (para4[(para4.Length) - 1].Length == 4)
                {
                    time_tmp *= 10;
                    time_tmp += para4[(para4.Length) - 1].ElementAt(3) - '0';
                }
                for (int i = 0; i < para4.Length; i++)
                {
                    int tmp = para4[i].ElementAt(0) - 'A';
                    record_stages[now, tmp] = 2;
                    record_times[now, tmp] = time_tmp;
                }

                time_tmp = para5[(para5.Length) - 1].ElementAt(2) - '0';
                if (para5[(para5.Length) - 1].Length == 4)
                {
                    time_tmp *= 10;
                    time_tmp += para5[(para5.Length) - 1].ElementAt(3) - '0';
                }
                for (int i = 0; i < para5.Length; i++)
                {
                    int tmp = para5[i].ElementAt(0) - 'A';
                    record_stages[now, tmp] = 3;
                    record_times[now, tmp] = time_tmp;
                }
                /* for(int i = 0;i<6;i++)
                   {
                       mesPrint(record_times[0, i].ToString() + " ");
                   }*/
                now++;
            }
            else
            {
                dst = (para2[7].ElementAt(6) - '0') * 10
                         + (para2[7].ElementAt(7) - '0');
                record_waferb[now] = dst;

                int time_tmp = para2[1].ElementAt(2) - '0';
                if (para2[1].Length == 4)
                {
                    time_tmp *= 10;
                    time_tmp += para2[1].ElementAt(3) - '0';
                }
                for (int i = 0; i < 6; i++)
                {
                    record_stages[now, i] = 1;
                    record_times[now, i] = time_tmp;
                }
                /*for(int i = 0;i<6;i++)
                  {
                      mesPrint(record_times[0, i].ToString() + " ");
                  }*/
                now++;
            }
            for (int i=1; i<para.Length; i++)
                answer += "," + para[i];

            mesPrintln(answer);
            Thread.Sleep(1000);
            sentCmd(answer);

            // second handShake
            command = getCmd();
            mesPrintln(command);
            para = command.Split(',');
            answer = "~Ack";
            for (int i = 1; i < para.Length; i++)
                answer += "," + para[i];
            mesPrintln(answer);
            Thread.Sleep(1000);
            sentCmd(answer);
        }

        public static int sentEvent(int opCode, int location, int waferNum, int cassNum)
        {
            int accept = 0;
            string[] op = { "GetWaferStart", "GetWaferCompleted",
                            "PutWaferStart", "PutWaferCompleted" };
            string evt = "~Evt,";
            string wafer = "W" ;
            string[] loc = {"CassA", "A", "B", "C", "D", "E", "F", "CassB" };
            string target;
            wafer += waferNum / 10;
            wafer += waferNum % 10;

            target = loc[location];
            if (location == 0)      target += "-" + cassNum / 10 + cassNum % 10;
            else if (location == 7) target += "-" + cassNum / 10 + cassNum % 10;
            evt = evt + op[opCode] + "," + wafer + "," + target + "@";
            sentCmd(evt);
            accept = getResponse();
            return accept;
        }

        public static int getResponse()
        {
            string command;
            string[] para;

            command = getCmd();
            mesPrintln(command);
            para = command.Split(',');
            string reject = para.ElementAt(para.Length-1);
            if (para.Length == 4) mesPrintln("Completed");
            else if (reject.Equals("0@"))
            {
                mesPrintln("Accept");
                return 1;
            }
            else mesPrintln("Reject");
            return 0;
            // Thread.Sleep(1000);
        }

        public static void init()
        {
            if (!isConnected())
            {
                connectionErrorHandler();
                return;
            }
            handShake();
            handShake();
            handShake();
            handShake();
            handShake();
            handShake();

            /* 0 : GET_START
             * 1 : GET_COMP
             * 2 : PUT_START
             * 3 : PUT_COMP */

            // sentEvent(OPCODE, STAGE);   
            // getStageTime(STAGE,WAFER_NUM);  
        }


        /*public int getStageTime(int stage,int wafer)
        {
            int i;
            for (i = 0; i < 6; i++)
                if(record_wafer[i] == wafer)    break;
            return record_time[i,stage];
        }*/

        public void finish()
        {
            tcpWriter.Close();
            tcpReader.Close();
        }

        private static void connectionErrorHandler()
        {
            Program.form.showWarnning("TRCClient: TRC server is not connected");
        }

        private static void mesPrint(string str)
        {
            if (Program.form != null)
                Program.form.mesPrint(str);
        }
        private static void mesPrintln(string str)
        {
            if (Program.form != null)
                Program.form.mesPrintln(str);
        }

    }
}
