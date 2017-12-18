////////////////////////////////////////////////////////////////////////////////////////
// IService.cs : This package is interface for Communication.                         //
// ver 1.0                                                                            //
//                                                                                    //
//Language:     Visual C#                                                             //
// Platform    : Lenovo 510S Ideapad, Win Pro 10, Visual Studio 2017                  //
// Application : CSE-681 SMA Project 4                                                //
// Author      : Rohit More,Syracuse University                        //
// Source      : Dr. Jim Fawcett, EECS, SU                                            //
////////////////////////////////////////////////////////////////////////////////////////

/*
* Module Operations:
*===================
* This package is the interface for the client. 
*
* public Interfaces:
* =================
* ICommunication : Interface used for message passing and fileName transfer.
*
* Required Files:
* ===============
* MPCommService.cs
*
* Maintainance History:
* =====================
* ver 1.0
*
*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.Runtime.Serialization;

namespace Project4Comm
{
    [ServiceContract(Namespace = "Project4Comm")]
    public interface IService
    {
        /*----< support for message passing >--------------------------*/

        [OperationContract(IsOneWay = true)]
        void postMessage(CommMessage msg);

        // private to receiver so not an OperationContract
        CommMessage getMessage();

        /*----< support for sending fileName in blocks >-------------------*/

        [OperationContract]
        bool openFileForWrite(string name,string receivePath);
        [OperationContract]
        bool writeFileBlock(byte[] block);
        [OperationContract]
        void closeFile();
    }
    [DataContract]
    public class CommMessage
    {
        public enum MessageType
        {
            [EnumMember]    //To connect the sender and receiver initially
            connect,
            [EnumMember]    //to send build request
            BuildRequest,
            [EnumMember]    //to send test request
            TestRequest,
            [EnumMember]    //to giveready status to mother builder
            Ready,
            [EnumMember]    //Builders sends to intitate connection to repo for writing logs
            BuildLog,
            [EnumMember]    
            TestLog,
            [EnumMember]    
            Request,
            [EnumMember]    
            FileTransferRequest,
            [EnumMember]        
            BuildDLL,
            [EnumMember]       
            Drivers,
            [EnumMember]      
            TestCodes,
            [EnumMember]      
            SaveBuildRequest,
            [EnumMember]     
            BuildRequests,
            [EnumMember]       
            BuildLogs,
            [EnumMember]       //child builder asks test harness to execute after sending libraries
            TestExecute,
            [EnumMember]        //client requests repo for test logs
            TestLogs,
            [EnumMember]
            closeReceiver,
            [EnumMember]
            closeSender,
            [EnumMember]         //command to create specified number of processes
            SpawnProcess,
            [EnumMember]
            quit
        }
        public CommMessage(MessageType mt)
        {
            type = mt;
        }
        /*----< data members - all serializable public properties >----*/
        [DataMember]
        public string to { get; set; }

        [DataMember]
        public string from { get; set; }

        [DataMember]
        public MessageType type { get; set; } = MessageType.connect;

        [DataMember]
        public string fileName { get; set; }

        [DataMember]
        public string filePath { get; set; }

        [DataMember]
        public string command { get; set; }

        [DataMember]
        public string dllPath { get; set; }

        [DataMember]
        public string body { get; set; }

        [DataMember]
        public List<string> fileNames { get; set; } = new List<string>();

        [DataMember]
        public int numberProcess { get; set; }

        [DataMember]
        public string errorMsg { get; set; } = "no error";
        //-< Displays the message transferred between the sender(client/proxy) and reciever(host) >-----------
        public void show()
        {
            Console.Write("\n  MessageRecieved:");
            Console.Write("\n    MessageType : {0}", type.ToString());
            if (to != null)
                Console.Write("\n    to          : {0}", to);
            if (from != null)
                Console.Write("\n    from        : {0}", from);
            if (fileName != null)
                Console.Write("\n    fileName   :  {0}", fileName);
            if (fileNames != null)
            {
                Console.Write("\n    List of Files   :");


                if (fileNames.Count > 0)
                    Console.Write("\n      ");
                foreach (string arg in fileNames)
                    Console.Write("{0} ", arg);
            }
            Console.Write("\n    errorMsg    : {0}", errorMsg);

        }

        /*----< clones the attributes of a message >--------------------------*/
        public CommMessage Clone()
        {
            CommMessage msg = new CommMessage(CommMessage.MessageType.connect);
            msg.type = type;
            msg.to = to;
            msg.from = from;
            msg.fileName = fileName;
            msg.command = command;
            return msg;
        }

        public static void Main()
        {
            Console.WriteLine("Hello World");
        }
    }

}
