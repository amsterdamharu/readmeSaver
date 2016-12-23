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
  return (x)=>two(one(x));
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
  tasks:[
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
    (x)=>x
    ,taco(
      compose(
        taskHandlers.concat([
          taco(
            compose(taskHandlers)
            ,hasTaskHandler
          )          
        ])
      )
      ,hasTaskHandler
    )
  ])
)(null,Model);
