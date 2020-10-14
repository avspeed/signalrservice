using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using System.Configuration;
using System.Xml;
using Stream = System.IO.Stream;
using Microsoft.Owin;
using Microsoft.AspNet.SignalR;
using System.Threading.Tasks;
using Owin;

using RTCSignalingService;

[assembly: OwinStartup(typeof(RTCSignalingService.Startup))]
namespace RTCSignalingService
{

    public partial class Main : ServiceBase
    {

        private Core core;

        public Main()
        {
            InitializeComponent();

            core = new Core();
        }

        

     
        protected override void OnStart(string[] args)
        {
            if (!core.StartIt())
            {
                this.OnStop();
            }

        }

        protected override void OnStop()
        {
            core.StopIt();
        }

    }


}