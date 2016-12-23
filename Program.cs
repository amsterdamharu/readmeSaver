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
            whatevuh.readmeObjects = Model.rmObjects;
            Func<Func<ReadMeObject, ReadMeObject>, Func<Model, Model>> processReadme = Functions.processEachReadmeObject((m) => m.error != null);
            Func<Model,Model> process = Functions.compose(
                new Func<Model, Model>[] 
                  { 
                    Functions.removeDuplicateReadmeObjectUrl
                    ,Functions.taco(
                        Functions.taco(Functions.downLoadHtml,processReadme)
                        ,Functions.throttle
                    )
                    ,Functions.taco(Functions.setHtml,processReadme)
                    ,Functions.taco(Functions.setFileName,processReadme)
                    ,Functions.taco(Functions.setXml,processReadme)
                    ,Functions.taco(Functions.createImageObjects,processReadme)
                    //following functions deal with imageObjects, should wrap them like processEachReadmeObject
                    ,Functions.taco(
                        Functions.taco(Functions.setImageFileNamesPathsAndNewUrl,Functions.processImageObject)
                        ,processReadme
                    )
                    ,Functions.taco(
                        Functions.taco(Functions.createResourceRootDirectories,Functions.processImageObject)
                        ,processReadme
                    )
                    ,Functions.taco(//can't throttle this with taco, C# can't handle the type for it @todo: anther implementation of throttle
                        Functions.taco(Functions.downloadImages,Functions.processImageObject)
                        ,processReadme
                    )
                    //these operate on ReadmeObjects
                    ,Functions.taco(Functions.setUrlsInDocument,processReadme)
                    ,Functions.taco(Functions.rewriteHtmlString,processReadme)
                    ,Functions.taco(Functions.saveHtmlStringToFile,processReadme)
                  }
            );
            Model result = process(whatevuh);
            ImageObject[] imageErrors = result.readmeObjects
                    .Where((readmeObject)=> readmeObject.images != null)
                    .Select((readmeObject) => readmeObject.images
                        .Where((imageObject) => imageObject.error != null)
                    )
                    .Aggregate(
                      new ImageObject[] {}
                      ,(all, one) => {
                        if(all == null){
                            all = new ImageObject[] {};
                        }
                        return all.Concat(one).ToArray();
                    })
                    .ToArray();

            string[] errors = result.readmeObjects
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
