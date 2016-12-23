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
            Model whatevuh = new Model();
            whatevuh.TaskItems = Model.rmObjects;
            List<Task> taskList = new List<Task>();
            List<IHasTasks> parentContext = new List<IHasTasks>();
            Func<Func<ITask, List<IHasTasks>, ITask>, Func<IHasTasks, IHasTasks>> processTask = 
                Functions.processTaskList((m) => m.error == null,parentContext);
            Func<IHasTasks,IHasTasks> process = Functions.compose(
                new Func<IHasTasks, IHasTasks>[] 
                  { 
                    Functions.removeDuplicateReadmeObjectUrl
                    ,Functions.taco(Functions.downLoadHtml,processTask)
                    ,Functions.taco(Functions.setHtml,processTask)
                    ,Functions.taco(Functions.setFileName,processTask)
                    ,Functions.taco(Functions.setXml,processTask)
                    ,Functions.taco(Functions.createImageObjects,processTask)
                    //following functions deal with imageObjects, should wrap them like processEachReadmeObject
                    ,Functions.taco(
                        Functions.multiWrap(
                            Functions.taco(Functions.setImageFileNamesPathsAndNewUrl, processTask)
                        )
                        ,processTask
                    )
                    ,Functions.taco(
                        Functions.multiWrap(
                            Functions.taco(Functions.createResourceRootDirectories,processTask)
                        )
                        ,processTask
                    )
                    ,Functions.taco(//can't throttle this with taco, C# can't handle the type for it @todo: anther implementation of throttle
                        Functions.multiWrap(
                            Functions.taco(Functions.downloadImages,processTask)
                        )
                        ,processTask
                    )
                    //these operate on ReadmeObjects
                    ,Functions.taco(Functions.setUrlsInDocument,processTask)
                    ,Functions.taco(Functions.rewriteHtmlString,processTask)
                    ,Functions.taco(Functions.saveHtmlStringToFile,processTask)
                  }
            );
            Model result = (Model)process(whatevuh);
            ImageObject[] imageErrors = result.TaskItems
                    .Where((readmeObject) => ((ReadMeObject)readmeObject).TaskItems != null)
                    .Select((readmeObject) => (
                        (ReadMeObject)readmeObject).TaskItems
                        .Select((imageObject)=>(ImageObject)imageObject)
                        .Where((imageObject) => imageObject.error != null)
                    )
                    .Aggregate(
                      (all, one) => {
                        if(all == null){
                            all = new ImageObject[] {};
                        }
                        return all.Concat(one);
                    })
                    .ToArray();

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
                    (imageErrors.Length > 0)
                        ? (Environment.NewLine + "    Non critical, Failed images:" 
                            + imageErrors.Length.ToString() 
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
    }
}
