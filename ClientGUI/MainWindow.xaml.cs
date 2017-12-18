////////////////////////////////////////////////////////////////////////////////////////
// MainWindow.xaml.cs : This package helps to display things on window.               //
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
* This package displays the main window to the client.
* The package helps to display the build requests, logs from the repository to the client.
* The client can create build request and can store or send the build request to the mother builder.
* This module also demonstartes the requirements for WPF module in the console.
*
* public Interfaces:
* =================
* 
* testDriverDispatcher(): shows test driver in the list box
* getDriversOnClick(): requests the repository to get the test driver.
* getTestCodesClick(): sends a message to repository to for displaying test codes.
*  viewBuildLogsClick(): requests the repository for build logs
* showBuildlogs(): shows build logs in the listbox
* buildLogFiles2Click(): shows the build log in a pop up window
* displayTestLogs_Click(): requests the repository for build logs
* addTestLogs(): shows test log in the listbox of the GUI
* Add_Click(): add the selected fil
* Remove_Click(): clears files from the right list box
* createOnClick(): generates build reques
* saveBRequestOnClick(): requests repository to save the build request.
* sendToMotherBuilder_Click(): transfers build request to mother builder
* sendBuildRequests_Click(): sends build requests to mother builder.
* showBuildRequestsOnClick(): requests repository for displaying build requests.
* addBuildRequests(): shows build request from repo in the list box
* showBuildRequestsOn2Click(): displays the build request details in a pop up text
* viewSelectedFile(): displays the contents of a fileName in a pop up test
* receiverThreadProc(): checks the receiver queue abd perofrms operations based on the message
* addDriversLBox(): displays test driver from repo in the UI
* addTestCodes(): displays test code from repo in ui

* showTestLogs2Click(): opens a new window to display content of test log.
*
* Required Files:
* ===============
* XMLManager.cs,serialization.cs,MPCommService.cs,IService.cs
*
* Maintainance History:
* =====================
* ver 1.0
*
*/



using Project4Comm;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.IO;


namespace Project4
{
    public partial class MainWindow : Window
    {
        IAsyncResult asyncResult;
        Action<List<String>> proc = null;
        CommMessage message;
        static int testNo = 1;
        Comm comm { get; set; } = null;
        Thread receiverThread = null;
        TestRequest testRequest;

        public MainWindow()
        {
            InitializeComponent();
            comm = new Comm("http://localhost", 9060);
            message = new CommMessage(CommMessage.MessageType.connect);
            message.to = "http://localhost:9070/BasicService";
            message.from = "http://localhost:9060/BasicService";
            receiverThread = new Thread(receiverThreadProc);
            receiverThread.Start();
            main();

        }


        //----------- provides dispatcher to show drivers in GUI--------------------------------------------------------------------
        void testDriverDispatcher(List<string> driversList)
        {
            foreach (string fileName in driversList)
            {
                if (Dispatcher.CheckAccess())
                    addFiletDriver(fileName);
                else
                    Dispatcher.Invoke(
                      new Action<string>(addFiletDriver),
                      System.Windows.Threading.DispatcherPriority.Background,
                       new string[] { fileName }
                    );
            }
        }

        //---------------Thread to check messages in the receiver queue------------------------------------------------------------
        void receiverThreadProc()
        {
            Console.Write("\n  Started receiver thread for Client");
            while (true)
            {
                CommMessage msg1 = comm.getMessage();
                msg1.show();
                switch (msg1.type)
                {
                    case CommMessage.MessageType.Drivers:
                        addDriversLBox(msg1.fileNames);
                        break;
                    case CommMessage.MessageType.TestCodes:
                        addTestCodes(msg1.fileNames);
                        break;
                    case CommMessage.MessageType.TestLogs:
                        addTestLogs(msg1.fileNames);
                        break;
                    case CommMessage.MessageType.BuildRequests:
                        addBuildRequests(msg1.fileNames);
                        break;
                    case CommMessage.MessageType.BuildLogs:
                        showBuildlogs(msg1.fileNames);
                        break;
                }
            }
        }


        //------send spawn process message to mother builder-------------------------------------------------------------------------------
        private void startProcessOnClick(object sender, RoutedEventArgs e)
        {

            String num = numberofProcess.SelectedIndex.ToString();
            int nums = Convert.ToInt32(num);
            CommMessage msg = new CommMessage(CommMessage.MessageType.SpawnProcess);
            msg.numberProcess = nums + 1;
            msg.from = "http://localhost:9060/BasicService";
            msg.to = "http://localhost:9080/BasicService";
            comm.postMessage(msg);
        }

