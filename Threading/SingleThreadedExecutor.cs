namespace VRender.Threading;

using System.Collections.Concurrent;

//As it turns out, C# has zero equivalent to Java's ExecutorService.
// So, I had to make my own.

// This one simply uses a single thread. 
// the run() method starts the queue and will block until the stop() method is called by a different thread.

public sealed class SingleThreadedExecutor
{
    public SingleThreadedExecutor()
    {
        tasks = new ConcurrentQueue<ExecutorTask>();
        running = false;
    }

    //The thread that will execute the tasks should call this method.
    // It blocks until the executor stops.
    public void Run()
    {
        running = true;
        while(running)
        {
            if(tasks.TryDequeue(out var task))
            {
                try{
                    task.Execute();
                    //System.Console.WriteLine("Executed a task");
                } catch (Exception e)
                {
                    System.Console.Error.WriteLine(e.Message);
                }
            }
            Thread.Sleep(0);
            
        }
    }

    public void Stop()
    {
        running = false;
    }
    private ConcurrentQueue<ExecutorTask> tasks;
    volatile private bool running;

    public IEnumerable<ExecutorTask> GetScheduledTasks()
    {
        return tasks.AsEnumerable();
    }
    public ExecutorTask QueueTask(Action task)
    {
        var t = new ExecutorTask(task);
        this.tasks.Enqueue(t);
        return t;
    }

    public ExecutorTask<TResult> QueueTask<TResult>(Func<TResult> task)
    {
        var t = new ExecutorTask<TResult>(task);
        this.tasks.Enqueue(t);
        return t;
    }

    public void WaitUntilQueueEmpty()
    {
        while(!tasks.IsEmpty)
        {
            Thread.Sleep(0);
        }
    }
}
