/*
    Copyright ?2002, The KPD-Team
    All rights reserved.
    http://www.mentalis.org/

  Redistribution and use in source and binary forms, with or without
  modification, are permitted provided that the following conditions
  are met:

    - Redistributions of source code must retain the above copyright
       notice, this list of conditions and the following disclaimer. 

    - Neither the name of the KPD-Team, nor the names of its contributors
       may be used to endorse or promote products derived from this
       software without specific prior written permission. 

  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
  "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
  LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS
  FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL
  THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT,
  INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
  (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
  SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
  HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
  STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
  ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED
  OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Collections;
using System.Net.Sockets;
using System.Security.Cryptography;
using Org.Mentalis.Proxy;
using Org.Mentalis.Proxy.Http;
using Org.Mentalis.Proxy.Socks;
using Org.Mentalis.Proxy.PortMap;
using Org.Mentalis.Proxy.Socks.Authentication;
using Org.Mentalis.Utilities.ConsoleAttributes;
using Proxy.IProxy;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Org.Mentalis.Proxy
{
    /// <summary>
    /// Represents an item in a Listeners collection.
    /// </summary>
    public struct ListenEntry
    {
        /// <summary>
        /// The Listener object.
        /// </summary>
        public Listener listener;
        /// <summary>
        /// The Listener's ID. It must be unique troughout the Listeners collection.
        /// </summary>
        public Guid guid;
        /// <summary>
        /// Determines whether the specified Object is equal to the current Object.
        /// </summary>
        /// <param name="obj">The Object to compare with the current Object.</param>
        /// <returns>True if the specified Object is equal to the current Object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return ((ListenEntry)obj).guid.Equals(guid);
        }
    }

    /// <summary>
    /// Defines the class that controls the settings and listener objects.
    /// </summary>
    public class Proxy : IProxy
    {
        private DateTime beginTime;
        private string ip;
        private int port;
        private int port2;
        private object syncRoot = new object();
        /// <summary>
        /// Initializes a new Proxy instance.
        /// </summary>
        /// <param name="file">The XML configuration file to use.</param>
        public Proxy(string file)
        {
            Config = new ProxyConfig(this, file);
            if (File.Exists(Config.File))
                Config.LoadData();
        }

        public void Initial()
        {
            Console.WriteLine("Initial...");
        }

        public void Run()
        {
            Thread thread = new Thread(() =>
              {
                  while (true)
                  {
                      if (!CheckConnection())
                      {
                          while (true)
                          {
                              if (Restart())
                              {
                                  break;
                              }
                              else
                              {
                                  Thread.Sleep(3000);
                              }
                          }
                      }
                      else
                      {
                          if ((DateTime.Now - beginTime).TotalMinutes > 20)
                          {
                              Restart();
                          }
                      }
                      Thread.Sleep(3000);
                  }
              });
            thread.Start();
        }

        public bool Start()
        {
            try
            {
                if (!CheckConnection())
                {
                    return false;
                }
                string newIp = GetIp();
                Random random = new Random((int)DateTime.Now.Ticks);
                int newPort = random.Next(10000, 65536);
                int newPort2 = random.Next(10000, 65536);
                Start(newIp, newPort, ProxyType.Http);
                Start(newIp, newPort2, ProxyType.Socks);
                string[] rdStrs = GetRandomStrs(4, 2);
                AddUser(rdStrs[0], rdStrs[1]);
                ip = newIp;
                port = newPort;
                port2 = newPort2;
                beginTime = DateTime.Now;
                Console.WriteLine("IP:" + ip);
                Console.WriteLine("User:{0} Pass:{1}", rdStrs[0], rdStrs[1]);
                Console.WriteLine(Submit(ip, port, port2, rdStrs[0], rdStrs[1]));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool Restart()
        {
            try
            {
                if (ip != null)
                {
                    Stop();
                    Delete(ip, port);
                    Delete(ip, port2);
                    ip = null;
                }
            }
            catch
            {
                return false;
            }

            if (!Start())
            {
                return false;
            }
            else
            {
                return true;
            }
        }


        private void Delete(string ip, int port)
        {
            var request = WebRequest.Create(string.Format("http://118.193.131.17/service/Delete?ip={0}&port={1}", ip, port));
            request.GetResponse();
        }

        public string Submit(string ip, int port, int port2, string name = "", string pwd = "")
        {
            var request = WebRequest.Create(string.Format("http://118.193.131.17/service/Submit?ip={0}&port={1}&port2={2}&name={3}&pwd={4}", ip, port, port2, name, pwd));
            using (var stream = request.GetResponse().GetResponseStream())
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
        public static string GetIp()
        {
            string tempip = "127.0.0.1";
            try
            {
                WebRequest wr = WebRequest.Create("http://118.193.131.17/Service/GetIp");
                Stream s = wr.GetResponse().GetResponseStream();
                StreamReader sr = new StreamReader(s, Encoding.Default);
                tempip = sr.ReadToEnd();
                sr.Close();
                s.Close();
            }
            catch
            {
            }
            return tempip;
        }
        public static string[] GetRandomStrs(int length, int count)
        {

            string[] strs = new string[count];
            Random rd = new Random((int)DateTime.Now.Ticks);
            for (int j = 0; j < count; j++)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < length; i++)
                {
                    int num = rd.Next(97, 123);
                    sb.Append((char)num);
                }
                strs[j] = sb.ToString();
            }

            return strs;
        }

        /// <summary>
        /// Starts a new Proxy server by reading the data from the configuration file and start listening on the specified ports.
        /// </summary>
        public void Start(string ip, int port, ProxyType type)
        {
            // Initialize some objects
            StartTime = DateTime.Now;
            // Start the proxy
            Listener listener = null;
            switch (type)
            {
                case ProxyType.Http:
                    listener = CreateListener("Org.Mentalis.Proxy.Http.HttpListener", string.Format("host:{0};int:{1}", ip, port));
                    break;
                case ProxyType.Socks:
                    listener = CreateListener("Org.Mentalis.Proxy.Socks.SocksListener", string.Format("host:{0};int:{1};authlist", ip, port));
                    break;
            }
            if (listener != null)
            {
                listener.Start();
                AddListener(listener);
            }
            else
            {
                throw new Exception("无法在指定端口监听！");
            }
        }

        public void AddUser(string name, string pwd)
        {
            if (Config.UserList.IsUserPresent(name))
            {
                Config.UserList.RemoveItem(name);
            }
            Config.UserList.AddItem(name, pwd);
        }
        public void Menu()
        {
            Console.WriteLine("\r\n  Proxy\r\n  ~~~~~~~~~~~~~~~~~~\r\n\r\n (type 'help' for the command list)");
            Console.Write("\r\n>");
            string command = Console.ReadLine().ToLower();
            while (!command.Equals("exit"))
            {
                switch (command)
                {
                    case "help":
                        ShowHelp();
                        break;
                    case "uptime":
                        ShowUpTime();
                        break;
                    case "version":
                        ShowVersion();
                        break;
                    case "adduser":
                        ShowAddUser();
                        break;
                    case "deluser":
                        ShowDelUser();
                        break;
                    case "listusers":
                        ShowUsers();
                        break;
                    case "addlistener":
                        ShowAddListener();
                        break;
                    case "listlisteners":
                        ShowListeners();
                        break;
                    case "dellistener":
                        ShowDelListener();
                        break;
                    default:
                        Console.WriteLine("Command not understood.");
                        break;
                }
                Console.Write("\r\n>");
                command = Console.ReadLine().ToLower();
            }
            Stop();
            Console.WriteLine("Goodbye...");
        }
        /// <summary>
        /// Asks the user which listener to delete.
        /// </summary>
        protected void ShowDelListener()
        {
            Console.WriteLine("Please enter the ID of the listener you want to delete:\r\n (use the 'listlisteners' command to show all the listener IDs)");
            string id = Console.ReadLine();
            if (id != "")
            {
                try
                {
                    ListenEntry le = new ListenEntry();
                    le.guid = new Guid(id);
                    if (!Listeners.Contains(le))
                    {
                        Console.WriteLine("Specified ID not found in list!");
                        return;
                    }
                    else
                    {
                        this[Listeners.IndexOf(le)].Dispose();
                        Listeners.Remove(le);
                        Config.SaveData();
                    }
                }
                catch
                {
                    Console.WriteLine("Invalid ID tag!");
                    return;
                }
                Console.WriteLine("Listener removed from the list.");
            }
        }
        /// <summary>
        /// Shows the Listeners list.
        /// </summary>
        protected void ShowListeners()
        {
            for (int i = 0; i < Listeners.Count; i++)
            {
                Console.WriteLine(((ListenEntry)Listeners[i]).listener.ToString());
                Console.WriteLine("  id: " + ((ListenEntry)Listeners[i]).guid.ToString("N"));
            }
        }
        /// <summary>
        /// Asks the user which listener to add.
        /// </summary>
        protected void ShowAddListener()
        {
            Console.WriteLine("Please enter the full class name of the Listener object you're trying to add:\r\n (ie. Org.Mentalis.Proxy.Http.HttpListener)");
            string classtype = Console.ReadLine();
            if (classtype == "")
                return;
            else if (Type.GetType(classtype) == null)
            {
                Console.WriteLine("The specified class does not exist!");
                return;
            }
            Console.WriteLine("Please enter the construction parameters:");
            string construct = Console.ReadLine();
            object listenObject = CreateListener(classtype, construct);
            if (listenObject == null)
            {
                Console.WriteLine("Invalid construction string.");
                return;
            }
            Listener listener;
            try
            {
                listener = (Listener)listenObject;
            }
            catch
            {
                Console.WriteLine("The specified object is not a valid Listener object.");
                return;
            }
            try
            {
                listener.Start();
                AddListener(listener);
            }
            catch
            {
                Console.WriteLine("Error while staring the Listener.\r\n(Perhaps the specified port is already in use?)");
                return;
            }
            Config.SaveData();
        }
        /// <summary>
        /// Shows a list of commands in the console.
        /// </summary>
        protected void ShowHelp()
        {
            Console.WriteLine("          help - Shows this help message\r\n        uptime - Shows the uptime of the proxy server\r\n       version - Prints the version of this program\r\n     listusers - Lists all users\r\n       adduser - Adds a user to the user list\r\n       deluser - Deletes a user from the user list\r\n listlisteners - Lists all the listeners\r\n   addlistener - Adds a new listener\r\n   dellistener - Deletes a listener\r\n\r\n Read the readme.txt file for more help.");
        }
        /// <summary>
        /// Shows the uptime of this proxy server.
        /// </summary>
        protected void ShowUpTime()
        {
            TimeSpan uptime = DateTime.Now.Subtract(StartTime);
            Console.WriteLine("Up " + uptime.ToString());
        }
        /// <summary>
        /// Shows the version number of this proxy server.
        /// </summary>
        protected void ShowVersion()
        {
            Console.WriteLine("This is version " + Assembly.GetCallingAssembly().GetName().Version.ToString(3) + " of the Mentalis.org proxy server.");
        }
        /// <summary>
        /// Asks the user which username to add.
        /// </summary>
        protected void ShowAddUser()
        {
            Console.Write("Please enter the username to add: ");
            string name = Console.ReadLine();
            if (Config.UserList.IsUserPresent(name))
            {
                Console.WriteLine("Username already exists in database.");
                return;
            }
            Console.Write("Please enter the password: ");
            ConsoleAttributes.EchoInput = false;
            string pass1 = Console.ReadLine();
            Console.Write("\r\nPlease enter the password again: ");
            string pass2 = Console.ReadLine();
            ConsoleAttributes.EchoInput = true;
            if (!pass1.Equals(pass2))
            {
                Console.WriteLine("\r\nThe passwords do not match.");
                return;
            }
            Config.SaveUserPass(name, pass1);
            Console.WriteLine("\r\nUser successfully added.");
        }
        /// <summary>
        /// Asks the user which username to delete.
        /// </summary>
        protected void ShowDelUser()
        {
            Console.Write("Please enter the username to remove: ");
            string name = Console.ReadLine();
            if (!Config.UserList.IsUserPresent(name))
            {
                Console.WriteLine("Username not present in database.");
                return;
            }
            Config.RemoveUser(name);
            Console.WriteLine("User '" + name + "' successfully removed.");
        }
        /// <summary>
        /// Shows a list of usernames in the console.
        /// </summary>
        protected void ShowUsers()
        {
            if (Config.UserList == null || Config.UserList.Keys.Length == 0)
            {
                Console.WriteLine("There are no users in the user list.");
            }
            else
            {
                Console.WriteLine("The following " + Config.UserList.Keys.Length.ToString() + " users are allowed to use the SOCKS5 proxy:");
                Console.WriteLine(string.Join(", ", Config.UserList.Keys));
            }
        }
        /// <summary>
        /// Stops the proxy server.
        /// </summary>
        /// <remarks>When this method is called, all listener and client objects will be disposed.</remarks>
        public void Stop()
        {
            // Stop listening and clear the Listener list
            for (int i = 0; i < ListenerCount; i++)
            {
                Console.WriteLine(this[i].ToString() + " stopped.");
                this[i].Dispose();
            }
            Listeners.Clear();
        }
        /// <summary>
        /// Adds a listener to the Listeners list.
        /// </summary>
        /// <param name="newItem">The new Listener to add.</param>
        public void AddListener(Listener newItem)
        {
            if (newItem == null)
                throw new ArgumentNullException();
            ListenEntry le = new ListenEntry();
            le.listener = newItem;
            le.guid = Guid.NewGuid();
            while (Listeners.Contains(le))
            {
                le.guid = Guid.NewGuid();
            }
            Listeners.Add(le);
            Console.WriteLine(newItem.ToString() + " started.");
        }
        /// <summary>
        /// Creates a new Listener obejct from a given listener name and a given listener parameter string.
        /// </summary>
        /// <param name="type">The type of object to instantiate.</param>
        /// <param name="cpars"></param>
        /// <returns></returns>
        public Listener CreateListener(string type, string cpars)
        {
            try
            {
                string[] parts = cpars.Split(';');
                object[] pars = new object[parts.Length];
                string oval = null, otype = null;
                int ret;
                // Start instantiating the objects to give to the constructor
                for (int i = 0; i < parts.Length; i++)
                {
                    ret = parts[i].IndexOf(':');
                    if (ret >= 0)
                    {
                        otype = parts[i].Substring(0, ret);
                        oval = parts[i].Substring(ret + 1);
                    }
                    else
                    {
                        otype = parts[i];
                    }
                    switch (otype.ToLower())
                    {
                        case "int":
                            pars[i] = int.Parse(oval);
                            break;
                        case "host":
                            pars[i] = Dns.Resolve(oval).AddressList[0];
                            break;
                        case "authlist":
                            pars[i] = Config.UserList;
                            break;
                        case "null":
                            pars[i] = null;
                            break;
                        case "string":
                            pars[i] = oval;
                            break;
                        case "ip":
                            pars[i] = IPAddress.Parse(oval);
                            break;
                        default:
                            pars[i] = null;
                            break;
                    }
                }
                return (Listener)Activator.CreateInstance(Type.GetType(type), pars);
            }
            catch
            {
                return null;
            }
        }
        /// <summary>
        /// Gets the collection that contains all the Listener objects.
        /// </summary>
        /// <value>An ArrayList object that contains all the Listener objects.</value>
        protected ArrayList Listeners
        {
            get
            {
                return m_Listeners;
            }
        }
        /// <summary>
        /// Gets the number of Listener objects.
        /// </summary>
        /// <value>An integer specifying the number of Listener objects.</value>
        internal int ListenerCount
        {
            get
            {
                return Listeners.Count;
            }
        }
        /// <summary>
        /// Gets the Listener object at the specified position.
        /// </summary>
        /// <value>The Listener instance at position <c>index</c>.</value>
        internal virtual Listener this[int index]
        {
            get
            {
                return ((ListenEntry)Listeners[index]).listener;
            }
        }
        /// <summary>
        /// Gets or sets the date when this Proxy server was first started.
        /// </summary>
        /// <value>A DateTime structure that indicates when this Proxy server was first started.</value>
        protected DateTime StartTime
        {
            get
            {
                return m_StartTime;
            }
            set
            {
                m_StartTime = value;
            }
        }
        /// <summary>
        /// Gets or sets the configuration object for this Proxy server.
        /// </summary>
        /// <value>A ProxyConfig instance that represents the configuration object for this Proxy server.</value>
        protected ProxyConfig Config
        {
            get
            {
                return m_Config;
            }
            set
            {
                m_Config = value;
            }
        }
        // private variables
        /// <summary>Holds the value of the StartTime property.</summary>
        private DateTime m_StartTime;
        /// <summary>Holds the value of the Config property.</summary>
        private ProxyConfig m_Config;
        /// <summary>Holds the value of the Listeners property.</summary>
        private ArrayList m_Listeners = new ArrayList();

        [DllImport("winInet.dll ")]
        private static extern bool InternetCheckConnection(string url, int dwFlag, int dwReserved);
        private bool CheckConnection()
        {
            return InternetCheckConnection("http://www.baidu.com", 0x00000001, 0);
        }
    }

}
