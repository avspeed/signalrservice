using System;
using System.Collections.Generic;
using System.Threading;
using System.Configuration;
using Microsoft.AspNet.SignalR;
using System.Threading.Tasks;
using Owin;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.Cors;
using System.Linq;
using RTCSignalingService.Models;
using RTCSignalingService.Logging;
using System.Diagnostics;

namespace RTCSignalingService
{

    public class WebRtcHub : Hub
    {
        private static readonly List<User> Users = new List<User>();

        public  async Task JoinConference( string conferenceId, string sessionId )
        {
            if (conferenceId.Trim() == String.Empty)
            {
                conferenceId = Guid.NewGuid().ToString();
            }

            await Groups.Add(Context.ConnectionId, conferenceId);

            var usr = GetUser(Context.ConnectionId);

            if (usr != null)
            {
                usr.Channel = conferenceId;
            }

            Clients.Group(conferenceId).onJoinedConference(conferenceId, Context.ConnectionId, usr.Username);
            //send request for clients to broadcast thier session Id so that everyone can be connected
            Clients.Group(conferenceId).onSessionIdRequest(conferenceId);
        }

        public void SendMySessionID(string conferenceId, string sessionId)
        {
            //we are going to send our session id - but we are going to concatenate an identifier which is the connection id of the 
            //client that will be viewing our session
            string name = GetUser(Context.ConnectionId).Username;

            Clients.OthersInGroup(conferenceId).onSessionIdBroadcast(conferenceId, sessionId, name);
        }

        private User GetUser(string connectionId)
        {
            var usr = Users.FindAll(u => u.ConnectionId == Context.ConnectionId);

            if (usr.Any())
            {
                return usr.First();
            }
            return null;
        }

        public async Task LeaveConference(string conferenceId)
        {
            await Groups.Remove(Context.ConnectionId, conferenceId);

            var usr = GetUser(Context.ConnectionId);
            Clients.OthersInGroup(conferenceId).onPeerLeftConference(conferenceId, Context.ConnectionId, usr.Username);
            Clients.Caller.onSelfLeftConference(conferenceId, Context.ConnectionId, usr.Username);
        }

        public void Send(string message)
        {
            Core.LogEvent("Pass message = " + message, Logger.logType.info);
            Clients.All.onMessageReceived(message);
        }

        public void SendDataToConference(string message, string conferenceId)
        {
            var usr = GetUser(Context.ConnectionId);
            Clients.OthersInGroup(conferenceId).onConferenceMessageReceived(message, usr.Username, Context.ConnectionId);
        }

        public async Task Join(string username, string channel)
        {
            // Add the new user
            Users.Add(new User
            {
                Username = username,
                ConnectionId = Context.ConnectionId,
                Channel = channel
            });

            await Groups.Add(Context.ConnectionId, channel);
            
        }

        
        private string GetChannelForUser( string conectionId )
        {
            string channel = "";

            var usr = Users.FindAll(u => u.ConnectionId == Context.ConnectionId);

            return channel;
        }

        public override Task OnConnected()
        {
            string name = Context.QueryString["userName"];

            Users.Add(new User
            {
                Username = name,
                ConnectionId = Context.ConnectionId
            });

            
            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            string name = Context.QueryString["username"];

            Users.RemoveAll(u => u.ConnectionId == Context.ConnectionId);

            return base.OnDisconnected(stopCalled);
        }
    }

    public class Startup
    {

        static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private static IDisposable _runningInstance;
        private static int listeningPort;
        // Your startup logic
        public static void StartServer(int port)
        {
            listeningPort = port;
            var cancellationTokenSource = new CancellationTokenSource();
            Task.Factory.StartNew(RunSignalRServer, TaskCreationOptions.LongRunning
                                  , cancellationTokenSource.Token);
        }

        private static void RunSignalRServer(object task)
        {
            bool isLocal = ConfigurationManager.AppSettings["localhost"] == null ? false : (ConfigurationManager.AppSettings["localhost"].ToString() == "true" ? true : false);
            string protocol = ConfigurationManager.AppSettings["ssl"] == null ? "http" : (ConfigurationManager.AppSettings["ssl"].ToString() == "true" ? "https" : "http");
            string url = protocol + @"://" + (isLocal ? "localhost" : "*") + ":" + listeningPort + @"/";
            _runningInstance = WebApp.Start(url);

            if (_runningInstance == null)
                Core.LogEvent("Unable to start SignalR server. check and make sure that your service is running under an account that has sufficient permissions.", Logger.logType.error);
            else Core.LogEvent("SignalR Running...", Logger.logType.info);
        }

        public static void StopServer()
        {
            _cancellationTokenSource.Cancel();

            if (_runningInstance != null)
                _runningInstance.Dispose();
        }

        // This code configures Web API. The Startup class is specified as a type
        // parameter in the WebApp.Start method.
        public void Configuration(IAppBuilder app)
        {
            app.Map("/signalr", map =>
            {

                map.UseCors(CorsOptions.AllowAll);
                var hubConfiguration = new HubConfiguration
                {
                    EnableJSONP = true
                };

                hubConfiguration.EnableDetailedErrors = true;
                map.RunSignalR(hubConfiguration);
            });
        }
    }


    public class Core
    {
        public Core()
        {
            _context = GlobalHost.ConnectionManager.GetHubContext<WebRtcHub>();
        }



        #region vars
        private static readonly TimeSpan _disconnectThreshold = TimeSpan.FromSeconds(10);
        static Logger logger;


        public static IHubContext _context;

        #endregion

        public bool StartIt()
        {

            try
            {
                
                LogEvent("Starting AVSPEED SignalR Server v." + System.Reflection.Assembly.GetExecutingAssembly().
                                                            GetName().Version.ToString(), Logger.logType.info);

                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

                int listeningPort = Convert.ToInt32(ConfigurationManager.AppSettings["TCPCentralListeningPort"]);

                Startup.StartServer(listeningPort);

                return true;
            }
            catch (Exception ex)
            {
                LogEvent("Unable to start Server " + ex.Message, Logger.logType.error);
                //todo this.OnStop();
            }

            return false;
        }

        public void StopIt()
        {
            Startup.StopServer();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogEvent("Unhandled exception " + ((Exception)e.ExceptionObject).Message + "-- Target -- " + ((Exception)e.ExceptionObject).TargetSite.ToString(), Logger.logType.error);
        }


        #region Log
        public static void LogEvent(string eventToLog, Logger.logType type)
        {
            try
            {
                if (Convert.ToBoolean(ConfigurationManager.AppSettings["EnableLog"]))
                {
                    logger.WriteToEventLog(eventToLog, type);
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("AVSPEED Signaling Service", "Unable to log event through standard logger " + ex.Message + "\n" + eventToLog, EventLogEntryType.Error);
            }
            
        }
        #endregion

    }
}

        