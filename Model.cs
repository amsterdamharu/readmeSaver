using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace readmeApp
{
    interface ITask
    {
        string error { get; set; }
        string taskDetails { get; set; }
        ITask[] TaskItems { get; set; }
        string basePath { get; set; }
    }


    class StatusUpdate
    {
        public int processed {get;set;}
        public int errors { get; set; }
        public int total { get; set; }
    }

    class ReadMeObject : ITask
    {
        public string readmeStringContent { get; set; }
        public string htmlStringContent { get; set; }
        public string error { get; set; }
        public string url { get; set; }
        public string fileName { get; set; }
        public string taskDetails { get; set; }
        public string basePath { get; set; }
        public System.Xml.XmlDocument xmlDocument { get; set; }
        public ITask[] TaskItems { get; set; }
    }
    class ImageObject : ITask
    {
        public string url { get; set; }
        public string newUrl { get; set; }
        public string filePath { get; set; }
        public string fileName {get;set;}
        public string error { get; set; }
        public string taskDetails { get; set; }
        public ITask[] TaskItems { get; set; }
        public string basePath { get; set; }
    }
    class Model : ITask
    {
        public ITask[] TaskItems { get; set; }
        public int activeTasks = 20;
        public int activeConnections = 10;
        public int cooldownTime = 1000;
        public string basePath {get; set;}
        public string error { get; set; }
        public string taskDetails { get; set; }
        public StatusUpdate status;
        public Func<StatusUpdate, StatusUpdate> statusUpdater { get; set; }

    }
}
