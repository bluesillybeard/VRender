namespace VRenderLib.Threading;

public class ExecutorTask
{
    public ExecutorTask(Action? task, string name)
    {
        this.task = task;
        this.name = name;
    }

    public ExecutorTask(Action? task, bool completed, string name)
    {
        this.task = task;
        this.completed = completed;
        this.name = name;
    }
    /**
    <summary>
        Runs the task on the thread calling this function,
        Or waits for it to finish if it is already running.
    </summary>
    */
    public void Execute()
    {
        //We want to be thread-safe if possible.
        //mutex.WaitOne(); //For now we don't use it, because booleans might already be thread safe.
        //It's already done, so we can just return.
        if(completed)return;
        //I can't just run the task, because it might already be running.
        if(running)
        {
            //If it is already running, wait for it to complete
            while(running)
            {
                Thread.Sleep(0);
            }
            return;
        }
        //We've verified that it's not running or already done, so we can just run it.
        running = true;
        try{
            if(task is not null) task.Invoke();
        } catch (Exception e)
        {
            exception = e;
        }
        completed = true;
        running = false;
    }

    public void WaitUntilDone()
    {
        while(!completed)
        {
            Thread.Yield();
            Thread.Sleep(0);
        }
    }

    public Exception? GetException()
    {
        return exception;
    }
    //The task to run
    protected Action? task;
    //Weather the task has finished
    private bool completed;
    //true if the task is actively running
    private bool running;
    //Any exception that was thrown by the task
    private Exception? exception;
    //This is to make debugging eaiser; all task require a name so I can more easily track them.
    public string name;
}

public sealed class ExecutorTask<TResult> : ExecutorTask
{

    public ExecutorTask(Func<TResult>? resultTask, string name)
    : base(null, name) //C# drives me nuts sometimes, there has to be a better way to do this
    {
        if(resultTask is null){
            base.task = null;
            return;
        }
        base.task = () => {RunTask(resultTask, out result);};
    }

    public ExecutorTask(Func<TResult>? resultTask, bool completed, string name)
    : base(null, completed, name) //C# drives me nuts sometimes, there has to be a better way to do this
    {
        if(resultTask is null){
            base.task = null;
            return;
        }
        base.task = () => {RunTask(resultTask, out result);};
    }
    /**
    <summary>
        For creating a task that is already finished
    </summary>
    */
    public ExecutorTask(TResult? result, string name)
    : base(null, true, name)
    {
        this.result = result;
    }
    public TResult? GetResult()
    {
        return result;
    }
    private static void RunTask(Func<TResult> resultTask, out TResult result)
    {
        result = resultTask.Invoke();
    }
    private TResult? result;
}