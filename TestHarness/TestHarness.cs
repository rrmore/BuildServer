////////////////////////////////////////////////////////////////////////////////////////
// TestHarness.cs : This package is used for testing of dll files                     //
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
* The below module performs testing of dll files.
* It also provides test logs of the testing
*
* public Interfaces:
* =================
* loadTest(): performs loading of tests
* run(): performs testing on test driver
* parseBuildRequest(): Parses test requests received from build server 
* performTest(): provides dll files to perfomr testing 
* generateTestLogs(): generate test logs after testing
*
* receiverThreadProc(): checks the receiver and performs operations based on the message
*
* Required Files:
* ===============
* XMLManager.cs,Repository.cs,ITest.cs,TestInterfaces.dll
*
* Maintainance History:
* =====================
* ver 1.0
*
*/



using Project4Comm;
using LoadingTests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Project4
{
    class MockTestHarness
    {
        Comm comm { get; set; } = null;
        Thread rcvThread = null;
        public string Receivepath { get; set; } = "";
        List<LogManager> data = new List<LogManager>();
        static ITest testDriver;

        public MockTestHarness()
        {
            comm = new Comm("http://localhost",9090);
            rcvThread = new Thread(receiverThreadProc);
            rcvThread.Start();

        }

        //--------------------------Creates test log file---------------------------------------------------------------------------------
        public void generateBuildLogs(TestElement t, string buildStatus, string authorName)
        {
            LogManager logData = new LogManager();
            logData.author = authorName;
            logData.testname = t.testName;
            if (buildStatus == null) logData.status = "Test Success";
            else { logData.status = "Test Failed"; logData.message = buildStatus; }
            data.Add(logData);
        }


        //--------------------------Parses build request--------------------------------------------------------------
        public List<String> parseBuildRequest(String buildRequest)
        {

            Console.Write("\n TestHarnes parses the Test Request");
            List<String> files = new List<string>();
            TestRequest testRequestNew = buildRequest.FromXml<TestRequest>();
            Receivepath = "../../../TestHarness/" + "DLL_PATH" + "_" + DateTime.Now.ToString("yyyyMMddHHmmssffff");
            Directory.CreateDirectory(Receivepath);
            string testRequest = Path.GetFullPath(Receivepath) + "/TestRequest" + DateTime.Now.ToFileTime() + ".xml";
            XDocument doc = XDocument.Parse(buildRequest);
            doc.Save(testRequest);
            List<TestElement> test = testRequestNew.tests;
            foreach (TestElement t in test)
            {
                files.Add(t.testDriver);
                List<String> codes = t.testCodes;
                foreach (String c in codes)
                {
                    files.Add(c);
                }
            }
            return files;
        }


        //----------------------starter function to perform test on dll files----------------------------------------------
        public void performTest(CommMessage msg)
        {
            TestRequest newRequest = msg.body.FromXml<TestRequest>();
            String name = newRequest.author;
            String testLog = "";
            List<TestElement> test = newRequest.tests;
            foreach (TestElement t in test)
            {
                string[] tempFiles = Directory.GetFiles(msg.filePath, t.testDriver);
                tempFiles[0] = Path.GetFullPath(tempFiles[0]);

                string status = loadTest(tempFiles[0]);
                generateBuildLogs(t, status, name);
            }
            if (test.Count != 0)
            {
                testLog=LogManager.createLogs(data, "TestLog");
            }
            CommMessage msg1 = new CommMessage(CommMessage.MessageType.TestLog);
            msg1.from = msg.to;
            msg1.to = "http://localhost:9070/BasicService";
            msg1.body = testLog;
            comm.postMessage(msg1);
            data.Clear();
            Console.Write("\n Test Request executed");
        }


        //--------------------load dll files and identify test driver using reflection-----------------------------------------------------
        public string loadTest(string fileName)
        {
            Console.Write("\n Testing the following files "+fileName);
            string status = "";
            try
            {
                Assembly assmbly = Assembly.LoadFrom(fileName);
                Type[] types = assmbly.GetTypes();
                foreach (Type t in types)
                {
                    if (t.IsClass && typeof(ITest).IsAssignableFrom(t))  // does this type derive from ITest ?
                    {
                        ITest tdr = (ITest)Activator.CreateInstance(t);    // create instance of test driver
                        testDriver = tdr;
                        status = run(testDriver);
                    }
                }
            }

            catch (Exception ex)
            {
                //------------------Display error message if any--------------------------------------------------------------------
                Console.Write("\nError:  {0}", ex.Message);
                return ex.Message;
            }
            return status;
        }

        //-----------------performs the test on library---------------------------------------------------------------------
        public string run(ITest testDrvr)
        {
            try
            {
                if (testDrvr.test() == true)
                    Console.Write("\n Test status= Passed");
                else
                    Console.Write("\n Test status= Failed");
            }
            catch (Exception ex)
            {
                Console.Write("\n Error:  {0}", ex.Message);
                return ex.Message;
            }
            return null;
        }


        //---------------------Checks the receivers queue for messages------------------------------------------------------
        void receiverThreadProc()
        {
            Console.Write("\n Test Harness's receive thread is  started");
            while (true)
            {
                CommMessage commMessage = comm.getMessage();
                Console.Write("\n Message received by the test harness of type "+commMessage.type);
                commMessage.show();
                switch(commMessage.type)
                {
                    case CommMessage.MessageType.TestRequest:
                        commMessage.fileNames = parseBuildRequest(commMessage.body);
                        commMessage.filePath = Receivepath;
                        commMessage.fileName = null;
                        commMessage.body = commMessage.body;
                        commMessage = addressSwap(commMessage);
                        Console.Write("\n Test harness sends request message to child builder for libraries");
                        comm.postMessage(commMessage);
                        break;
                    case CommMessage.MessageType.TestExecute:
                        Console.Write("\n Test Harness received the required libraries");
                        performTest(commMessage);
                        break;
                }
                Console.Write("\n");
                // pass the Dispatcher's action value to the main thread for execution
            }
        }


         // -----------Swaps to and from adress of the message-------------------------------------------------------
        CommMessage addressSwap(CommMessage msg)
        {
            if (msg.from != null)
            {
                String temp = msg.to;
                msg.to = msg.from;
                msg.from = temp;
            }
            return msg;
        }

#if(TESTHARNESS)
        static void Main(string[] args)
        {
            Console.Write("\n --------------------Test Harness is started----------------------------------------");
            MockTestHarness th = new MockTestHarness();
            CommMessage msg = new CommMessage(CommMessage.MessageType.connect);
            msg.to = "http://localhost:9090/BasicService";
            msg.from = "http://localhost:9090/BasicService";
            th.comm.postMessage(msg);
        }
#endif
    }
}
