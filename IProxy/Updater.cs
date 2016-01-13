using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Proxy.Updater
{
    public class Updater
    {

        public static void Update()
        {
            try
            {
                CheckAndUpdate();
            }
            catch (Exception e)
            {
                Console.WriteLine("更新失败：" + e.Message);
            }

        }


        public static Dictionary<string, string> GetUpdateList()
        {
            Dictionary<string, string> fileDic = new Dictionary<string, string>();
            string path = "http://118.193.131.17/Download/";
            var request = WebRequest.Create(path + "update.txt");
            var stream = request.GetResponse().GetResponseStream();
            using (StreamReader reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    string record = reader.ReadLine();
                    string[] strs = record.Split(new char[] { ':' });
                    if (strs.Length < 2)
                    {
                        continue;
                    }
                    fileDic.Add(strs[0].Trim(), strs[1].Trim());
                }
            }
            stream.Close();
            return fileDic;
        }

        public static void CheckAndUpdate()
        {
            var netPath = "http://118.193.131.17/Download/";
            string localPath = AppDomain.CurrentDomain.BaseDirectory;
            var request = WebRequest.Create(netPath + "update.txt");
            var list = new List<string>();
            string netVersion;
            using (var netStream = request.GetResponse().GetResponseStream())
            {
                using (StreamReader reader = new StreamReader(netStream))
                {
                    netVersion = reader.ReadLine();

                    while (!reader.EndOfStream)
                    {
                        list.Add(reader.ReadLine());
                    }
                }
            }
            if (File.Exists(localPath+"update.txt"))
            {
                string version;
                using (var stream = File.OpenRead(localPath+"update.txt"))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        version = reader.ReadLine();
                    }
                }
                if(version==netVersion)
                {
                    return;
                }
                else
                {
                    Console.WriteLine("版本不一致，需要更新!");
                    foreach (var item in list)
                    {
                        Console.WriteLine(item+" 下载中...");
                        DownLoadFile(netPath, localPath, item);
                        Console.WriteLine(item + "下载完毕！");
                    }
                }
                DownLoadFile(netPath, localPath, "update.txt");

            }
            else
            {
                Console.WriteLine("版本不一致，需要更新!");
                foreach (var item in list)
                {
                    Console.WriteLine(item + " 下载中...");
                    DownLoadFile(netPath, localPath, item);
                    Console.WriteLine(item + "下载完毕！");
                }
                DownLoadFile(netPath, localPath, "update.txt");
            }
        }
        public static void DownLoadFile(string netPath, string localPath, string fileName)
        {
            var request = WebRequest.Create(netPath + fileName);
            using (var stream = request.GetResponse().GetResponseStream())
            {
    
                using (var localStream = File.Create(localPath + fileName))
                {
                   byte[] buffer = new byte[1024*1024*10];
                    var count = stream.Read(buffer, 0, buffer.Length);
                    while(count>0)
                    {
                        localStream.Write(buffer, 0, count);
                        count = stream.Read(buffer, 0, buffer.Length);
                    }
                }
            }
        }
    }
}
