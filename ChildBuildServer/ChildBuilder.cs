////////////////////////////////////////////////////////////////////////////////////////
// ChildBuilder.cs : This package is the child build server controller.               //
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
* 
*
* public Interfaces:
* =================
* performBuild(): Builds libraries based on the build request.
* parseBuildRequest(): Parses the build request received
* buildLogs(): Generates build logs for the specific test
* addressSwap(): Swaps the to and from addresses of the servers.
* createTestRequest(): generates a test request from the build request
* receiverThreadProc(): checks for messages in the receiver block and processes the message if present
* 
* 
*
* Required Files:
* ===============
* MPCommService.cs,IService.cs,XMLManager.cs,serialization.cs
*
* Maintainance History:
* =====================
* ver 1.0
*
*/


using Project4Comm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Project4
{
    class ChildBuilder
    {
        Comm comm { get; set; } = null;
        Thread receiverThread = null;
        public string dllPath { get; set; } = "";
        Builder builder = new Builder();
        public string name { get; set; }
        List<LogManager> logData = new List<LogManager>();
        List<String> buildFilesList = new List<string>();
        public CommMessage readyAgain { get; set; }
        public string receivePath { get; set; } = "";

        public ChildBuilder(int port)
        {
            Console.Write("\n----------------------Child builder is started------------------------------------------");
            comm = new Comm("http://localhost", port);
            Console.Write("\n Sender and receiver started for mother build with address http://localhost:"+port+"/BasicService");

        }


        //------------parses the build request and extract the required file names--------------------------------------------------
        public List<String> parseBuildRequest(String buildRequest)
        {
            Console.Write("\n Child Build parses the Build Request");
            Console.Write("\n Build Request=\n"+buildRequest);
            List<String> files = new List<string>();
            TestRequest testRequestNew = buildRequest.FromXml<TestRequest>();
             name = testRequestNew.author;
             receivePath = "../../../ChildBuildServer/" + name + "_" + DateTime.Now.ToString("yyyyMMddHHmmssffff");
            
             Directory.CreateDirectory(receivePath);
            List<TestElement> testElements = testRequestNew.tests;
            //--------------Iterating over each test element in the request------------------------------------------------------
            foreach (TestElement testElement in testElements)
            {
                files.Add(testElement.testDriver);
                List<String> codes = testElement.testCodes;
                foreach (String code in codes)
                {
                    files.Add(code);
                }
            }
            return files;
        }


        //-------------------starter funtion to perform builds----------------------------------------------------------

        public void performBuild(CommMessage message)
        {
            TestRequest testRequestNew = message.body.FromXml<TestRequest>();
            List<TestElement> testElements = testRequestNew.tests;

            foreach (TestElement testElement in testElements)
            {
                buildFilesList.Add(testElement.testDriver);
                List<String> codes = testElement.testCodes;
                foreach (String c in codes)
                {
                    buildFilesList.Add(c);
                }
                String status = builder.libraryBuilder(buildFilesList, receivePath);
                buildFilesList.Clear();
                buildLogs(testElement, status, name);
            }

            String logInString = LogManager.createLogs(logData, "BuildLog");
            String temp = message.to;
            message.to = message.from;
            message.from = temp;
            message.body = logInString;
            message.type = CommMessage.MessageType.BuildLog;
            comm.postMessage(message);
            String testRequest = createTestRqst(logData, receivePath, name);
            //------------------check if test reuqest is not null------------------------------------------------------
            if (!testRequest.Equals(""))
            {
                CommMessage msg1 = new CommMessage(CommMessage.MessageType.TestRequest);
                msg1.from = message.from;
                msg1.to = "http://localhost:9090/BasicService";
                msg1.body = testRequest;
                comm.postMessage(msg1);
            }
            else
            {
                Console.Write("\n Sent Ready Message to Mother builder \n");
                comm.postMessage(readyAgain);
            }
            logData.Clear();

        }


        //-------------------------generates build logs---------------------------------------------------------------------
        public void buildLogs(TestElement t, string status, string name)
        {
            LogManager logData = new LogManager();
            logData.author = name;
            logData.testname = t.testName;
            logData.testDriver = t.testDriver;
            if (status == "") logData.status = "Build Success";
            else { logData.status = "Build Failed"; logData.message = status; }
            this.logData.Add(logData);
        }


        //-------------------------creates a test request after building libraries--------------------------------------------------
        public string createTestRqst(List<LogManager> test, string path, string name)
        {
            List<TestElement> testElements = new List<TestElement>();
            foreach (LogManager testElement in test)
            {
                if (testElement.status.Equals("Build Success"))
                {
                    TestElement te1 = new TestElement();
                    te1.testName = testElement.testname;
                    te1.addDriver(Path.GetFileNameWithoutExtension(testElement.testDriver) + ".dll");
                    testElements.Add(te1);
                }
            }
            TestRequest testRequest = new TestRequest();
            testRequest.author = name;
            for (int i = 0; i < testElements.Count; i++)
                testRequest.tests.Add(testElements[i]);
            string trXml;
            //-------Check if the test request has any test elements---------------------------------------------------------------
            if (testRequest.tests.Count >= 1)
            {
                trXml = testRequest.ToXml();
                File.WriteAllText(path + "/TestRequesttoHarness.xml", trXml);
                Console.Write("\n Created Test request for test harness");
                Console.Write("\n Test Request= \n"+trXml);
                return trXml;
            }
            else
            {
                Console.Write("\n No test request created due to build failure");
                return "";
            }
        }


        //---------checks the receiver queue and performs operation based on the message---------------------------------------------------
        void receiverThreadProc()
        {
            Console.Write("\n Started child builders receiver thread");
            while (true)
            {
                CommMessage receiverMessage = comm.getMessage();
                switch (receiverMessage.type)
                {
                    //-------------------Check the message type and do processing based on that-------------------------------------------
                    case CommMessage.MessageType.BuildRequest:
                        buildRequestMessageOperations(receiverMessage);
                        break;
                    case CommMessage.MessageType.BuildDLL:
                        Console.Write("\n\n Received build dll message from repo with address: " + receiverMessage.from);
                        Console.Write("\n Message details: ");
                        receiverMessage.show();
                        performBuild(receiverMessage);
                        break;
                    case CommMessage.MessageType.TestRequest:
                        testRequestOperations(receiverMessage);
                        break;
                }
                if (receiverMessage.body == "quit")
                {
                    Console.WriteLine("shut down host by client request");
                    break;
                }
                Thread.Sleep(1000);
            }
        }

        //-------------------helper class to process build request message------------------------
        void buildRequestMessageOperations(CommMessage message)
        {
            Console.Write("\n\n Received  message from test harness with address: " + message.from);
            Console.Write("\n Message details: ");
            message.show();
            message.fileNames = parseBuildRequest(message.body);
            message.filePath = receivePath;
            message.fileName = null;
            message.body = message.body;
            message = addressSwap(message);
            message.to = "http://localhost:9070/BasicService";
            message.type = CommMessage.MessageType.FileTransferRequest;
            Console.Write("Sent file request mesage to repository with address " + message.to);
            comm.postMessage(message);
        }

        //----------------------helper class to process test request messages--------------------------
        void testRequestOperations(CommMessage messages)
        {
            Console.Write("\n\n Received test request message from test harness with address: " + messages.from);
            Console.Write("\n Message details: ");
            messages.show();
            messages = addressSwap(messages);
            messages.dllPath = receivePath;
            bool output1 = comm.postFile(messages);
            if (output1)
            {
                Console.WriteLine("\n DLL Files Transfered successfully to Test Harness");
            }
            Console.Write("\n Sending Ready CommMessage to Mother builder \n");
            comm.postMessage(readyAgain);
        }


        //------------------Swaps to and from adress of the message--------------------------------------------------------------------
        CommMessage addressSwap(CommMessage messages)
        {
            if (messages.from != null)
            {
                String temp = messages.to;
                messages.to = messages.from;
                messages.from = temp;
            }
            return messages;
        }

#if(CHILDBUILDSERVER)
        static void Main(string[] args)
        {
            ChildBuilder rs = new ChildBuilder(Int32.Parse(args[0]));
            CommMessage message1 = new CommMessage(CommMessage.MessageType.connect);
            message1.to = "http://localhost:"+args[0]+"/BasicService";
            message1.from = "http://localhost:"+args[0]+"/BasicService";
            rs.comm.postMessage(message1);
            CommMessage message2 = new CommMessage(CommMessage.MessageType.Ready);
            message2.body = "ready";
            message2.to = "http://localhost:9080/BasicService";
            message2.from = "http://localhost:"+args[0]  + "/BasicService";
            Console.Write("\n\n Sending Ready CommMessage to Mother builder");
            rs.comm.postMessage(message2);
            rs.readyAgain = message2;
            rs.receiverThread = new Thread(rs.receiverThreadProc);
            rs.receiverThread.Start();


        }
#endif
    }
}
