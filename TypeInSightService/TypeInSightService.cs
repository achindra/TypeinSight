using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Runtime.InteropServices;
using System.IO;
using System.Reflection;

namespace TypeInSightService
{
    public partial class TypeInSightService : ServiceBase
    {
        #region Declares
        public enum ServiceState
        {
            SERVICE_STOPPED = 0x00000001,
            SERVICE_START_PENDING = 0x00000002,
            SERVICE_STOP_PENDING = 0x00000003,
            SERVICE_RUNNING = 0x00000004,
            SERVICE_CONTINUE_PENDING = 0x00000005,
            SERVICE_PAUSE_PENDING = 0x00000006,
            SERVICE_PAUSED = 0x00000007,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ServiceStatus
        {
            public int dwServiceType;
            public ServiceState dwCurrentState;
            public int dwControlsAccepted;
            public int dwWin32ExitCode;
            public int dwServiceSpecificExitCode;
            public int dwCheckPoint;
            public int dwWaitHint;
        };

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(IntPtr handle, ref ServiceStatus serviceStatus);
        
        #endregion

        #region Service
        public TypeInSightService()
        {
            InitializeComponent();

            //Set service status PENDING
            ServiceStatus serviceStatus = new ServiceStatus
            {
                dwCurrentState = ServiceState.SERVICE_START_PENDING,
                dwWaitHint = 100000
            };
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            //Setup event log
            TISEventLog = new System.Diagnostics.EventLog();
            if (!System.Diagnostics.EventLog.SourceExists("TypeInSight"))
            {
                System.Diagnostics.EventLog.CreateEventSource(
                    "TypeInSight", "TypeInSightLog");
            }
            TISEventLog.Source = "TypeInSight";
            TISEventLog.Log = "TypeInSightLog";
            
            //Set service status RUNNING
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        protected override void OnStart(string[] args)
        {
            TISEventLog.WriteEntry("Service Start");
        }

        protected override void OnStop()
        {
            TISEventLog.WriteEntry("Service Stop");
        }

        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            TISEventLog.WriteEntry(changeDescription.ToString());
            switch (changeDescription.Reason)
            {
                case SessionChangeReason.ConsoleConnect:
                case SessionChangeReason.RemoteConnect:
                case SessionChangeReason.SessionUnlock:
                case SessionChangeReason.SessionLogon:                    
                    try
                    {
                        Process[] pname = Process.GetProcessesByName("typeinsight");
                        if (pname.Length == 0)
                        {
                            if (!ProcessExtension.StartProcessAsCurrentUser(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).ToString() + "\\TypeInSight.exe"))
                                TISEventLog.WriteEntry("Failed to launch TypeInSight in Session " + changeDescription.SessionId);
                            else
                                TISEventLog.WriteEntry("Process launched in Session " + changeDescription.SessionId);
                        }
                        else
                        {
                            TISEventLog.WriteEntry(pname.ToString());
                        }
                    }
                    catch(Exception e)
                    {
                        TISEventLog.WriteEntry(e.Message);
                    }
                    break;
            }
        }

        #endregion

    }
}
