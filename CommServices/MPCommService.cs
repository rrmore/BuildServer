////////////////////////////////////////////////////////////////////////////////////////
// MPCommService.cs: service for message passing communication.                       //
// ver 1.0                                                                            //
//                                                                                    //
//Language:     Visual C#                                                             //
// Platform    : Acer Aspire R, Win Pro 10, Visual Studio 2017                        //
// Application : CSE-681 SMA Project 4                                                //
// Author      : Rohit More,Syracuse University                                       //
// Source      : Dr. Jim Fawcett, EECS, SU                                            //
////////////////////////////////////////////////////////////////////////////////////////

/*   
 * Package Operations:
 * -------------------
 * This package defines three classes:
 * - Sender which implements the public methods:
 *   -------------------------------------------
 *   - connect          : opens channel and attempts to connect to an endpoint, 
 *                        trying multiple times to send a connect message
 *   - close            : closes channel
 *   - postMessage      : posts to an internal thread-safe blocking queue, which
 *                        a sendThread then dequeues msg, inspects for destination,
 *                        and calls connect(address, port)
 *   - postFile         : attempts to upload a fileName in blocks
 *   - getLastError     : returns exception messages on method failure
 * - Receiver which implements the public methods:
 *   ---------------------------------------------
 *   - start            : creates instance of ServiceHost which services incoming messages
 *   - postMessage      : Sender proxies call this message to enqueue for processing
 *   - getMessage       : called by Receiver application to retrieve incoming messages
 *   - close            : closes ServiceHost
 *   - openFileForWrite : opens a fileName for storing incoming fileName blocks
 *   - writeFileBlock   : writes an incoming fileName block to storage
 *   - closeFile        : closes newly uploaded fileName
 * - Comm which implements, using Sender and Receiver instances, the public methods:
 *   -------------------------------------------------------------------------------
 *   - postMessage      : send CommMessage instance to a Receiver instance
 *   - getMessage       : retrieves a CommMessage from a Sender instance
 *   - postFile         : called by a Sender instance to transfer a fileName
 * 
 *
 * Required Files:
 * ---------------
 * IMPCommService.cs, MPCommService.cs
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 
 * - first release
 * 
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.Threading;
using System.IO;
using Project4Comm;

namespace Project4
{
    public class Receiver : IService
    {
        public static SWTools.BlockingQueue<CommMessage> rcvQ { get; set; } = null;
        public bool restartFailed { get; set; } = false;
        ServiceHost commHost = null;
        FileStream filestream = null;
        string lastError = "";


        /*----< constructor >------------------------------------------*/

        public Receiver()
        {
            if (rcvQ == null)
                rcvQ = new SWTools.BlockingQueue<CommMessage>();
        }
        /*----< create ServiceHost listening on specified endpoint >---*/
        /*
         * baseAddress is of the form: http://IPaddress or http://networkName
         */
        public void start(string baseAddress, int port)
        {
            string address = baseAddress + ":" + port.ToString() + "/BasicService";
            createCommHost(address);
        }
        /*----< create ServiceHost listening on specified endpoint >---*/
        /*
         * address is of the form: http://IPaddress:8080/IMessagePassingComm
         */
        public void createCommHost(string address)
        {
            WSHttpBinding binding = new WSHttpBinding();
            Uri baseAddress = new Uri(address);
            commHost = new ServiceHost(typeof(Receiver), baseAddress);
            commHost.AddServiceEndpoint(typeof(IService), binding, baseAddress);
            commHost.Open();
        }
        /*----< enqueue a message for transmission to a Receiver >-----*/

        public void postMessage(CommMessage msg)
        {
            rcvQ.enQ(msg);
        }
        /*----< retrieve a message sent by a Sender instance >---------*/

        public CommMessage getMessage()
        {
            CommMessage msg = rcvQ.deQ();
            if (msg.type == CommMessage.MessageType.closeReceiver)
            {
                close();
            }
            return msg;
        }
        /*----< close ServiceHost >------------------------------------*/

        public void close()
        {
            Console.Write("\n  closing receiver - please wait");
            commHost.Close();
            Console.Write("\n  commHost.Close() returned");
        }
        /*---< called by Sender's proxy to open fileName on Receiver >-----*/

        public bool openFileForWrite(string name, string receivePath)
        {
            try
            {
                string filename = Path.Combine(receivePath, name);
                //  string writePath = Path.Combine(ServerEnvironment.root, name);
                filestream = File.OpenWrite(filename);
                return true;
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                return false;
            }
        }
        /*----< write a block received from Sender instance >----------*/

        public bool writeFileBlock(byte[] block)
        {
            try
            {
                filestream.Write(block, 0, block.Length);
                return true;
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                return false;
            }
        }
        /*----< close Receiver's uploaded fileName >-----------------------*/

        public void closeFile()
        {
            filestream.Close();
        }
    }
    ///////////////////////////////////////////////////////////////////
    // Sender class - sends messages and files to Receiver

    public class Sender
    {
        private IService channel;
        private ChannelFactory<IService> factory = null;
        private SWTools.BlockingQueue<CommMessage> sndQ = null;
        private int port = 0;
        private string from = "";
        private string to = "";
        Thread sndThread = null;
        int tryCount = 0, maxCount = 10;
        string lastError = "";
        string lastUrl = "";
        string Storagepath = @"../../../RepoStorage";

        /*----< constructor >------------------------------------------*/

        public Sender(string baseAddress, int listenPort)
        {
            port = listenPort;
            from = baseAddress + listenPort.ToString() + "/BasicService";
            sndQ = new SWTools.BlockingQueue<CommMessage>();
            sndThread = new Thread(threadProc);
            sndThread.Start();
        }
        /*----< creates proxy with interface of remote instance >------*/

        public void createSendChannel(string address)
        {
            EndpointAddress baseAddress = new EndpointAddress(address);
            WSHttpBinding binding = new WSHttpBinding();
            factory = new ChannelFactory<IService>(binding, address);
            channel = factory.CreateChannel();
        }
        /*----< attempts to connect to Receiver instance >-------------*/

        public bool connect(string baseAddress, int port)
        {
            to = baseAddress + ":" + port.ToString() + "/BasicService";
            return connect(to);
        }
        /*----< attempts to connect to Receiver instance >-------------*/
        /*
         * - attempts a finite number of times to connect to a Receiver
         * - first attempt to send will throw exception of no listener
         *   at the specified endpoint
         * - to test that we attempt to send a connect message
         */
        public bool connect(string to)
        {
            int sleepTime = 500;
            createSendChannel(to);
            CommMessage connectMsg = new CommMessage(CommMessage.MessageType.connect);
            while (true)
            {
                try
                {
                    channel.postMessage(connectMsg);
                    tryCount = 0;
                    return true;
                }
                catch (Exception ex)
                {
                    if (++tryCount < maxCount)
                    {
                        Thread.Sleep(sleepTime);
                    }
                    else
                    {
                        lastError = ex.Message;
                        return false;
                    }
                }
            }
        }
        /*----< closes Sender's proxy >--------------------------------*/

        public void close()
        {
            if (factory != null)
                factory.Close();
        }
        /*----< processing for send thread >--------------------------*/
        /*
         * - send thread dequeues send message and posts to channel proxy
         * - thread inspects message and routes to appropriate specified endpoint
         */
        void threadProc()
        {
            while (true)
            {
                CommMessage msg = sndQ.deQ();
                if (msg.type == CommMessage.MessageType.closeSender)
                {

                    break;
                }
                /*if (msg.to == lastUrl)
                {
                    channel.postMessage(msg);
                }
                */
                else
                {
                    close();
                    if (!connect(msg.to))
                        return;
                    lastUrl = msg.to;
                    channel.postMessage(msg);
                }
            }
        }
        /*----< main thread enqueues message for sending >-------------*/

        public void postMessage(CommMessage msg)
        {
            sndQ.enQ(msg);
        }
        /*----< uploads fileName to Receiver instance >--------------------*/

        public bool postFile(CommMessage msg)
        {
            FileStream filestream = null;
            long bytesRemaining;
            String path = "";
            try
            {
                close();
                connect(msg.to);
                foreach (string fileName in msg.fileNames)
                {
                    if (msg.dllPath != null)
                    {
                        path = Path.Combine(msg.dllPath, fileName);
                    }
                    else
                    {
                        path = Path.Combine(Storagepath, fileName);
                    }
                    filestream = File.OpenRead(path);
                    bytesRemaining = filestream.Length;
                    channel.openFileForWrite(fileName, msg.filePath);
                    while (true)
                    {
                        long bytesToRead = Math.Min(1024, bytesRemaining);
                        byte[] blk = new byte[bytesToRead];
                        long numBytesRead = filestream.Read(blk, 0, (int)bytesToRead);
                        bytesRemaining -= numBytesRead;

                        channel.writeFileBlock(blk);
                        if (bytesRemaining <= 0)
                            break;
                    }
                    channel.closeFile();
                    filestream.Close();
                }
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                return false;
            }
            postCsFile(msg);
            postDllFiles(msg);
            return true;
        }

        //---------helper function to post cs files
        public void postCsFile(CommMessage msg)
        {
            if (msg.fileNames[0].EndsWith(".cs"))
            {
                CommMessage formDllMessage = new CommMessage(CommMessage.MessageType.BuildDLL);
                formDllMessage.to = msg.to;
                formDllMessage.from = msg.from;
                formDllMessage.fileNames = null;
                formDllMessage.body = msg.body;
                channel.postMessage(formDllMessage);
            }
        }

        public void postDllFiles(CommMessage msg)
        {
            if (msg.fileNames[0].EndsWith(".dll"))
            {
                CommMessage executeDllMsg = new CommMessage(CommMessage.MessageType.TestExecute);
                executeDllMsg.to = msg.to;
                executeDllMsg.from = msg.from;
                executeDllMsg.filePath = msg.filePath;
                executeDllMsg.body = msg.body;
                channel.postMessage(executeDllMsg);
            }
        }

    }

   

    public class Comm
    {
        private Receiver rcvr = null;
        private Sender sndr = null;
        private string address = null;
        private int portNum = 0;

        /*----< constructor >------------------------------------------*/
        /*
         * - starts listener listening on specified endpoint
         */
        public Comm(string baseAddress, int port)
        {
            address = baseAddress;
            portNum = port;
            rcvr = new Receiver();
            rcvr.start(baseAddress, port);
            sndr = new Sender(baseAddress, port);
        }

        /*----< closes Comm instance >--------------------------*/

        public void close()
        {
            Console.Write("\n  Comm closing");
            rcvr.close();
            sndr.close();
        }
        /*----< post message to remote Comm >--------------------------*/

        public void postMessage(CommMessage msg)
        {
            sndr.postMessage(msg);
        }
        /*----< retrieve message from remote Comm >--------------------*/

        public CommMessage getMessage()
        {
            return rcvr.getMessage();
        }
        /*----< called by remote Comm to upload fileName >-----------------*/

        public bool postFile(CommMessage msg)
        {
            return sndr.postFile(msg);
        }
#if (MPCOMMSERVICE)
        public static void Main()
        {
            Comm comm = new Comm("http://localhost:",8080);
            CommMessage msg = new CommMessage(CommMessage.MessageType.connect);
            msg.from = "http://localhost:8080/BasicService";
            msg.to = "http://localhost:8080/BasicService";
            comm.postMessage(msg);
            CommMessage msg1=comm.getMessage();
            Console.Write("\n received message type is:" + msg1.type);
        }
#endif

    }

}
