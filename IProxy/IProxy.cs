using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Proxy.IProxy
{
    public interface IProxy
    {
        void AddUser(string name, string pwd);
        void Initial();
        void Menu();
        void Run();
        void Start(string ip, int port, ProxyType type);
        void Stop();
    }
    public enum ProxyType
    {
        Http = 0,
        Socks = 1,
    }
}
