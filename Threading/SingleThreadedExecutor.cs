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
        executorWaits = new AutoResetEvent(false);
        outsiderWaits = new ManualResetEvent(true);
    }

    //The thread that will execute the tasks should call this method.
    // It blocks until the executor stops.
    public void Run()
    {
        running = true;
        while(running)
        {
            var hadTask = tasks.TryDequeue(out var task);
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
    private ConcurrentQueue<ExecutorTask> tasks;
    volatile private bool running;
    private EventWaitHandle executorWaits;
    private EventWaitHandle outsiderWaits;

    public IEnumerable<ExecutorTask> GetScheduledTasks()
    {
        return tasks.AsEnumerable();
    }
    public ExecutorTask QueueTask(Action task)
    {
        var t = new ExecutorTask(task);
        this.tasks.Enqueue(t);
        executorWaits.Set();
        outsiderWaits.Reset();
        return t;
    }

    public ExecutorTask<TResult> QueueTask<TResult>(Func<TResult> task)
    {
        var t = new ExecutorTask<TResult>(task);
        this.tasks.Enqueue(t);
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
