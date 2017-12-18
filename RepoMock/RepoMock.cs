////////////////////////////////////////////////////////////////////////////////////////
// Repository.cs : This package is used for storing files.                            //
// ver 1.0                                                                            //
//                                                                                    //
//Language:     Visual C#                                                             //
// Platform    : Acer Aspire R, Win Pro 10, Visual Studio 2017                        //
// Application : CSE-681 SMA Project 4                                                //
// Author      : Rohit More,Syracuse University                                       //
// Source      : Dr. Jim Fawcett, EECS, SU                                            //
////////////////////////////////////////////////////////////////////////////////////////

/*
* Module Operations:
*===================
*This module stores sends the build request to mother builder.
* It also stores build logs,test logs and build request
*
* public Interfaces:
* =================
* receiverThreadProc(): Continuously monitors the receive queue and processes the message.
* addressSwap(): Swaps the to and from addresses of the servers.
*
* Required Files:
* ===============
* MPCommService.cs,IService.cs
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
using System.IO;
using System.Xml.Linq;

namespace Project4
{
    class RepoMock
    {
        Comm comm { get; set; } = null;
        string repoStorage = @"../../../RepoStorage";
        Thread receiverThread = null;

        public RepoMock()
        {
            Console.Write("\n------------------------Repository is started---------------------------\n");
            comm = new Comm("http://localhost", 9070);
            string[] buildRequests = Directory.GetFiles(Path.Combine(Path.GetFullPath(repoStorage), "BuildRequests"), "*BuildRequest*.xml");
            CommMessage commMessage = new CommMessage(CommMessage.MessageType.BuildRequest);
            commMessage.from = "http://localhost:9070/BasicService";
            commMessage.to = "http://localhost:9080/BasicService";
            foreach (string buildRequest in buildRequests)
            {
                CommMessage commMsg = commMessage.Clone();
                commMsg.body = File.ReadAllText(buildRequest);
                commMsg.fileName = Path.GetFileName(buildRequest);
                comm.postMessage(commMsg);
            }
            receiverThread = new Thread(receiverThreadProc);
            receiverThread.Start();
        }


        //------------------------thread to check messages in the receiver block-------------------------------------------------/
        void receiverThreadProc()
        {
            Console.Write("\n  Repository's receive thread is started");
            //-------------continuously check for messages-------------------------------------------------
            while (true)
            {
                CommMessage receiverMsg = comm.getMessage();
                switch (receiverMsg.type)
                {
                    case CommMessage.MessageType.FileTransferRequest:
                        fileTransferMessageOperation(receiverMsg);
                        break;
                    case CommMessage.MessageType.Drivers:
                       driverMessageOperations(receiverMsg);
                        break;
                    case CommMessage.MessageType.BuildLog:
                        buildLogMessage(receiverMsg);
                        break;
                    case CommMessage.MessageType.TestLog:
                        string testLog = Path.Combine(Path.GetFullPath(repoStorage), "TestLog") + "/TestLog" + DateTime.Now.ToFileTime() + ".xml";
                        Console.Write("\n Received message to store test log from test harness with address: " + receiverMsg.from);
                        Console.Write("\n Message details= ");
                        receiverMsg.show();
                        XDocument doc2 = XDocument.Parse(receiverMsg.body);
                        Console.Write("\n Stored the test log at the following location : " + testLog + "\n");
                        doc2.Save(testLog);
                        break;
                    case CommMessage.MessageType.BuildRequests:
                        buildRequestMessageOperation(receiverMsg);
                        break;
                    case CommMessage.MessageType.SaveBuildRequest:
                        saveRequesMessageOperation(receiverMsg);
                        break;
                    case CommMessage.MessageType.BuildLogs:
                        buildLogsMessageOperation(receiverMsg);
                        break;
                    case CommMessage.MessageType.TestCodes:
                        testCodeMessageOperation(receiverMsg);
                        break;
                    case CommMessage.MessageType.TestLogs:
                        testLogsMessageOperation(receiverMsg);
                        break;
                }
            }
        }

        public void buildLogsMessageOperation(CommMessage receiverMsg)
        {
            Console.Write("\n Received message for build requests from client with address: " + receiverMsg.from);
            Console.Write("\n Message details= ");
            receiverMsg.show();
            string[] buildLogs = Directory.GetFiles(repoStorage + "/BuildLog", "*.xml");
            receiverMsg = addressSwap(receiverMsg);
            foreach (string fileName in buildLogs)
            {
                receiverMsg.fileNames.Add(fileName);
            }
            Console.Write("\n Sent build logs to client \n");
            comm.postMessage(receiverMsg);
        }

        //--------------executes when  file tranfer message is executed-----------------------------------------------
        public void fileTransferMessageOperation(CommMessage receiverMsg)
        {
            Console.Write("\n Received file transfer request from child builder with address: " + receiverMsg.from);
            Console.Write("\n Message details= ");
            receiverMsg.show();
            receiverMsg = addressSwap(receiverMsg);
            bool output1 = comm.postFile(receiverMsg);
            if (output1)
            {
                Console.WriteLine("\nBuild Files Transfered successfully\n");
            }
        }

        public void driverMessageOperations(CommMessage receiverMsg)
        {
            Console.Write("\n Received driver request from client with address: " + receiverMsg.from);
            Console.Write("\n Message details= ");
            receiverMsg.show();
            string[] drivers = Directory.GetFiles(repoStorage, "*d*.cs");
            receiverMsg = addressSwap(receiverMsg);
            foreach (string fileName in drivers)
            {
                receiverMsg.fileNames.Add(fileName);
            }
            comm.postMessage(receiverMsg);
            Console.Write("\n Successfully sent  drivers to the client\n");
        }

        public void buildLogMessage(CommMessage receiverMsg)
        {
            string buildLog = Path.Combine(Path.GetFullPath(repoStorage), "BuildLog") + "/BuildLog" + DateTime.Now.ToFileTime() + ".xml";
            Console.Write("\n Received message to store build log from child address: " + receiverMsg.from);
            Console.Write("\n Message details= ");
            receiverMsg.show();
            XDocument doc = XDocument.Parse(receiverMsg.body);
            Console.Write("\n Stored the build log at the following location: " + buildLog + "\n");
            doc.Save(buildLog);
        }

        //--------------executed when test log message is received-------------------------------------------------
        public void testLogsMessageOperation(CommMessage receiverMsg)
        {
            Console.Write("\n Received message for test logs from client with address: " + receiverMsg.from);
            Console.Write("\n Message details= ");
            receiverMsg.show();
            string[] testLogs = Directory.GetFiles(repoStorage + "/TestLog", "*.xml");
            receiverMsg = addressSwap(receiverMsg);
            foreach (string fileName in testLogs)
            {
                receiverMsg.fileNames.Add(fileName);
            }
            Console.Write("\n Sent test logs to the client \n");
            comm.postMessage(receiverMsg);
        }

        //------------executed when save request message is received------------------------------------------------
        public void saveRequesMessageOperation(CommMessage receiverMsg)
        {
            Console.Write("\n Message to save build request received from the client address: " + receiverMsg.from);
            Console.Write("\n Message details= ");
            receiverMsg.show();
            string buildRequest = Path.Combine(Path.GetFullPath(repoStorage), "BuildRequests") + "/BuildRequest" + DateTime.Now.ToFileTime() + ".xml";
            XDocument doc1 = XDocument.Parse(receiverMsg.body);
            Console.Write("\n Saved build request at the following location: " + buildRequest + "\n");
            doc1.Save(buildRequest);
        }

        //---------------------executed when test code message is received-------------------------------------------
        public void testCodeMessageOperation(CommMessage receiverMsg)
        {
            Console.Write("\n Client requested for test codes");
            Console.Write("\n Message details: ");
            receiverMsg.show();
            string[] testCodes = Directory.GetFiles(repoStorage, "*c*.cs");
            receiverMsg = addressSwap(receiverMsg);
            foreach (string fileName in testCodes)
            {
                receiverMsg.fileNames.Add(fileName);
            }
            Console.Write("\n Sent test codes to client\n");
            comm.postMessage(receiverMsg);
        }

        //---------------helper function to build request---------------------------------------------------------
        public void buildRequestMessageOperation(CommMessage receiverMsg)
        {
            Console.Write("\n Received message for build requests from client with address: " + receiverMsg.from);
            Console.Write("\n Message details= ");
            receiverMsg.show();
            string[] buildRequests = Directory.GetFiles(repoStorage + "/BuildRequests", "*.xml");
            receiverMsg = addressSwap(receiverMsg);
            foreach (string fileName in buildRequests)
            {
                receiverMsg.fileNames.Add(fileName);
            }
            Console.Write("\n Sent build request to the client \n");
            comm.postMessage(receiverMsg);
        }


        //----------Swaps to and from address of the message-------------------------------------------------------------------------------
        CommMessage addressSwap(CommMessage msg)
        {
            string temp = msg.to;
            msg.to = msg.from;
            msg.from = temp;
            return msg;
        }
#if(REPOSITORY)
        static void Main(string[] args)
        {
            RepoMock repo = new RepoMock();
            CommMessage commMessage = new CommMessage(CommMessage.MessageType.connect);
            commMessage.from = "http://localhost:9070/BasicService";
            commMessage.to = "http://localhost:9070/BasicService";
            repo.comm.postMessage(commMessage);
        }
#endif
    }
}
