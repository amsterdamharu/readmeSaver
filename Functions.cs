using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Xml;
using System.Threading;
namespace readmeApp
{
    class Functions
    {
        public static Func<IHasTasks, IHasTasks> compose(Func<IHasTasks, IHasTasks> one, Func<IHasTasks, IHasTasks> two)
        {
            return x => two(one(x));
        }
        public static Func<IHasTasks, IHasTasks> compose(Func<IHasTasks, IHasTasks>[] functions)
        {
            return ((new Func<IHasTasks, IHasTasks>[] { (x) => x })).Concat(functions)
                .Aggregate((f, all) => Functions.compose(f, all));
        }
        public static Func<IHasTasks, IHasTasks> taco(Func<ITask, List<IHasTasks>, ITask> filling, Func<Func<ITask, List<IHasTasks>, ITask>, Func<IHasTasks, IHasTasks>> wrap)
        {
            return wrap(filling);
        }
        public static Func<ITask, List<IHasTasks>, ITask> multiWrap(Func<IHasTasks, IHasTasks> taco)
        {
            return (ITask task,List<IHasTasks> l)=>{
                taco(l.Last());
                return task;
            };
        }

        public static ReadMeObject setHtml(ITask o, List<IHasTasks> parentContext)
        {
            ReadMeObject readmeObject = (ReadMeObject)o;
            readmeObject.taskDetails = "Convert readme to html";
            readmeObject.htmlStringContent = CommonMark.CommonMarkConverter.Convert(readmeObject.readmeStringContent);
            return readmeObject;
        }

        public static ReadMeObject downLoadHtml(ITask o, List<IHasTasks> parentContext)
        {
            ReadMeObject readmeObject = (ReadMeObject)o;
            readmeObject.taskDetails = "Download:"+readmeObject.url;
            WebClient client = new WebClient();
            readmeObject.readmeStringContent = client.DownloadString(readmeObject.url);
            return readmeObject;
        }

