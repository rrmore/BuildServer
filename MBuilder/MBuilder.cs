////////////////////////////////////////////////////////////////////////////////////////
// MotherBuilder.cs : This package spawns the child builders.                         //
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
* This package spawns the number of child builder processes.
* It maintains two blocking queues for build requests and ready messages.
*
* public Interfaces:
* =================
* processMessage(): Invoked if the message type is spawn process to help in spawning of child builders.
* receiverThreadProc(): Continuously monitors the receive queue and assigns tasks to other components.
* checkQueues(): Monitors request queue and ready queue and sends request to child builder if both are not empty.
* creatProcess(): spawns number of child processes.
*
* Required Files:
* ===============
* MPCommService.cs,IService.cs,BlockingQueue.cs
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
using Project4Comm;
using System.Threading;
using System.Diagnostics;
using System.IO;
using SWTools;

namespace Project4

{
    class MotherBuilder
    {
        static int numberOfProcesses = 0;
        Comm comm { get; set; } = null;
        Thread receiverThread = null;
        static BlockingQueue<CommMessage> readyQ;
        static BlockingQueue<CommMessage> requestQ;

        public MotherBuilder()
        {
            Console.Write("----------------------Mother builder is started------------------------------------------");
            comm = new Comm("http://localhost", 9080);
            Console.Write("\n Sender and receiver started for mother build with address http://localhost:9080/BasicService");
            receiverThread = new Thread(receiverThreadProc);
            receiverThread.Start();

        }


        //------< helps in spawning number of child builders >----------
        
        public void processMessage(CommMessage msg)
        {
            creatProcess(msg.numberProcess);
            return;
        }


        /*------< Monitors the receive queue of mother builder >----------------*/

        void receiverThreadProc()
        {
            Console.Write("\n Mother builder's receiver thread is started");
            Console.Write("\n Process pooling is started is started");
            while (true)
            {
                if (checkQueues()) {
                 }
                CommMessage receiveMessage = comm.getMessage();
                Console.Write("\n Message received by mother builder");
                Console.Write("\n Message details=");
                receiveMessage.show();
                switch(receiveMessage.type)
                {
                    case CommMessage.MessageType.Ready:
                        readyQ.enQ(receiveMessage);
                        Console.Write("\n Added the message to the ready queue");
                        break;
                    case CommMessage.MessageType.SpawnProcess:
                        processMessage(receiveMessage);
                        break;
                   case CommMessage.MessageType.BuildRequest:
                        requestQ.enQ(receiveMessage);
                        Console.Write("\n Added the message to the request queue");
                        break;
                }
            Console.WriteLine("\n Size of ready queue="+readyQ.size());
            Console.WriteLine(" Size of request queue"+requestQ.size()+"\n");
            if (receiveMessage.body == "quit")
            {
                Console.WriteLine("shutting down the mother builder");
                break;
            }
        }
    }

        //------< spawns number of child builders >----------

        void creatProcess(int numProcess)
        {
            for (int i = 0; i < numProcess; i++)
            {
                if (numberOfProcesses < 10)
                {
                    Process p2 = new Process();
                    string fileName = "../../../ChildBuildServer/bin/debug/ChildBuildServer.exe";
                    Process.Start(Path.GetFullPath(fileName), "908" + ((numberOfProcesses + 1).ToString()));
                    numberOfProcesses++;
                }
            }
            Console.Write("\n Created"+numProcess+" processes");
            return;
        }

            //------< Check if any messages dropped into the ready queue and request queue >----------

            public bool checkQueues()
        {
            if (readyQ.size() > 0 && requestQ.size() > 0)
            {
                CommMessage mready = readyQ.deQ();
                Console.Write("\n Dequeuing ready message from ready queue");
                CommMessage mrequest = requestQ.deQ();
                Console.Write("\n Dequeuing request message from request queue");
                mrequest.to = mready.from;
                Console.Write("\n Details about ready message");
                mready.show();
                Console.Write("\n Details about request message");
                mready.show();
                comm.postMessage(mrequest);
                Console.Write("\n Successfully sent build request to the child builder address: "+mrequest.to+"\n");
                return true;
            }
            return false;
        }


        
#if(MOTHERBUILDER)
        static void Main(string[] args)
        {
            MotherBuilder rs = new MotherBuilder();
            readyQ = new BlockingQueue<CommMessage>();
            requestQ = new BlockingQueue<CommMessage>();
            CommMessage msg = new CommMessage(CommMessage.MessageType.connect);
            msg.to = "http://localhost:9080/BasicService";
            msg.from = "http://localhost:9080/BasicService";
            rs.comm.postMessage(msg);

            CommMessage msg3 = msg.Clone();
            msg3.type = CommMessage.MessageType.SpawnProcess;
            msg3.numberProcess = 2;
            rs.comm.postMessage(msg3);
        }
#endif
    }
}
