using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace readmeApp
{
    interface IHasTasks
    {
        ITask[] TaskItems { get; set; }
    }
    interface ITask
    {
        string error { get; set; }
        string taskDetails { get; set; }
    }


    class ReadMeObject : IHasTasks, ITask
    {
        public string readmeStringContent { get; set; }
        public string htmlStringContent { get; set; }
        public string error { get; set; }
        public string url { get; set; }
        public string fileName { get; set; }
        public string taskDetails { get; set; }
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
    }
    class Model : IHasTasks
    {
        public ITask[] TaskItems { get; set; }
        public int activeConnections = 2;
        public int cooldownTime = 1000;


        //paths
        #region
        public static string paths = @"http://localhost:8888/documentation/tmpreadme/1.txt
http://localhost:8888/documentation/tmpreadme/2.txt
http://localhost:8888/documentation/tmpreadme/3.txt"; //https://raw.githubusercontent.com/amsterdamharu/redux-middleware/master/basic/README.md
        #endregion

        //creating a test ReadMeObject[]
        #region
        public static ReadMeObject[] rmObjects = ((Func<ReadMeObject[]>)(() =>
        {
            string[] why = Model.paths.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            return Model.paths.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None)
                .Select((path)=>{
                    ReadMeObject r = new ReadMeObject();
                    r.url = path;
                    return r;
                })
                .ToArray();
        }))();


        
        #endregion
    }
}
