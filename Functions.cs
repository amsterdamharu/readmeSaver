﻿using System;
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
        public static Func<ITask, ITask> compose(Func<ITask, ITask> one, Func<ITask, ITask> two)
        {
            return x => two(one(x));
        }
        public static Func<ITask, ITask> compose(Func<ITask, ITask>[] functions)
        {
            return ((new Func<ITask, ITask>[] { (x) => x })).Concat(functions)
                .Aggregate((f, all) => Functions.compose(f, all));
        }

        public static ReadMeObject setHtml(ITask o)
        {
            ReadMeObject readmeObject = (ReadMeObject)o;
            readmeObject.taskDetails = "Convert readme to html";
            readmeObject.htmlStringContent = CommonMark.CommonMarkConverter.Convert(readmeObject.readmeStringContent);
            return readmeObject;
        }

        public static ReadMeObject downloadText(ITask o)
        {
            ReadMeObject readmeObject = (ReadMeObject)o;
            readmeObject.taskDetails = "Download:"+readmeObject.url;
            WebClient client = new WebClient();
            readmeObject.readmeStringContent = client.DownloadString(readmeObject.url);
            return readmeObject;
        }

        public static ReadMeObject setFileName(ITask o)
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
        public static ReadMeObject saveHtmlStringToFile(ITask o)
        {
            ReadMeObject readmeObject = (ReadMeObject)o;
            readmeObject.taskDetails = "Save to:" + readmeObject.fileName;
            System.IO.File.WriteAllText(readmeObject.basePath + readmeObject.fileName, readmeObject.htmlStringContent);
            return readmeObject;
        }
        public static Func<ITask, ITask> createStatusUpdate(Model m)
        {
            return (task) =>
            {
                if (task.error == null)
                {
                    m.status.processed++;
                }
                else
                {
                    m.status.errors++;
                }
                if (m.statusUpdater != null)
                {
                    m.statusUpdater(m.status);
                }
                return task;
            };
        }
        public static List<System.Xml.XmlNode> toList(System.Xml.XmlNodeList nodelist){
            return nodelist.Cast<System.Xml.XmlNode>().ToList();
        }
        public static ReadMeObject setXml(ITask o)
        {
            ReadMeObject readmeObject = (ReadMeObject)o;
            readmeObject.xmlDocument = new System.Xml.XmlDocument();
            readmeObject.xmlDocument.LoadXml("<body>"+readmeObject.htmlStringContent+"</body>");
            return readmeObject;
        }
        public static ReadMeObject createImageObjects(ITask o)
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
                    io.basePath = readmeObject.basePath + readmeObject.fileName.Substring(0, readmeObject.fileName.Length - 5) + ".resource";
                    return io;
                })
                .ToArray();
            readmeObject.TaskItems = l;
            return readmeObject;
        }
        public static ImageObject setImageFileNamesPaths(ITask o)
        {
            ImageObject imageObject = (ImageObject)o;
            Uri url = new Uri(imageObject.url);
            string[] path = (url.Host + url.AbsolutePath)
                .Split(new char[] {'/'}).ToArray();
            imageObject.fileName = path.Last();
            imageObject.filePath = String.Join(
                new String(new char[] {Path.DirectorySeparatorChar}), path
            )
                .Replace("%20", "-")
                .Replace(":","-");
            return imageObject;
        }
        public static ImageObject createResourceRootDirectories(ITask o)
        {
            ImageObject imageObject = (ImageObject)o;
            new string[] { imageObject.basePath }
                .Concat(imageObject.filePath.Split(new char[] { Path.DirectorySeparatorChar }))
                .Aggregate((all, one) =>
                {
                    Directory.CreateDirectory(all);
                    return all + Path.DirectorySeparatorChar + one;
                });
            return imageObject;
        }
        public static ImageObject downloadImages(ITask o)
        {
            ImageObject imageObject = (ImageObject)o;
            string check = imageObject.basePath
                    + "\\"
                    + imageObject.filePath;
            WebClient client = new WebClient();
            client.DownloadFile(
                new Uri(imageObject.url)
                , check
            );
            imageObject.newUrl = imageObject.basePath
                + "/"
                + imageObject.filePath.Replace(
                    Path.DirectorySeparatorChar
                    , '/'
                );
            return imageObject;
        }
        public static ReadMeObject setUrlsInDocument(ITask o)
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
                    string newUrl = ((ImageObject)readmeObject.TaskItems[index]).newUrl;
                    if (newUrl != null)
                    {
                        imageNode.Attributes["src"].Value = ((ImageObject)readmeObject.TaskItems[index]).newUrl;
                    }
                }
            );
            return readmeObject;
        }
        public static Func<Func<ITask,ITask>,Func<ITask,ITask>> createThrottle (int max,List<Task> taskList, Func<Task,Task> afterRun){
            return (Func<ITask, ITask> taskHandler) =>
            {
                return (ITask task) =>
                {
                    if (taskList.Count % max == 0 && taskList.Count != 0)
                    {
                        Task.WaitAll(taskList.ToArray());
                    }
                    Task t = Task.Run(() =>
                    {
                        try
                        {
                            taskHandler(task);
                        }
                        catch (Exception e)
                        {
                            task.error = e.Message;
                        }
                        
                    });
                    taskList.Add(t);
                    afterRun(t);
                    return task;
                };
            };
        }
        public static Func<ITask, ITask> createWaitFor(List<List<Task>> taskList)
        {
            return (ITask task) =>
            {
                taskList.ForEach((tl) =>
                {
                    Task.WaitAll(tl.ToArray());
                });
                return task;
            };
        }
        public static ReadMeObject rewriteHtmlString(ITask o)
        {
            ReadMeObject readmeObject = (ReadMeObject)o;
            StringWriter stringWriter = new StringWriter();
            XmlWriter xmlTextWriter = XmlWriter.Create(stringWriter);
            readmeObject.xmlDocument.WriteTo(xmlTextWriter);
            xmlTextWriter.Flush();

            readmeObject.htmlStringContent = stringWriter.GetStringBuilder().ToString();
            return readmeObject;
        }
        public static ITask setBasePath(ITask task)
        {
            Array.ForEach(
                task.TaskItems
                , (taskItem) =>
                {
                    taskItem.basePath = task.basePath;
                }
            );
            return task;
        }
        public static Model removeDuplicateReadmeObjectUrl(ITask o)
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
            model.status = new StatusUpdate();
            model.status.errors = 0;
            model.status.processed = 0;
            model.status.total = model.TaskItems.Count();
            return model;
        }

        public static Func<Func<ITask, ITask>, Func<ITask, ITask>> processTaskList(Func<ITask, bool> continueProcess)
        {
            return (Func<ITask, ITask> filling) =>
            {
                //return (m) => m;
                Func<ITask, ITask> ret = (itemContainingTasks) =>
                {
                    Array.ForEach(
                        itemContainingTasks.TaskItems.ToArray()
                        , (readmeObject) =>
                        {
                            if (continueProcess((ITask)readmeObject))
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
                        }
                    );
                    return itemContainingTasks;
                };
                return ret;
            };
        }
        public static Func<ITask, ITask> createPerTaskProcessor(Model workModel)
        {
            List<Task> taskList = new List<Task>();
            List<Task> webConnectionList = new List<Task>();
            List<List<Task>> allTasks = new List<List<Task>>();
            Func<Func<ITask, ITask>, Func<ITask, ITask>> processTasks =
                Functions.processTaskList((m) => m.error == null);
            Func<Func<ITask, ITask>, Func<ITask, ITask>> throttleTask = Functions.createThrottle(workModel.activeTasks, taskList, (x) => x);
            Func<Func<ITask, ITask>, Func<ITask, ITask>> throttleWebConnectionAndWait
                = Functions.createThrottle(workModel.activeConnections, webConnectionList, (x) => { Task.WaitAll(x); return x; });
            allTasks.Add(taskList);
            allTasks.Add(webConnectionList);
            Func<ITask, ITask> waitForTasks = Functions.createWaitFor(allTasks);
            Func<ITask, ITask> process =
                Functions.compose(
                    new Func<ITask, ITask>[] 
                        {
                            Functions.removeDuplicateReadmeObjectUrl//function that operates on the model
                            ,Functions.setBasePath
                            ,processTasks(
                                throttleTask(
                                    Functions.compose(
                                        new Func<ITask, ITask>[] 
                                          { 
                                            //throttle webconnection here but need to wait for this to finish before moving on the next
                                            //  throttle needed because throttleTask may start hundreds of tasks causing hundreds of web connections
                                            throttleWebConnectionAndWait(Functions.downloadText)
                                            ,Functions.setHtml
                                            ,Functions.setFileName
                                            ,Functions.setXml
                                            ,Functions.createImageObjects
                                            //following functions deal with imageObjects, should wrap them like processEachReadmeObject
                                            ,processTasks(
                                               Functions.setImageFileNamesPaths
                                            )
                                            ,processTasks(
                                              Functions.createResourceRootDirectories
                                            )
                                            ,processTasks(
                                              throttleWebConnectionAndWait(Functions.downloadImages)
                                            )
                                            //these operate on ReadmeObjects
                                            ,Functions.setUrlsInDocument
                                            ,Functions.rewriteHtmlString
                                            ,Functions.saveHtmlStringToFile
                                            ,Functions.createStatusUpdate(workModel)
                                          }
                                    )
                                )
                            )
                            ,waitForTasks
                        }
                );
            return process;
        }
    }
}