        //------------------------------displays drivers int the listbox-----------------------------------------------------------------------------
        public void addDriversLBox(List<string> driversList)
        {
            proc = this.testDriverDispatcher;
            asyncResult = proc.BeginInvoke(driversList, null, null);
        }


        //---------------------------displays test codes in the list box of GUI-----------------------------------------------------------------
        public void addTestCodes(List<string> testCodeList)
        {
            proc = this.addTestCodesDispatcher;
            asyncResult = proc.BeginInvoke(testCodeList, null, null);
        }

        //-------------uses a dispatcher to display test codes in the GUI(Helper function)---------------------------------------------------
        void addTestCodesDispatcher(List<string> testCodeList)
        {
            foreach (string fileName in testCodeList)
            {
                if (Dispatcher.CheckAccess())
                    populateTestCases(fileName);
                else
                    Dispatcher.Invoke(
                      new Action<string>(populateTestCases),
                      System.Windows.Threading.DispatcherPriority.Background,
                       new string[] { fileName }
                    );
            }

        }

        //---------------------Inserts the provided file in the list box----------------------------------------------------------------
        void addFiletDriver(string fileName)
        {
            leftDriverBox.Items.Insert(0, fileName);
            Console.Write("\n Added test driver:" + fileName + " in the list box");
        }


        //-----------insert the test cases in the list box of GUI------------------------------------------------------------------------
        void populateTestCases(string fileName)
        {
            TestCodeBoxLeft.Items.Insert(0, fileName);
            Console.Write("\n Added test code:" + fileName + " in the list box");
        }


        //----------------sends test driver request message to repo------------------------------------------------------------------------
        private void getDriversOnClick(object sender, RoutedEventArgs e)
        {
            leftDriverBox.Items.Clear();
            CommMessage msg = message.Clone();
            msg.type = CommMessage.MessageType.Drivers;
            comm.postMessage(msg);
            Console.Write("Request sent to Repo for test drivers");
        }


        //------------------------sends a request message to repo for test codes-----------------------------------------------------------
        private void getTestCodesClick(object sender, RoutedEventArgs e)
        {
            TestCodeBoxLeft.Items.Clear();
            CommMessage msg = message.Clone();
            msg.type = CommMessage.MessageType.TestCodes;
            comm.postMessage(msg);
            Console.Write("Request sent to Repo for test codes");
        }


        //--------------------Inserts the drivers and test codes in the right text box----------------------------------------------------
        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (leftDriverBox.SelectedItems.Count == 0)
            {
                MessageBox.Show("Select one test Driver");
                return;
            }
            if (TestCodeBoxLeft.SelectedItems.Count == 0)
            {
                MessageBox.Show("Select atleast one test code");
                return;
            }
            foreach (var x in TestCodeBoxLeft.SelectedItems)
            {
                Console.Write("\n The test code selected by the user is " + x);
                rightTCBox.Items.Insert(0, x);
            }
            foreach (var x in leftDriverBox.SelectedItems)
            {
                Console.Write("\n The test code selected by the user is " + x);
                rightDriverBox.Items.Insert(0, x);
            }
        }


        //----------------------------removes the file list from the right list box-------------------------------------------------------
        private void removeOnClick(object sender, RoutedEventArgs e)
        {

            for (int i = 0; i < rightTCBox.SelectedItems.Count; i++)
            {
                rightTCBox.Items.Remove(rightTCBox.SelectedItems[i]);
            }
            for (int i = 0; i < rightDriverBox.SelectedItems.Count; i++)
            {
                rightDriverBox.Items.Remove(rightDriverBox.SelectedItems[i]);
            }
            rightTCBox.Items.Refresh();
        }

        //----------------------------------sends build request to mother builder-------------------------------------------------------------
        private void sendToMotherBuilder_Click(object sender, RoutedEventArgs e)
        {
            if (createdBuildRequest.Text.Equals(""))
            {
                MessageBox.Show("Please create a build request");
                return;
            }
            CommMessage msg = new CommMessage(CommMessage.MessageType.BuildRequest);
            msg.to = "http://localhost:9080/BasicService";
            msg.from = "http://localhost:9060/BasicService";
            msg.body = createdBuildRequest.Text;
            comm.postMessage(msg);
            Console.Write("\n Created build request sent to mother builder successfully");
            MessageBox.Show("Build Request sent to Mother Builder");
        }


