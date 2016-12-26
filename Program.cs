using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace readmeApp
{
    class Program
    {
        static ReadMeObject createRm(string error)
        {
            ReadMeObject r = new ReadMeObject();
            r.error = error;
            return r;
        }

        static void Main(string[] args)
        {
            Program.run(args);
        }


        public static void run (String[] args){
            Model workModel = new Model();
            workModel.TaskItems = Model.rmObjects;
            List<Task> taskList = new List<Task>();
            List<Task> webConnectionList = new List<Task>();
            List<List<Task>> allTasks = new List<List<Task>>();
            Func<Func<ITask, ITask>, Func<ITask, ITask>> processTasks = 
                Functions.processTaskList((m) => m.error == null);
            Func<Func<ITask, ITask>, Func<ITask, ITask>> throttleTask = Functions.createThrottle(workModel.activeTasks, taskList);
            Func<Func<ITask, ITask>, Func<ITask, ITask>> throttleWebConnection = Functions.createThrottle(workModel.activeConnections, webConnectionList);
            allTasks.Add(taskList);
            allTasks.Add(webConnectionList);
            Func<ITask, ITask> waitForTasks = Functions.createWaitFor(allTasks);
            Func<ITask, ITask> process =
                Functions.compose(
                    new Func<ITask, ITask>[] 
                        {
                            Functions.removeDuplicateReadmeObjectUrl//function that operates on the model
                            ,processTasks(
                                throttleTask(
                                    Functions.compose(
                                        new Func<ITask, ITask>[] 
                                          { 
                                            //@todo: should throttle webconnection here but need to wait for this to finish before moving on the next
                                            //  throttle needed because throttleTask may start hundreds of tasks causing hundreds of web connections
                                            Functions.downloadText
                                            ,Functions.setHtml
                                            ,Functions.setFileName
                                            ,Functions.setXml
                                            ,Functions.createImageObjects
                                            //following functions deal with imageObjects, should wrap them like processEachReadmeObject
                                            ,processTasks(
                                               Functions.setImageFileNamesPathsAndNewUrl
                                            )
                                            ,processTasks(
                                              Functions.createResourceRootDirectories
                                            )
                                            ,processTasks(
                                              throttleWebConnection(Functions.downloadImages)
                                            )
                                            //these operate on ReadmeObjects
                                            ,Functions.setUrlsInDocument
                                            ,Functions.rewriteHtmlString
                                            ,Functions.saveHtmlStringToFile
                                          }
                                    )
                                )
                            )
                            ,waitForTasks
                        }
                );
            Model result = (Model)process(workModel);
            List<ImageObject> imageErrors = result.TaskItems
                    .Where((readmeObject) => readmeObject.TaskItems != null)
                    .Select((readmeObject) => {
                        return readmeObject.TaskItems
                        .Where((imageObject) => imageObject != null)
                        .Select((imageObject) => (ImageObject)imageObject)
                        .Where((imageObject) => imageObject.error != null)
                        .ToList();
                    })//.ToList()
                    .Aggregate(
                      new List<ImageObject> ()
                      ,(all, one) => {
                        return  all.Concat(one).ToList();
                    });
            string[] errors = result.TaskItems
                .Where((o) => o.error != null)
                .Select((readmeObject) => readmeObject.error + Environment.NewLine + "   " + readmeObject.taskDetails)
                .ToArray();


            Console.WriteLine(
                "Done "
                + (
                    (errors.Length > 0)
                        ? (Environment.NewLine + " Errors:" + errors.Length.ToString() + Environment.NewLine + "   " + String.Join(Environment.NewLine + "   ", errors))
                        : ""
                 )
                + (
                    (imageErrors.Count > 0)
                        ? (Environment.NewLine + "    Non critical, Failed images:" 
                            + imageErrors.Count.ToString() 
                            + Environment.NewLine + "     " 
                            + String.Join(
                                Environment.NewLine + "     "
                                ,imageErrors.Select((image)=>{
                                  return image.url
                                    + Environment.NewLine + "     "
                                    + image.error;
                                })
                              )
                           )
                           : ""
                 )
            );
            Console.ReadLine();
        }

        public static void someTest(string[] args)
        {
            Model testModel = new Model();
            List<ReadMeObject> tasks = new List<ReadMeObject>();
            int i = 0;
            while (++i < 10)
            {
                tasks.Add(
                    Program.createRm(i.ToString())
                );
            }
            Func<Func<ITask, ITask>, Func<ITask, ITask>> processTasks =
                Functions.processTaskList((m) => true);
            Func<Func<ITask, ITask>, Func<ITask, ITask>> throttleWeb = Functions.createThrottle(20, new List<Task>());
            Func<Func<ITask, ITask>, Func<ITask, ITask>> throttleTask = Functions.createThrottle(11, new List<Task>());
            testModel.TaskItems = tasks.ToArray();
            var hi = processTasks(
                throttleTask(
                    Functions.compose(
                        new Func<ITask, ITask>[] {
                            (ITask t)=>{
                                Console.WriteLine(">>  task:" + t.error + " -- handler 1");
                                return t;
                            }
                            ,
                            throttleWeb(
                                (ITask t)=>{
                                    System.Threading.Thread.Sleep(1000);
                                    Console.WriteLine(">>  task:" + t.error + " -- handler 2");
                                    return t;
                                }
                            )
                        }
                    )
                )
            );
            hi(testModel);

            Console.ReadLine();
        }    
    }
}
