using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace readmeApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Program.run(args);
        }


        public static void run (String[] args){
            Model workModel = new Model();
            workModel.basePath = "E:\\tmp\\test\\";
            string paths = @"http://localhost:8888/documentation/tmpreadme/1.txt
http://localhost:8888/documentation/tmpreadme/2.txt
http://localhost:8888/documentation/tmpreadme/3.txt";
            workModel.TaskItems = paths.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None)
                .Select((path)=>{
                    ReadMeObject r = new ReadMeObject();
                    r.url = path;
                    return r;
                })
                .ToArray();
            workModel.activeConnections = 2;
            workModel.statusUpdater = Program.updateHandler;
            Func<ITask, ITask> process = Functions.createPerTaskProcessor(workModel);
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

        public static StatusUpdate updateHandler(StatusUpdate u)
        {
            Console.WriteLine("Processed:" + u.processed.ToString() + " of:" + u.total.ToString() + " Failed:" + u.errors.ToString());
            return u;
        }
    }
}