        //------------------creates build request using the selected files---------------------------------------------------------------
        private void createOnClick(object sender, RoutedEventArgs e)
        {
            List<String> testCasesList = new List<String>();
            List<String> driversList = new List<String>();
            if (rightDriverBox.Items.Count <= 0)
            {
                MessageBox.Show("Please select and add a Test Driver");
                return;
            }
            else if (rightTCBox.Items.Count <= 0)
            {
                MessageBox.Show("Please select and add atleast one testcode");
                return;
            }
            else
            {
                foreach (String testcase in rightTCBox.Items)
                {
                    testCasesList.Add(System.IO.Path.GetFileName(testcase));
                }
                foreach (String driver in rightDriverBox.Items)
                {
                    driversList.Add(System.IO.Path.GetFileName(driver));
                }
                if (testRequest == null)
                {
                    testRequest = new TestRequest();
                }
                TestElement testElement = new TestElement();
                testElement.addDriver(driversList[0]);
                foreach (string testcode in testCasesList)
                {
                    testElement.addCode(testcode);
                }
                testElement.testName = "test" + testNo;
                testNo++;
                testRequest.author = "Jim Fawcett";
                testRequest.tests.Add(testElement);
            }
            String xml = testRequest.ToXml();
            Console.Write("\n\nThe created XML is:\n" + xml + "\n");
            createdBuildRequest.Clear();
            createdBuildRequest.Text = xml;
        }


        //------------Helper function to add more tests to existing build rquest--------------------------------------------------
        private void addTestsOnClick(object sender, RoutedEventArgs e)
        {
            if (createdBuildRequest.Text.Equals("") || createdBuildRequest.Text == null)
            {
                MessageBox.Show("You have to create a build request first");
                return;
            }
            leftDriverBox.SelectedIndex = -1;
            TestCodeBoxLeft.SelectedIndex = -1;
            rightDriverBox.Items.Clear();
            rightTCBox.Items.Clear();
        }


        //------------------clears the generated build request---------------------------------------------------------------------
        private void clearOnClick(object sender, RoutedEventArgs e)
        {
            leftDriverBox.SelectedIndex = -1;
            TestCodeBoxLeft.SelectedIndex = -1;
            rightDriverBox.Items.Clear();
            rightTCBox.Items.Clear();
            createdBuildRequest.Clear();
            testRequest = null;
            testNo = 1;
        }


        //-------------------Sends request to repo to save the build request----------------------------------------------------------
        private void saveBRequestOnClick(object sender, RoutedEventArgs e)
        {
            if (createdBuildRequest.Text.Equals(""))
            {
                MessageBox.Show("Please create a build request");
                return;
            }
            CommMessage msg = message.Clone();
            msg.body = createdBuildRequest.Text;
            msg.type = CommMessage.MessageType.SaveBuildRequest;
            comm.postMessage(msg);
            MessageBox.Show("Build Request  successfully saved in Repo");
            Console.Write("\nBuild Request  successfully saved in Repo");
        }


        //---------------Tranferring build request to  mother builder-----------------------------------------------------
        private void sendBRequestOnClick(object sender, RoutedEventArgs e)
        {
            if (buildRequests.SelectedItems.Count <= 0)
            {
                MessageBox.Show("Please select build Requests");
                return;
            }
            //------------Itearting over the selected build request items-----------------------------------------------------
            for (int i = 0; i < buildRequests.SelectedItems.Count; i++)
            {
                String path = System.IO.Path.GetFullPath(buildRequests.SelectedItems[i].ToString());
                CommMessage msg = new CommMessage(CommMessage.MessageType.BuildRequest);
                msg.to = "http://localhost:9080/BasicService";
                msg.from = "http://localhost:9060/BasicService";
                msg.body = File.ReadAllText(path);
                comm.postMessage(msg);
                Console.Write("\n sent message to mother builder with address: " + msg.to);

            }
            MessageBox.Show("Build Requests sent to Mother Builder");
        }


        //-----------------------------------sends build request message to repo-----------------------------------------------------
        private void showBuildRequestsOnClick(object sender, RoutedEventArgs e)
        {
            buildRequests.Items.Clear();
            CommMessage message1 = message.Clone();
            message1.type = CommMessage.MessageType.BuildRequests;
            Console.Write("\n Message for build request is created ");
            comm.postMessage(message1);
            Console.Write("\n sent message to repo with address: " + message1.to);
            buildRequests.Items.Clear();
        }


        //------------adds build request file using dispatcher---------------------------------------------------------------------------
        public void addBuildRequests(List<string> buildRequests)
        {
            proc = this.addBuildRequestsDispatcher;
            asyncResult = proc.BeginInvoke(buildRequests, null, null);
        }


        //------------------inserts build request in the list box--------------------------------------------------------------------------
        void addFileBuildRequests(string fileName)
        {
            buildRequests.Items.Insert(0, fileName);
        }


