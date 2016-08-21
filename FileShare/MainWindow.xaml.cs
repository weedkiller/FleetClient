﻿using FleetIPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Interop;
using System.Reflection;

namespace FileShare
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            String[] droppedFiles = null;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                droppedFiles = e.Data.GetData(DataFormats.FileDrop, true) as String[];
            }

            if ((null == droppedFiles) || (!droppedFiles.Any())) { return; }

            var cAddress = new EndpointAddress("net.pipe://localhost/fleetdaemon");
            var cBinding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);
            var daemon = new FleetDaemonClient(cBinding, cAddress);

            
            var message = new IPCMessage();
            message.ApplicaitonSenderID = "FileShare";
            message.ApplicationRecipientID = "friendface";
            message.LocationHandle = IPCMessage.MessageLocationHandle.REMOTE;
            message.Type = "sendFile";
            message.Content["filePath"] = droppedFiles[0];

            daemon.Request(message);
            
        }
    }
}
