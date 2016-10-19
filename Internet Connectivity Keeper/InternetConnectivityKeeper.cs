using System;
using System.Windows.Forms;
using System.Collections;
using System.Collections.Generic;
using NativeWifi;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Diagnostics;

namespace Internet_Connectivity_Keeper
{
    internal class InternetConnectivityKeeper : ApplicationContext
    {
        private const int k_ConnectTimeOut = 2500; // in ms
        private const int k_SleepAfterConnectTimeout = 1000;

        private int m_PingTimeout = 1200;
        private long m_PingResponseTimeout;
        private NotifyIcon m_NotifyIcon = new NotifyIcon();
        private WlanClient.WlanInterface m_Interface;
        private Thread m_Thread_IconChanger;

        public long PingResponseTimeout
        {
            get
            {
                Ping myPing = new Ping();
                String host = "8.8.8.8";
                byte[] buffer = new byte[32];
                int timeout = m_PingTimeout;
                PingOptions pingOptions = new PingOptions();
                PingReply reply = null;
                try
                {
                    reply = myPing.Send(host, timeout, buffer, pingOptions);
                }
                catch
                {
                    return 0;
                }

                if (reply != null)
                {
                    return reply.RoundtripTime;
                }

                return 0;
            }

            set
            {
                m_PingResponseTimeout = value;
            }
        }

        public InternetConnectivityKeeper()
        {
            m_Thread_IconChanger = new Thread(setIcon);
            setInterface();
            registerEvents();
            try
            {
                firstConnect();
                m_NotifyIcon.DoubleClick += m_NotifyIcon_MouseDoubleClick_Deactivate;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                registerEvents(i_ToRegister: false);
            }

            m_Thread_IconChanger.Start();
            m_NotifyIcon.MouseMove += M_NotifyIcon_MouseMove;
            initMenus();
            m_NotifyIcon.Visible = true;
        }

        private void M_NotifyIcon_MouseMove(object sender, MouseEventArgs e)
        {
            m_NotifyIcon.BalloonTipText = PingResponseTimeout.ToString();
            m_NotifyIcon.ShowBalloonTip(50000);
        }

        private void setIcon()
        {
            while (true)
            {
                if (checkInternetConnectivity())
                {
                    m_NotifyIcon.Icon = Properties.Resources.AppIconGreen;
                }
                else
                {
                    m_NotifyIcon.Icon = Properties.Resources.AppIconRed;
                } 
            }
        }

        private bool checkInternetConnectivity()
        {
            Ping myPing = new Ping();
            String host = "8.8.8.8";
            byte[] buffer = new byte[32];
            int timeout = m_PingTimeout;
            PingOptions pingOptions = new PingOptions();
            PingReply reply = null;
            try
            {
                reply = myPing.Send(host, timeout, buffer, pingOptions);
            }
            catch
            {
                return false;
            }

            if (reply != null)
            {
                return reply.Status == IPStatus.Success;
            }

            return false;
        }

        private void firstConnect()
        {
            Wlan.WlanAvailableNetwork? chosenNetwork = getChosenNetwork();

            if (chosenNetwork != null)
            {
                if (!checkInternetConnectivity())
                {
                    connect(chosenNetwork.Value.profileName);
                }
            }
            else
            {
                throw new Exception("No connectable networks!");
            }
        }

        private Wlan.WlanAvailableNetwork? getChosenNetwork()
        {
            Debug.log("First connect");
            uint maxSignal = 0;
            Wlan.WlanAvailableNetwork? chosenNetwork = null;
            var profiles = m_Interface.GetProfiles();

            var networks = m_Interface.GetAvailableNetworkList(Wlan.WlanGetAvailableNetworkFlags.IncludeAllAdhocProfiles);

            foreach (var network in networks)
            {
                if (network.networkConnectable && network.wlanSignalQuality > maxSignal/* && profileExists(network.profileName, profiles)*/)
                {
                    maxSignal = network.wlanSignalQuality;
                    chosenNetwork = network;
                }
            }

            return chosenNetwork;
        }

        private void registerEvents(bool i_ToRegister = true)
        {
            if (i_ToRegister)
            {
                m_Interface.WlanConnectionNotification += m_Interface_WlanConnectionNotification;
            }
            else
            {
                m_Interface.WlanConnectionNotification -= m_Interface_WlanConnectionNotification;
            }
        }

        private void m_NotifyIcon_MouseDoubleClick_Activate(object sender, EventArgs e)
        {
            registerEvents();
            firstConnect();
            try
            {
                m_Thread_IconChanger.Resume();
            }
            catch (Exception ex)
            {
                Debug.log($"Exception while trying to start setIcon thread. MSG: {ex.Message}");
            }
            m_NotifyIcon.DoubleClick -= m_NotifyIcon_MouseDoubleClick_Activate;
            m_NotifyIcon.DoubleClick += m_NotifyIcon_MouseDoubleClick_Deactivate;
        }

