using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Reflection;
using Proxy.IProxy;

namespace ProxyConsole
{
    class Program
    {

        static void Main(string[] args)
        {
            try
            {
                Proxy.Updater.Updater.Update();
                Assembly ass = Assembly.Load("Mentalis"); 
                Type tp = ass.GetType("Org.Mentalis.Proxy.Proxy"); 
                string dir = Environment.CurrentDirectory;
                if (!dir.Substring(dir.Length - 1, 1).Equals(@"\"))
                    dir += @"\";
                object obj = Activator.CreateInstance(tp, dir + "config.xml");
                var prx = (IProxy)obj;
                prx.Run();
                prx.Menu();
            }
            catch (Exception ex)
            {
                Console.WriteLine("The program ended abnormally!" + ex.Message);
                Console.ReadLine();
            }
        }


    }
}
