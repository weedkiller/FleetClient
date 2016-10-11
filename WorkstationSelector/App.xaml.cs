﻿/* 
 * Description: WorkstationSelector application
 *              Application initialisation logic for WorkstationSelector
 *              Starts IPC service + assignes handlers
 * Project: Fleet/FleetDaemon
 * Last modified: 11 October 2016
 * Last Author: Hayden Cheers
 * 
*/

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;
using FleetServer;
using System.Drawing;
using System.Windows.Forms;
using WorkstationSelectorIPC;

namespace WorkstationSelector
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private ServiceHost service;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Create and start IPC service
            var address = new Uri("net.pipe://localhost/workstationselector");
            var binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);
            this.service = new ServiceHost(typeof(WorkstationSelectService));
            this.service.AddServiceEndpoint(typeof(IWorkstationSelectIPC), binding, address);
            this.service.Open();
        }
    }

    public class WorkstationSelectService : IWorkstationSelectIPC
    {
        public List<FleetClientIdentifier> SelectWorkstations(List<FleetClientIdentifier> clients)
        {
           // Create window. Filter the clients based on user input. and reutrn the list.
            var window = new MainWindow();
            var selected = window.ShowSelectorDialog(clients);
            return selected;
        }
    }
}
