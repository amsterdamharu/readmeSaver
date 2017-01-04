        //compose 2 functions but guard the functions so they cannot recieve a promise
        //  They will only receive the resolve value
        var compose2 = (one,two)=>{
            return (x)=>{
                var resultOne
                if(x && typeof x.then === "function"){
                    return x
                        .then((x)=>one(x))
                        .then((x)=>two(x));
                }
                resultOne = one(x);
                if(resultOne && typeof resultOne.then === "function"){
                    return resultOne
                        .then((x)=>two(x));
                }
                return two(resultOne);
            }
        };
        //compose an array of funcitons
        var compose = (fn)=>{
        return fn.reduce((pref,current)=>{
            return compose2(pref,current);
            }
            ,(x)=>x
        )
        };
        //simulate an asynchronous action where you can pass in for how long to wait 
        //  and what value to return
        var wait = (howLong,returnValue)=>new Promise(
            (resolve,reject)=>{
                console.log("Wait has been called with returnvalue:",returnValue)
                setTimeout(()=>{
                    console.log("resolving:",returnValue);
                    resolve(returnValue);
                }
                    ,howLong
                )
            }
        );
        var createThrottle = (simultaniousTasks, fn)=>{//fn is a function that should return a promise
            var taskList = [];//store all promises wrapped in a task object here
            return (val)=>{
                var initialPromise = Promise.resolve(val)//first promise to return does not wait
                    ,task = {promise:false,done:false,resolveFunction:false};//create a task for the process
                taskList.push(task);//add task to list
                if(taskList.length > simultaniousTasks && taskList.length !== 0){//if true the initialPromise needs to wait for fn to finish
                    initialPromise = new Promise((res,rej)=>task.resolveFunction=res)//create a promise but set the resolve function to the task.resolveFunction
                        .then((any)=>val);//when this resolves then return the original value
                }
                return initialPromise//return fn wrapped in a promise that does or does not wait for other tasks to finish (first tasks will not wait)
                    .then((val)=>fn(val))//call the function, we either waited or there were no more tasks than simultaniousTasks
                    .then((val)=>{//the throttled function is done and resolved in a value
                        var waitingTasks//this will hold the tasks that are waiting for others to finish
                        task.done = true;//current task is done, mark it this way
                        taskList = taskList.filter((t)=>!t.done);//remove tasks that are done from the tasklist
                        //waitingTasks is a list of tasks that are waiting fpr other tasks to finish
                        //  their initialPromise was not an immediately resolving promise but a new promise that is waiting
                        //  for another promise to resolve so it can start a waiting one
                        waitingTasks = taskList.filter((t)=>typeof t.resolveFunction === "function");//set the list of waitingTasks
                        if(!waitingTasks[0]){
                            console.log("wuh the fuck not?",val);
                        }
                        (waitingTasks[0] //make sure it is not undefined (there are still waiting tasks)
                            && waitingTasks[0].resolveFunction("start another"));//call resolve on the initialPromise of a waiting task
                        return val;//resolve the promise
                    });
            }
        }
        var processItem = compose([
            (val)=>Promise.resolve(val)//making sure compose returns promises; any function after a promise causes all functions to be wrapped in a .then
            ,createThrottle(3, (val)=>wait((15-(val*2))*500,val))//throttle this process, could be an xhr or db write
            ,(val)=>{console.log("value:",val); return val;}
        ]);

        Promise.all(//process 7 items simultaniously, one of the process functions is throttled to allow only 3 active (unresolved promises)
            [1,2,3,4,5,6,7].map(processItem)
        ).then((resolve)=>console.log("All items are resolved",resolve));