        public static ReadMeObject setFileName(ITask o, List<IHasTasks> parentContext)
        {
            ReadMeObject readmeObject = (ReadMeObject)o;
            readmeObject.taskDetails = "Set file name";
            Uri url = new Uri(readmeObject.url);
            readmeObject.fileName = url.AbsolutePath.Replace("/","-");
            readmeObject.fileName = readmeObject.fileName.Substring(
                1
                , ((readmeObject.fileName.Length > 100) ? 100 : readmeObject.fileName.Length) - 1
            );
            readmeObject.fileName = url.Host + "-" + readmeObject.fileName + ".html";
            return readmeObject;
        }
        public static ReadMeObject saveHtmlStringToFile(ITask o, List<IHasTasks> parentContext)
        {
            ReadMeObject readmeObject = (ReadMeObject)o;
            readmeObject.taskDetails = "Save to:" + readmeObject.fileName;
            System.IO.File.WriteAllText(readmeObject.fileName, readmeObject.htmlStringContent);
            return readmeObject;
        }
        public static List<System.Xml.XmlNode> toList(System.Xml.XmlNodeList nodelist){
            return nodelist.Cast<System.Xml.XmlNode>().ToList();
        }
        public static ReadMeObject setXml(ITask o, List<IHasTasks> parentContext)
        {
            ReadMeObject readmeObject = (ReadMeObject)o;
            readmeObject.xmlDocument = new System.Xml.XmlDocument();
            readmeObject.xmlDocument.LoadXml("<body>"+readmeObject.htmlStringContent+"</body>");
            return readmeObject;
        }
        public static ReadMeObject createImageObjects(ITask o, List<IHasTasks> parentContext)
        {
            ReadMeObject readmeObject = (ReadMeObject)o;
            System.Xml.XmlNodeList images = readmeObject.xmlDocument.SelectNodes("//img");
            ImageObject[] l = Functions.toList(images)
                .Where((image) => image.Attributes != null)
                .Where((image) => image.Attributes["src"] != null)
                .Where((image) => image.Attributes["src"].Value != "")
                .Select((image) =>
                {
                    ImageObject io = new ImageObject();
                    io.url = image.Attributes["src"].Value;
                    return io;
                })
                .ToArray();
            readmeObject.TaskItems = l;
            return readmeObject;
        }
        public static ImageObject setImageFileNamesPathsAndNewUrl(ITask o, List<IHasTasks> parentContext)
        {
            ImageObject imageObject = (ImageObject)o;
            ReadMeObject readmeObject = (ReadMeObject)parentContext.Last();
            Uri url = new Uri(imageObject.url);
            string[] path = (url.Host + url.AbsolutePath)
                .Split(new char[] {'/'}).ToArray();
            imageObject.fileName = path.Last();
            imageObject.filePath = String.Join(
                new String(new char[] {Path.DirectorySeparatorChar}), path
            )
                .Replace("%20", "-")
                .Replace(":","-");
            imageObject.newUrl = readmeObject.fileName.Substring(0, readmeObject.fileName.Length - 5) + ".resource"
                + "/" 
                +imageObject.filePath.Replace(
                    Path.DirectorySeparatorChar
                    , '/'
                );
            return imageObject;
        }
        public static ImageObject createResourceRootDirectories(ITask o, List<IHasTasks> parentContext)
        {
            ImageObject imageObject = (ImageObject)o;
            ReadMeObject readmeObject = (ReadMeObject)parentContext.Last();
            new string[] { readmeObject.fileName.Substring(0, readmeObject.fileName.Length - 5) + ".resource" }
                .Concat(imageObject.filePath.Split(new char[] { Path.DirectorySeparatorChar }))
                .Aggregate((all, one) =>
                {
                    Directory.CreateDirectory(all);
                    return all + Path.DirectorySeparatorChar + one;
                });
            return imageObject;
        }
        public static ImageObject downloadImages(ITask o, List<IHasTasks> parentContext)
        {
            ImageObject imageObject = (ImageObject)o;
            ReadMeObject readmeObject = (ReadMeObject)parentContext.Last();
            string check = readmeObject.fileName.Substring(0, readmeObject.fileName.Length - 5) + ".resource"
                    + "\\"
                    + imageObject.filePath;
            WebClient client = new WebClient();
            client.DownloadFile(
                new Uri(imageObject.url)
                , check
            );
            return imageObject;
        }
        public static ReadMeObject setUrlsInDocument(ITask o, List<IHasTasks> parentContext)
        {
            ReadMeObject readmeObject = (ReadMeObject)o;
            int index = -1;
            Array.ForEach(
                Functions.toList(readmeObject.xmlDocument.SelectNodes("//img"))
                    .Where((image) => image.Attributes != null)
                    .Where((image) => image.Attributes["src"] != null)
                    .Where((image) => image.Attributes["src"].Value != "")
                    .ToArray()
                , (imageNode) =>
                {
                    ++index;
                    imageNode.Attributes["src"].Value = ((ImageObject)readmeObject.TaskItems[index]).newUrl;
                }
            );
            return readmeObject;
        }
        public static ReadMeObject rewriteHtmlString(ITask o, List<IHasTasks> parentContext)
        {
            ReadMeObject readmeObject = (ReadMeObject)o;
            StringWriter stringWriter = new StringWriter();
            XmlWriter xmlTextWriter = XmlWriter.Create(stringWriter);
            readmeObject.xmlDocument.WriteTo(xmlTextWriter);
            xmlTextWriter.Flush();

            readmeObject.htmlStringContent = stringWriter.GetStringBuilder().ToString();
            return readmeObject;
        }
        public static Model removeDuplicateReadmeObjectUrl(IHasTasks o)
        {
            Model model = (Model)o;
            bool[] returnValues;
            int index = 0;
            string[] urls = model.TaskItems
                .Select((readmeObject)=>((ReadMeObject)readmeObject).url).ToArray();
            returnValues = urls.Select((string url) =>
            {
                int urlIndex = Array.IndexOf(urls, url,++index);
                return urlIndex == -1;
            })
            .ToArray();
            index = -1;
            model.TaskItems = model.TaskItems
                .Where((x) => returnValues[++index])
                .ToArray();
            return model;
        }

        public static Func<Func<ITask, List<IHasTasks>, ITask>, Func<IHasTasks, IHasTasks>> processTaskList(Func<ITask, bool> continueProcess,List<IHasTasks> parentContext)
        {
            return (Func<ITask, List<IHasTasks>, ITask> filling) =>
            {
                //return (m) => m;
                Func<IHasTasks, IHasTasks> ret = (itemContainingTasks) =>
                {
                    parentContext.Add(itemContainingTasks);
                    Array.ForEach(
                        itemContainingTasks.TaskItems.ToArray()
                        , (readmeObject) =>
                        {
                            if (continueProcess((ITask)readmeObject))
                            {
                                readmeObject.taskDetails = "";
                                try
                                {
                                    filling(readmeObject,parentContext);
                                }
                                catch (Exception e)
                                {
                                    readmeObject.error = e.Message;
                                }

                            }
                        }
                    );
                    return itemContainingTasks;
                };
                return ret;
            };
        }
        public static Func<IHasTasks, IHasTasks> throttle(Func<IHasTasks, IHasTasks> filling)
        {
            Func<IHasTasks, IHasTasks> ret = (model) =>
            {
                ITask[] all = model.TaskItems.ToArray();
                int process = 0;
                int activeConnections = 10;
                int cooldownTime = 1000;
                while(process<all.Length){
                    model.TaskItems = all.Skip(process).Take(activeConnections).ToArray();
                    filling(model);
                    process = process + activeConnections;
                    Console.WriteLine("Have fetched, now sleeping"+process.ToString());
                    if (process < all.Length)
                    {
                        Thread.Sleep(cooldownTime);
                    }
                    Console.WriteLine("Woke up, continuing"+process.ToString());
                }
                model.TaskItems = all;
                return model;
            };
            return ret;
        }
    }
}
