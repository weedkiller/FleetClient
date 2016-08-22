﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FleetIPC;
using System.ServiceModel;
using System.Diagnostics;
using System.IO;
using FleetServer;
using System.Net.NetworkInformation;
using Newtonsoft.Json;
using FleetServer;

namespace FleetDaemon
{
    class Program
    {
        static void Main(string[] args)
        {
            var daemon = Daemon.Instance;
            daemon.Run();
            Console.ReadLine();
        }
    }

    class Daemon
    {
        // Static instance handling
        private static Daemon instance;
        public static Daemon Instance { get
            {
                if (instance == null)
                {
                    instance = new Daemon();
                }
                return instance;
            }
        }

        private ServiceHost service;
        private SimpleStorage Storage;

        private IPCMessage FileShareMessage;
        private FleetClientToken ClientToken;
        private IFleetService FleetServer
        {
            get
            {
                var client = this.FleetServer;
                if (client == null)
                {
                    string address = null; // TODO: Get address from config or whatever
                    var remoteAddress = new System.ServiceModel.EndpointAddress(address);
                    var binding = new System.ServiceModel.BasicHttpBinding();
                    binding.MaxReceivedMessageSize = int.MaxValue;
                    binding.MaxBufferSize = int.MaxValue;
                    client = new FleetServiceClient(binding, remoteAddress);
                    ((FleetServiceClient)client).Endpoint.Binding.SendTimeout = new TimeSpan(0, 0, 20, 0);
                    FleetServer = client;
                }
                return FleetServer;
            }
            set
            {
                FleetServer = value;
            }
        }

        private Daemon()
        {
            DaemonService.OnRequest += DaemonService_OnRequest;
            this.FleetServer = null;
            this.Storage = new SimpleStorage("./filestore.json");

            var processes = new Dictionary<String, String>();
            processes.Add("drag_drop", @"..\..\..\FileShare\bin\Debug\FileShare.exe");
            this.Storage.Store("process_list", processes);
            
        }

        private void DaemonService_OnRequest(IPCMessage message)
        {
            Console.WriteLine(String.Format("Received message from: {0}, to: {1}", message.ApplicaitonSenderID, message.ApplicationRecipientID));
            Console.WriteLine(String.Format("Message Type: {0}", message.Content["type"]));

            switch (message.LocationHandle)
            {
                case IPCMessage.MessageLocationHandle.REMOTE:
                    //TODO(AL+JORDAN): Check if communication is granted access
                    // Que process send request so that only one workstation_selector runs at a time

                    var workstationSelector = RunProcess("workstation_selector");

                    //Selection process is running, setup wait for send
                    // Okay so now that's showin lets setup to wait for a reply
                    this.FileShareMessage = message;

                    if (message.Type.Equals("sendFile"))
                    {
                        Console.WriteLine("We got a file.");
                        Console.WriteLine(String.Format("File URL: {0}", message.Content["fileurl"]));
                    }
                    /* Example: 
                    else if (message.Type.Equals("quiz"))
                    {

                    }
                    */
                    break;

                case IPCMessage.MessageLocationHandle.LOCAL:
                    //TODO(AL+JORDAN): Check if the process name given exists
                    //                 Check if communication is granted access
                    //                 Check if it is running
                    //                 
                    break;

                case IPCMessage.MessageLocationHandle.DAEMON:
                    //NOTE(AL+JORDAN): These are all the messages directed for the daemon to handle
                    
                    //TODO(AL+JORDAN): Worstations selected
                    //                 Accept or rejection of file
                    //                 
                    // workstationShareList, fileAccepted, 

                    if(message.Type.Equals("workstationShareList"))
                    {
                        //NOTE(AL): Workstation process is sending us the list of 
                        //          workstations we want to send a file to

                        /*NOTE(AL): For client to send it needs to do the following
                        
                            JsonSerializer serializer = new JsonSerializer();
                            var fcid = new FleetClientIdentifier();
                            fcid.Identifier = "test";
                            fcid.WorkstationName = "test";
                            
                            FleetClientIdentifier[] new_array = { fcid, fcid1 };
                            String message_content = JsonConvert.SerializeObject(new_array);
                            
                            And then send it in a message 
                            message.Content["workstations"] = message_content;
                        */

                        var workstations = JsonConvert
                            .DeserializeObject<string[]>(message.Content["workstations"]);

                        var filePath = (string)this.FileShareMessage.Content["filePath"];

                        var fileContent = File.ReadAllBytes(filePath);
                        string fileName = filePath.Split(Path.DirectorySeparatorChar)
                            .Last();

                        var file = new FleetFile{
                            FileContents = fileContent,
                            FileName = fileName
                        };

                        var selectedClients = workstations.Select(s => new FleetClientIdentifier
                        {
                            Identifier = s,
                            WorkstationName = ""
                        });

                        var server = new FleetServiceClient();
                        server.SendFileMultipleRecipient(this.ClientToken, selectedClients.ToArray(), file);
                    }
                    else if(message.Type.Equals("fileAccepted"))
                    {
                        // message.Content["accepted"] = true | false;
                        // message.Content["filePath"]
                        //TODO(AL+JORDAN):  open the file in the default thing
                    }

                break;
                default:
                    Console.WriteLine("SADFACE please figure out what to do here");
                break;

                Console.WriteLine("We got a file.");
                Console.WriteLine(String.Format("File URL: {0}", message.Content["fileurl"]));
            }
        }

