﻿using FleetDaemon.Hauler;
using FleetDaemon.Storage.Interfaces;
using FleetIPC;
using FleetServer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.ServiceModel;
using System.Threading.Tasks;

namespace FleetDaemon
{
    public static class DaemonContext
    {
        public static FleetClientContext CurrentContext { get; set; } = FleetClientContext.Campus;
    }

    public class Daemon
    {
        // Static instance handling
        private ServiceHost Service;
        private ISimpleStorage Storage { get; set; }
        private IRouter Router { get; set; }

        private FleetClientToken ClientToken { get; set; }

        public Daemon(ISimpleStorage Store, IRouter router, FleetClientToken token)
        {
            this.Storage = Store;
            this.Router = router;
            this.ClientToken = token;
            DaemonService.OnRequest += DaemonService_OnRequest;
        }

        private void DaemonService_OnRequest(IPCMessage message)
        {
            Console.WriteLine(String.Format("Received message from: {0}, to: {1}", message.ApplicaitonSenderID, message.ApplicationRecipientID));

            this.Router.HandleMessage(message);
        }

        /// <summary>
        /// File handling interface called from the RemoteFileManager object
        /// Converts the passed path and attributes to an IPC message before
        /// dispatching to the recipient application
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="attributes"></param>
        public void HandleFileReceive(String filepath, Dictionary<String, String> attributes)
        {
            var message = new IPCMessage();
            message.ApplicaitonSenderID = "fileshare";  // TODO(hc): Change this to the actual sender
            message.ApplicationRecipientID = "fileinbox";
            message.Target = IPCMessage.MessageTarget.Local;
            message.Content["filepath"] = filepath;
            message.Type = "sendFile";

            foreach (var pair in attributes)
            {
                message.Content[pair.Key] = pair.Value;
            }

            this.Router.HandleMessage(message);
        }

        public void HandleControlMessageReceive(FleetMessage message) {
            // todo(hc): implement handler
            var ipcMessage = new IPCMessage();
            ipcMessage.ApplicationRecipientID = message.Application;
            ipcMessage.Content["message"] = message.Message;
            ipcMessage.Content["sent"] = message.Sent.ToString();
            ipcMessage.Type = "control";

            if (message.Application == "daemon") {
                ipcMessage.Target = IPCMessage.MessageTarget.Daemon;
            } else {
                ipcMessage.Target = IPCMessage.MessageTarget.Local;
            }

            this.Router.HandleMessage(ipcMessage);
        }

        public void Run()
        {
            // Service initialisation
            StartService();

            // Start heartbeat
            StartHeartbeat();

            // App Hauler
            //InitialiseAppHauler();

            // Platform Services
            //StartPlatformServices();

            // Daemon is running
            Console.WriteLine("Daemon running. Press the any key to exit.");
            Console.ReadLine();
            this.Service.Close();

            //HeartbeatManager.Instance.StopHeartbeat();

            foreach (var app in AppHauler.Instance.RunningApplications.Values)
            {
                app.Process.Kill();
            }
        }

        private void StartService()
        {
            var address = new Uri("net.pipe://localhost/fleetdaemon");
            var binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);
            this.Service = new ServiceHost(typeof(DaemonService));
            this.Service.AddServiceEndpoint(typeof(IDaemonIPC), binding, address);
            this.Service.Open();
            
            Console.WriteLine("Daemon IPC service listening");
        }

        private void StartHeartbeat()
        {
            RemoteFileManager.DaemonInstance = this;
            HeartbeatManager.WaitLength = 3000;
            HeartbeatManager.Instance.Token = this.ClientToken;
            HeartbeatManager.Instance.StartHeartbeat();

            Console.WriteLine("Heartbeat is running");
        }

        private void StartPlatformServices()
        {
            Console.WriteLine(Directory.GetCurrentDirectory());

            // Start Workstation Selector & Accept Dialog
            Process.Start("services.bat");

            // Start Dock
            AppHauler.Instance.LaunchApplication("fleetshelf");
        }
    }
}