        private void m_NotifyIcon_MouseDoubleClick_Deactivate(object sender, EventArgs e)
        {
            registerEvents(i_ToRegister: false);
            m_Thread_IconChanger.Suspend();
            Debug.log("Thread aborted");
            m_NotifyIcon.Icon = Properties.Resources.AppIconGray;
            m_NotifyIcon.DoubleClick -= m_NotifyIcon_MouseDoubleClick_Deactivate;
            m_NotifyIcon.DoubleClick += m_NotifyIcon_MouseDoubleClick_Activate;
        }

        private void m_Interface_WlanConnectionNotification(Wlan.WlanNotificationData i_NotifyData, Wlan.WlanConnectionNotificationData i_ConnNotifyData)
        {
            Debug.log($"Connection status changed to {i_NotifyData.NotificationCode.ToString()}");

            //string[] goodNotificationCodes = { "ConnectionStart", "Associating", "Associated", "Authenticating", "Connected", "ConnectionComplete", "RoamingStart" };
            m_Interface.WlanConnectionNotification -= m_Interface_WlanConnectionNotification;
            if (!checkInternetConnectivity()/*!goodNotificationCodes.Any((i_NotifyData.NotificationCode.ToString()).Contains)*/) // i.e disconnected
            {
                connect(i_ConnNotifyData.profileName);
            }

            m_Interface.WlanConnectionNotification += m_Interface_WlanConnectionNotification;
        }

        private void connect(string i_ProfileName)
        {
            Debug.log($"Connecting to {i_ProfileName}");
            ExecuteCommandSync($"netsh wlan connect {i_ProfileName}");
            Thread.Sleep(k_SleepAfterConnectTimeout);
            //m_Interface.ConnectSynchronously(Wlan.WlanConnectionMode.Profile, Wlan.Dot11BssType.Any, i_ProfileName, connectTimeout: k_ConnectTimeOut);
            Debug.log("Connection complete");
        }

        private static string ExecuteCommandSync(object command)
        {
            string res;
            try
            {
                // create the ProcessStartInfo using "cmd" as the program to be run,
                // and "/c " as the parameters.
                // Incidentally, /c tells cmd that we want it to execute the command that follows,
                // and then exit.
                System.Diagnostics.ProcessStartInfo procStartInfo =
                    new System.Diagnostics.ProcessStartInfo("cmd", "/c " + command);

                // The following commands are needed to redirect the standard output.
                // This means that it will be redirected to the Process.StandardOutput StreamReader.
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.UseShellExecute = false;
                // Do not create the black window.
                procStartInfo.CreateNoWindow = true;
                // Now we create a process, assign its ProcessStartInfo and start it
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo = procStartInfo;
                proc.Start();
                // Get the output into a string
                res = proc.StandardOutput.ReadToEnd();
                // Display the command output.
                Console.WriteLine(res);
                return res;
            }
            catch (Exception objException)
            {
                Debug.log(objException.Message);
            }
            return null;
        }

        private void setInterface()
        {
            WlanClient wlanClient = new WlanClient();
            var interfaces = wlanClient.Interfaces;
            if (interfaces.Length != 1)
            {
                throw new Exception($"More than one interface({interfaces.Length})");
            }
            else
            {
                m_Interface = interfaces[0];
            }
        }

        private void initMenus()
        {
            /// V set ping timeout
            /// Reconnect
            /// Disconnect
            /// V Exit

            MenuItem mi_Reconnect = new MenuItem("Reconnect", new EventHandler(reconnect));
            MenuItem mi_PingTimeout = new MenuItem("Set Ping Timeout", new EventHandler(setPingTimeout));
            MenuItem mi_Exit = new MenuItem("Exit", new EventHandler(Exit));
            m_NotifyIcon.ContextMenu = new ContextMenu(new MenuItem[] { mi_PingTimeout, mi_Reconnect, mi_Exit });
        }

        private void reconnect(object sender, EventArgs e)
        {
            Wlan.WlanAvailableNetwork? chosenNetwork = getChosenNetwork();

            if (chosenNetwork != null)
            {
                connect(chosenNetwork.Value.profileName);
            }
        }


        private void Exit(object sender, EventArgs e)
        {
            Application.Exit();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            m_Thread_IconChanger.Abort();
            MessageBox.Show("Goodbye!", "Exit");
            m_NotifyIcon.Dispose();
        }

        private void setPingTimeout(object sender, EventArgs e)
        {
            m_PingTimeout = new Form_PingTimeout(m_PingTimeout).GetPingTimeout();
        }
    }
}