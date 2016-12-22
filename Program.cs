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
            Func<Model,Model> process = Functions.compose(
                new Func<Model, Model>[] 
                  { 
                    Functions.removeDuplicateReadmeObjectUrl
                    ,Functions.taco(
                        Functions.taco(Functions.downLoadHtml,Functions.processEachReadmeObject)
                        ,Functions.throttle
                    )
                    ,Functions.taco(Functions.setHtml,Functions.processEachReadmeObject)
                    ,Functions.taco(Functions.setFileName,Functions.processEachReadmeObject)
                    ,Functions.taco(Functions.setXml,Functions.processEachReadmeObject)
                    ,Functions.taco(Functions.createImageObjects,Functions.processEachReadmeObject)
                    //following functions deal with imageObjects, should wrap them like processEachReadmeObject
                    ,Functions.taco(
                        Functions.taco(Functions.setImageFileNamesPathsAndNewUrl,Functions.processImageObject)
                        ,Functions.processEachReadmeObject
                    )
                    ,Functions.taco(
                        Functions.taco(Functions.createResourceRootDirectories,Functions.processImageObject)
                        ,Functions.processEachReadmeObject
                    )
                    ,Functions.taco(//can't throttle this with taco, C# can't handle the type for it @todo: anther implementation of throttle
                        Functions.taco(Functions.downloadImages,Functions.processImageObject)
                        ,Functions.processEachReadmeObject
                    )
                    //these operate on ReadmeObjects
                    ,Functions.taco(Functions.setUrlsInDocument,Functions.processEachReadmeObject)
                    ,Functions.taco(Functions.rewriteHtmlString,Functions.processEachReadmeObject)
                    ,Functions.taco(Functions.saveHtmlStringToFile,Functions.processEachReadmeObject)
                  }
            );
            Model result = process(whatevuh);
            var imageErrors = result.readmeObjects
                    .Select((readmeObject) => readmeObject.images
                        .Where((imageObject) => imageObject.error != null)
                    )
                    .Aggregate((all, one) => {
                        if(all == null){
                            all = new ImageObject[] {};
                        }
                        return all.Concat(one);
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
