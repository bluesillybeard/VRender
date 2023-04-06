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
        tasks = new PriorityQueue<ExecutorTask, (int, DateTime)>();
        running = false;
        executorWaits = new AutoResetEvent(false);
        outsiderWaits = new ManualResetEvent(true);
    }

    //The thread that will execute the tasks should call this method.
    // It blocks until the executor stops.
    public void Run()
    {
        running = true;
        ExecutorTask? task;
        while(running)
        {
            bool hadTask;
            (int, DateTime) priority;
            lock(tasks){
                hadTask = tasks.TryDequeue(out task, out priority);
            }
            if(hadTask && task is not null)
            {
                task.Execute();
            }
            if(!hadTask)
            {
                outsiderWaits.Set();
                executorWaits.WaitOne(100);
            }
        }
}

    public void Stop()
    {
        running = false;
        outsiderWaits.Dispose();
        executorWaits.Dispose();
    }
    //NOTE: Priority queue isn't thread safe. That is why there are lock statements everywhere.
    private PriorityQueue<ExecutorTask, (int, DateTime)> tasks;
    volatile private bool running;
    private EventWaitHandle executorWaits;
    private EventWaitHandle outsiderWaits;
    public ExecutorTask QueueTask(Action? task, int priority, string name)
    {
        var t = new ExecutorTask(task, name);
        lock(tasks)this.tasks.Enqueue(t, (priority, DateTime.Now));
        executorWaits.Set();
        outsiderWaits.Reset();
        return t;
    }

    public ExecutorTask<TResult> QueueTask<TResult>(Func<TResult>? task, int priority, string name)
    {
        var t = new ExecutorTask<TResult>(task, name);
        lock(tasks)this.tasks.Enqueue(t, (priority, DateTime.Now));
        //Tell the main thread that there is a new task to do.
        executorWaits.Set();
        outsiderWaits.Reset();
        return t;
    }

    public void WaitUntilQueueEmpty()
    {
        outsiderWaits.WaitOne();
    }
}
