using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using Proxy.IProxy;
using System.Reflection;

namespace ProxyService
{
    public partial class ProxyService : ServiceBase
    {
        public static IProxy prx;
        public ProxyService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Proxy.Updater.Updater.Update();
            Assembly ass = Assembly.Load("Mentalis");
            Type tp = ass.GetType("Org.Mentalis.Proxy.Proxy");
            string dir = Environment.CurrentDirectory;
            if (!dir.Substring(dir.Length - 1, 1).Equals(@"\"))
                dir += @"\";
            object obj = Activator.CreateInstance(tp, dir + "config.xml");
            prx = (IProxy)obj;
            prx.Run();
        }

        protected override void OnStop()
        {
            prx.Stop();
        }
    }
}