        public void Run()
        {
            // Service initialisation
            var address = new Uri("net.pipe://localhost/fleetdaemon");
            var binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);
            this.service = new ServiceHost(typeof(DaemonService));
            this.service.AddServiceEndpoint(typeof(IDaemonIPC), binding, address);
            this.service.Open();

            Console.WriteLine("Daemon IPC service listening");

            // Create registration token
            var clientReg = new FleetClientRegistration();
            clientReg.FriendlyName = System.Environment.MachineName;
            clientReg.IpAddress = "boop boop we gotta implement";
            clientReg.MacAddress = (from nic in NetworkInterface.GetAllNetworkInterfaces()
                                    where nic.OperationalStatus == OperationalStatus.Up
                                    select nic.GetPhysicalAddress().ToString()
                                    ).FirstOrDefault();

            // Register with server
            var client = FleetServer;
            ClientToken = client.RegisterClient(clientReg);

            Console.WriteLine("Received registration token");

            // Start heartbeat
            HearbeatManager.WaitLength = 2000;
            HearbeatManager.Instance.StartHeartbeat(ClientToken);

            Console.WriteLine("Heartbeat is running");

            // Other loading
            // ????

            // Daemon is running
            Console.WriteLine("Daemon running. Press the any key to exit.");
            Console.ReadLine();

            //Console.WriteLine(Directory.GetCurrentDirectory());
            //var clientToken = this.FleetServer.RegisterClient(clientReg);
            //this.Storage.store("token", clientToken);
            //this.Storage.store("token", "test_token_cool");
            //Console.WriteLine("Client registered to Server.");
 
            //Process.Start(@"..\..\..\FileShare\bin\Debug\FileShare.exe");
        }

        
        public void HandleFileReceive(String filename)
        {

        }
    }

    public class FleetServerStub : IFleetService
    {
        public FleetFile GetFile(FleetClientToken token, FleetFileIdentifier fileId)
        {
            throw new NotImplementedException();
        }

        public FleetMessage GetMessage(FleetClientToken token, FleetMessageIdentifier fileId)
        {
            throw new NotImplementedException();
        }

        public FleetHearbeatEnum Heartbeat(FleetClientToken token)
        {
            throw new NotImplementedException();
        }

        public FleetHearbeatEnum Heartbeat(FleetClientToken token, FleetClientIdentifier[] knownClients)
        {
            throw new NotImplementedException();
        }

        public FleetFileIdentifier[] QueryFiles(FleetClientToken token)
        {
            throw new NotImplementedException();
        }

        public FleetMessageIdentifier[] QueryMessages(FleetClientToken token)
        {
            throw new NotImplementedException();
        }

        public FleetClientToken RegisterClient(FleetClientRegistration registrationModel)
        {
            throw new NotImplementedException();
        }

        public bool SendFileMultipleRecipient(FleetClientToken token, FleetClientIdentifier[] recipients, FleetFile file)
        {
            throw new NotImplementedException();
        }


        private Process RunProcess(String processName)
        {
            var processes = Storage.Get<Dictionary<String, String>>("process_list");
            Console.WriteLine(processes);
            if(processes.ContainsKey(processName))
            {
                var p = Process.Start(processes[processName]);
                return p;
            }
            return null;            
        } 
        
    }

    class SimpleStorage
    {
        public String filePath;
        public Dictionary<String, Object> storage;
        public SimpleStorage(String filePath)
        {
            this.filePath = filePath;

            if(File.Exists(filePath))
            {
                try
                {
                    using (StreamReader file = File.OpenText(filePath))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        this.storage = (Dictionary<String, Object>)serializer.Deserialize(file, typeof(Dictionary<String, Object>));
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                    this.storage = new Dictionary<String, Object>();
                }
            }
            else
            {
                this.storage = new Dictionary<String, Object>();
            }
        }

        public T Get<T>(String key)
        {
            Object val;
            storage.TryGetValue(key, out val);
            if (val == null) return default(T);
            return (T)val;
        }

        public bool Store(Dictionary<String, Object> dict)
        {
            this.storage = new Dictionary<String, Object>(dict);
            return WriteToFile();
        }

        public bool Store(String key, Object value)
        {
            this.storage[key] = value;
            return WriteToFile();
        }

        private bool WriteToFile()
        {
            try
            {
               using (StreamWriter file = File.CreateText(this.filePath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(file, storage);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }

            return true;
        }
    }
}
