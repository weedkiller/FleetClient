﻿using FileAcceptIPC;
using FleetIPC;
using FleetServer;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace FileAccept
{
    public partial class App : System.Windows.Application
    {
        private ServiceHost service;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var address = new Uri("net.pipe://localhost/fileaccept");
            var binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);
            this.service = new ServiceHost(typeof(FileAcceptService));
            this.service.AddServiceEndpoint(typeof(IFileAcceptIPC), binding, address);
            this.service.Open();

            Console.WriteLine("Service started");
        }
    }

    public class FileAcceptService : IFileAcceptIPC
    {
        public Boolean RequestAcceptFile(FleetFileIdentifier ident)
        {
            var window = new MainWindow();
            window.ShowRequestDialog(ident);
            return window.DidAccept;
        }
    }
}
