/**
 * writing it in ts is easier, study this and then try again in C#
 */
interface ITask{
  message:string;
}
interface IHasTasks{
  tasks:ITask[];
}
var compose2 = function(one:(any)=>any,two:(any)=>any):(any)=>any{
  return (x)=>{
    var resultOne
    if(
      (x && typeof x.then === "function")
    ){
      return x
        .then((x)=>one(x))
        .then((x)=>two(x));
    }
    resultOne = one(x);
    if(
      (resultOne && typeof resultOne.then === "function")
    ){
      return resultOne
        .then((x)=>two(x));
    }
    return two(resultOne);
  }
}

var compose = function(fn:((any)=>any)[]):(any)=>any{
  return fn.reduce((pref,current)=>{
      return compose2(pref,current);
    }
    ,(x)=>x
  )
}

var taco = function(filling:(IHasTasks,ITask)=>ITask, wrapper){
  return wrapper(filling);
}

var Model = {
  message:"MODEL                "
  ,tasks:[
    {
      message:"task 1               "
      ,tasks:[
        {
          message:"task 1 >>>> subtask 1"
        }
        ,{
          message:"task 1 >>>> subtask 2"
        }
      ]
    }
    ,{
      message:"task 2               "
      ,tasks:[
        {
          message:"task 2 >>>> subtask 1"
        }
        ,{
          message:"task 2 >>>> subtask 2"
        }
      ]
    }
  ]
};
var mergeParentTask = (composedFunction)=>{
  return(parent,task)=>{
    return composedFunction({
      parent:parent
      ,task:task
      ,taskPromises:[]
    });
  }
};
var taskHandlers = [
  (o)=>{ console.log(o.task.message," ---- handler 1"); return o; }
  ,(o)=>{ console.log(o.task.message," ---- handler 2"); return o; }
];



var hasTaskHandler = function(taskHandler){ 
  return (mergedTaskParent)=>{
    mergedTaskParent.task.tasks.forEach((task)=>taskHandler({parent:mergedTaskParent.task,task:task}));
    return mergedTaskParent;
  }
};
var hasTaskHandlerAsync = function(taskHandler){ 
  return (mergedTaskParent)=>{
    return compose(
      mergedTaskParent.task.tasks.map((task)=>{
        return (mergedTaskParent)=>{
          return taskHandler({
            parent:mergedTaskParent.task
            ,task:task
            ,taskPromises:mergedTaskParent.taskPromises
          })
        }
      })
    )(mergedTaskParent)
    .then((x)=>{
      return mergedTaskParent;
    });
  }
};

console.log(`
/**
 *************************************************************************
 *
 * grouped by handler, executes handler for each task first then goes to the next task
 * when no tasks and subtasks are to be executed by the handler it will go to the next handler
 * 
 ************************************************************************* 
 */
`);
mergeParentTask(
  compose([
      (x)=>x
    ].concat(
      taskHandlers.map((taskHandler)=>{
        return compose([
          taco(taskHandler,hasTaskHandler)
          ,taco(taskHandler,compose([hasTaskHandler,hasTaskHandler]))
        ]);
      })
    )
  )
)(null,Model);

console.log(`
/**
 *************************************************************************
 *
 * grouped by task; executes each handler for task and tasks subtask first
 * moves to the next task when all handlers of task and subtask are executed
 * 
 ************************************************************************* 
 */
`);
mergeParentTask(
  compose([
    hasTaskHandler(
      compose(
        taskHandlers.concat([
          hasTaskHandler(
            compose(taskHandlers)
          )          
        ])
      )
    )
    ,compose(taskHandlers)
  ])
)(null,Model);

console.log(`
/**
 *************************************************************************
 *
 * asynchronous
 * grouped by task; executes each handler for task and tasks subtask first
 * moves to the next task when all handlers of task and subtask are executed and waitFor is added
 * 
 ************************************************************************* 
 */
`);
taskHandlers[1] = 
  (o)=>{
    o.taskPromises.push(
      new window["Promise"]((resolve,reject)=>{
        setTimeout(()=>{
          console.log(o.task.message," ---- handler 2");
          resolve(o);
        },20);
      })
    );
    return window["Promise"].resolve(o);
  };
var waitFor = (x)=>{
  return window["Promise"].all(x.taskPromises)
    .then((y)=>x);
}
taskHandlers[2] = (o)=>{
  console.log(o.task.message," ---- handler 3");
  return o;
}
taskHandlers[3] = waitFor;
// taskHandlers[2] = (x)=>window["Promise"].resolve(x) 
mergeParentTask(
  compose([
    hasTaskHandlerAsync(
      compose(
        taskHandlers
          .concat([
            hasTaskHandlerAsync(
              compose(taskHandlers.slice(0,-1))//no waitfor, so will not wait to continue
            )
          ])
      )
    )
    ,waitFor
    ,compose(taskHandlers)
  ])
)(null,Model);
