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
        public static Func<Model, Model> compose(Func<Model, Model> one, Func<Model, Model> two)
        {
            return x => two(one(x));
        }
        public static Func<Model, Model> compose(Func<Model, Model>[] functions)
        {
            return ((new Func<Model, Model>[] { (x) => x })).Concat(functions)
                .Aggregate((f, all) => Functions.compose(f, all));
        }
        public static Func<Model, Model> taco(Func<ReadMeObject, ReadMeObject> filling, Func<Func<ReadMeObject, ReadMeObject>, Func<Model, Model>> wrap)
        {
            return wrap(filling);
        }
        public static Func<Model, Model> taco(Func<Model, Model> filling, Func<Func<Model, Model>, Func<Model, Model>> wrap)
        {
            return wrap(filling);
        }
        public static Func<ReadMeObject, ReadMeObject> taco(Func<ImageObject, string, ImageObject> filling, Func<Func<ImageObject, string, ImageObject>, Func<ReadMeObject, ReadMeObject>> wrap)
        {
            return wrap(filling);
        }

        public static ReadMeObject setHtml(ReadMeObject readmeObject)
        {
            readmeObject.taskDetails = "Convert readme to html";
            readmeObject.htmlStringContent = CommonMark.CommonMarkConverter.Convert(readmeObject.readmeStringContent);
            return readmeObject;
        }

        public static ReadMeObject downLoadHtml(ReadMeObject readmeObject)
        {
            readmeObject.taskDetails = "Download:"+readmeObject.url;
            WebClient client = new WebClient();
            readmeObject.readmeStringContent = client.DownloadString(readmeObject.url);
            return readmeObject;
        }

        public static ReadMeObject setFileName(ReadMeObject readmeObject)
        {
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
        public static ReadMeObject saveHtmlStringToFile(ReadMeObject readmeObject)
        {
            readmeObject.taskDetails = "Save to:"+readmeObject.fileName;
            System.IO.File.WriteAllText(readmeObject.fileName, readmeObject.htmlStringContent);
            return readmeObject;
        }
        public static List<System.Xml.XmlNode> toList(System.Xml.XmlNodeList nodelist){
            return nodelist.Cast<System.Xml.XmlNode>().ToList();
        }
        public static ReadMeObject setXml(ReadMeObject readmeObject){
            readmeObject.xmlDocument = new System.Xml.XmlDocument();
            readmeObject.xmlDocument.LoadXml("<body>"+readmeObject.htmlStringContent+"</body>");
            return readmeObject;
        }
        public static ReadMeObject createImageObjects(ReadMeObject readmeObject)
        {
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
            readmeObject.images = l;
            return readmeObject;
        }
        public static ImageObject setImageFileNamesPathsAndNewUrl(ImageObject imageObject,string baseDirectory)
        {
            Uri url = new Uri(imageObject.url);
            string[] path = (url.Host + url.AbsolutePath)
                .Split(new char[] {'/'}).ToArray();
            imageObject.fileName = path.Last();
            imageObject.filePath = String.Join(
                new String(new char[] {Path.DirectorySeparatorChar}), path
            )
                .Replace("%20", "-")
                .Replace(":","-");
            imageObject.newUrl = baseDirectory
                + "/" 
                +imageObject.filePath.Replace(
                    Path.DirectorySeparatorChar
                    , '/'
                );
            return imageObject;
        }
        public static ImageObject createResourceRootDirectories(ImageObject imageObject,string baseDirectory)
        {
            new string[] { baseDirectory }
                .Concat(imageObject.filePath.Split(new char[] { Path.DirectorySeparatorChar }))
                .Aggregate((all, one) =>
                {
                    Directory.CreateDirectory(all);
                    return all + Path.DirectorySeparatorChar + one;
                });
            return imageObject;
        }
        public static ImageObject downloadImages(ImageObject imageObject,string baseDirectory)
        {
            string check = baseDirectory
                    + "\\"
                    + imageObject.filePath;
            WebClient client = new WebClient();
            client.DownloadFile(
                new Uri(imageObject.url)
                , check
            );
            return imageObject;
        }
        public static ReadMeObject setUrlsInDocument(ReadMeObject readmeObject)
        {
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
                    imageNode.Attributes["src"].Value = readmeObject.images[index].newUrl;
                }
            );
            return readmeObject;
        }
        public static ReadMeObject rewriteHtmlString(ReadMeObject readmeObject)
        {

            StringWriter stringWriter = new StringWriter();
            XmlWriter xmlTextWriter = XmlWriter.Create(stringWriter);
            readmeObject.xmlDocument.WriteTo(xmlTextWriter);
            xmlTextWriter.Flush();

            readmeObject.htmlStringContent = stringWriter.GetStringBuilder().ToString();
            return readmeObject;
        }
        public static Model removeDuplicateReadmeObjectUrl(Model model)
        {
            bool[] returnValues;
            int index = 0;
            string[] urls = model.readmeObjects
                .Select((readmeObject)=>readmeObject.url).ToArray();
            returnValues = urls.Select((string url) =>
            {
                int urlIndex = Array.IndexOf(urls, url,++index);
                return urlIndex == -1;
            })
            .ToArray();
            index = -1;
            model.readmeObjects = model.readmeObjects
                .Where((x) => returnValues[++index])
                .ToArray();
            return model;
        }

        public static Func<Func<ReadMeObject, ReadMeObject>,Func<Model, Model>> processEachReadmeObject(Func<IHasError,bool> continueProcess)
        {
            return (Func<ReadMeObject, ReadMeObject> filling) =>
            {
                //return (m) => m;
                Func<Model, Model> ret = (model) =>
                {
                    List<Task> taskList = new List<Task>();
                    Array.ForEach(
                        model.readmeObjects
                        , (readmeObject) =>
                        {
                            taskList.Add(Task.Run(() =>
                            {
                                if (continueProcess((IHasError)readmeObject))
                                {
                                    readmeObject.taskDetails = "";
                                    try
                                    {
                                        filling(readmeObject);
                                    }
                                    catch (Exception e)
                                    {
                                        readmeObject.error = e.Message;
                                    }

                                }
                            }));
                        }
                    );
                    Task.WaitAll(taskList.ToArray());
                    return model;
                };
                return ret;
            };
        }
        public static Func<Model, Model> throttle(Func<Model, Model> filling)
        {
            Func<Model, Model> ret = (model) =>
            {
                ReadMeObject[] all = model.readmeObjects;
                int process = 0;
                while(process<all.Length){
                    model.readmeObjects = all.Skip(process).Take(model.activeConnections).ToArray();
                    filling(model);
                    process = process + model.activeConnections;
                    Console.WriteLine("Have fetched, now sleeping"+process.ToString());
                    if (process < all.Length)
                    {
                        Thread.Sleep(model.cooldownTime);
                    }
                    Console.WriteLine("Woke up, continuing"+process.ToString());
                }
                model.readmeObjects = all;
                return model;
            };
            return ret;
        }
        public static Func<ReadMeObject, ReadMeObject> processImageObject(Func<ImageObject, string, ImageObject> filling)
        {
            Func<ReadMeObject, ReadMeObject> ret = (readmeObject) =>
            {
                string basePath = readmeObject.fileName.Substring(0, readmeObject.fileName.Length - 5) + ".resource";
                List<Task> taskList = new List<Task>();
                Array.ForEach(
                    readmeObject.images
                    , (imageObject) =>
                    {
                        taskList.Add(Task.Run(() =>
                        {
                            try
                            {
                                filling(imageObject, basePath);
                            }
                            catch (Exception e)
                            {
                                imageObject.error = e.Message;
                            }
                        }));
                    }
                );
                return readmeObject;
            };
            return ret;
        }
    }
}