        //-------helper function to invoke dispatcher which executes display build request functionality---------------------------------------
        void addBuildRequestsDispatcher(List<string> buildRequests)
        {
            foreach (string fileName in buildRequests)
            {
                if (Dispatcher.CheckAccess())
                    addFileBuildRequests(fileName);
                else
                    Dispatcher.Invoke(
                      new Action<string>(addFileBuildRequests),
                      System.Windows.Threading.DispatcherPriority.Background,
                       new string[] { fileName }
                    );
            }

        }

        //------------------------------------Request repo for build logs-----------------------------------------------------------------
        private void viewBuildLogsClick(object sender, RoutedEventArgs e)
        {
            buildLogs.Items.Clear();
            CommMessage msg = message.Clone();
            msg.type = CommMessage.MessageType.BuildLogs;
            comm.postMessage(msg);
            Console.Write("\n Sent message for build logs to repo with address: " + msg.to);
        }

        //--------------------------Invokes dispatcher to add build log files to listbox --------------------------------------------
        public void showBuildlogs(List<string> buildLogs)
        {
            proc = this.showBuildlogsDispatcher;
            asyncResult = proc.BeginInvoke(buildLogs, null, null);
        }


        //------------------------shows buid request in the pop up window----------------------------------------------------
        private void showBuildRequestsOn2Click(object sender, RoutedEventArgs e)
        {
            String fileName = buildRequests.SelectedValue as string;
            viewSelectedFile(fileName);
        }


        //-----------------------displays the selected file in a pop up window------------------------------------------------
        private void viewSelectedFile(string fileName)
        {
            PopUpText viewFile = new PopUpText();
            String path = System.IO.Path.GetFullPath(fileName);
            viewFile.displayContent.Text = File.ReadAllText(path);
            viewFile.Show();
        }


        //-----------------------displays the selected build log file in the pop up window-------------------------------------------------------------
        private void buildLogFiles2Click(object sender, RoutedEventArgs e)
        {
            String fileName = buildLogs.SelectedValue as string;
            viewSelectedFile(fileName);
        }


        //--------------Requests repo for test logs----------------------------------------------------------------------------------------
        private void displayTestLogs_Click(object sender, RoutedEventArgs e)
        {
            testLogs.Items.Clear();
            CommMessage msg = message.Clone();
            msg.type = CommMessage.MessageType.TestLogs;
            comm.postMessage(msg);
            Console.Write("\n Sent message for test logs to repo with address: " + msg.to);

        }


        //---------------------------adds build logs to listbox ------------------------------------------------------------------------------------
        void addFileBuildLogs(string fileName)
        {
            buildLogs.Items.Insert(0, fileName);
        }


        //---------------------dispatcher checks for access and dispatches to a method for addition of fileName to list box-------------------------------
        void showBuildlogsDispatcher(List<string> buildLogs)
        {
            foreach (string fileName in buildLogs)
            {
                if (Dispatcher.CheckAccess())
                    addFileBuildLogs(fileName);
                else
                    Dispatcher.Invoke(
                      new Action<string>(addFileBuildLogs),
                      System.Windows.Threading.DispatcherPriority.Background,
                       new string[] { fileName }
                    );
            }

        }

        /*----< Invokes dispatcher to add test log files to listbox >----------------*/
        public void addTestLogs(List<string> testLogs)
        {
            proc = this.addTestLogsDispatcher;
            asyncResult = proc.BeginInvoke(testLogs, null, null);
        }


        //-----------------inserts test logs in the text box-------------------------------------------------------------------
        void insertFileTestLogs(string fileName)
        {
            testLogs.Items.Insert(0, fileName);
        }

        //----------------shows test logs in a pop up window----------------------------------------------------------------------------
        private void showTestLogs2Click(object sender, RoutedEventArgs e)
        {
            String fileName = testLogs.SelectedValue as string;
            viewSelectedFile(fileName);
        }


        //---------uses dispatcher to execute method for displaying test logs---------------------------------------------------
        void addTestLogsDispatcher(List<string> testLogs)
        {
            foreach (string fileName in testLogs)
            {
                if (Dispatcher.CheckAccess())
                    insertFileTestLogs(fileName);
                else
                    Dispatcher.Invoke(
                      new Action<string>(insertFileTestLogs),
                      System.Windows.Threading.DispatcherPriority.Background,
                       new string[] { fileName }
                    );
            }

        }


        void main()
        {
            CommMessage msg = new CommMessage(CommMessage.MessageType.connect);
            msg.to = "http://localhost:9060/BasicService";
            msg.from = "http://localhost:9060/BasicService";
            comm.postMessage(msg);

            CommMessage msg2 = msg.Clone();
            msg2.type = CommMessage.MessageType.BuildRequest;
            msg2.to = "http://localhost:9070/BasicService";
            msg2.body = "Build Request";
            comm.postMessage(msg2);
        }

    }
}
