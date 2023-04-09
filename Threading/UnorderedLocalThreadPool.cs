namespace VRenderLib.Threading;

using System.Collections.Concurrent;
//As it turns out, C# has zero equivalent to Java's ExecutorService.
// So, I had to make my own.

//"Why not use ThreadPool or Task.Run?"
// I don't have full control over those, and they are global systems.
// I want a single thread pool that has one purpose and can be controlled at an individual level.
// Honestly, the fact that C# doesn't even have an option for this is midly frustrating.
// But it's not too bad; I've only completely gone insane and lost my mind over it. Twice

/**
<summary>
    Allows a programmer to create a pool of threads.
    Unlike C#'s built-in ThreadPool class, this can be customized to a great degree,
    And it's called "Local" because it's not supposed to be used as a singleton,
    but rather as a pool of threads to be used for one task.

    This thread pool will execute tasks in an arbitrary order.
</summary>
*/

public sealed class UnorderedLocalThreadPool
{
    AutoResetEvent poolThreadWaits;
    ConcurrentStack<ExecutorTask> tasks;
    Thread[] threads;
    bool running;

    public UnorderedLocalThreadPool(int numThreads)
    {
        poolThreadWaits = new AutoResetEvent(false);
        tasks = new ConcurrentStack<ExecutorTask>();
        threads = new Thread[numThreads];
        running = true;
        for(int i=0; i<threads.Length; i++)
        {
            Thread poolThread = new Thread(PoolThreadMain);
            poolThread.Name = "UnorderedLocalPoolThread" + i;
            threads[i] = poolThread;
            poolThread.Start();
        }
    }

    public UnorderedLocalThreadPool() : this(Environment.ProcessorCount){}

    private void PoolThreadMain()
    {
        while(running)
        {
            if(tasks.TryPop(out var task))
            {
                task.Execute();
            }
            else
            {
                poolThreadWaits.WaitOne(100);
            }
            
        }
    }

    public ExecutorTask SubmitTask(Action? func, string name)
    {
        ExecutorTask task = new ExecutorTask(func, name);
        tasks.Push(task);
        poolThreadWaits.Set();
        return task;
    }

    public ExecutorTask<TResult> SubmitTask<TResult>(Func<TResult>? func, string name)
    {
        ExecutorTask<TResult> task = new ExecutorTask<TResult>(func, name);
        tasks.Push(task);
        poolThreadWaits.Set();
        return task;
    }

    public IEnumerable<ExecutorTask> Stop()
    {
        running = false;
        foreach(Thread t in threads)
        {
            t.Join();
        }
        poolThreadWaits.Dispose();
        return tasks;
    }
}
// public sealed class SingleThreadedExecutor
// {
//     public SingleThreadedExecutor()
//     {
//         tasks = new PriorityQueue<ExecutorTask, (int, DateTime)>();
//         running = false;
//         executorWaits = new AutoResetEvent(false);
//         outsiderWaits = new ManualResetEvent(true);
//     }

//     //The thread that will execute the tasks should call this method.
//     // It blocks until the executor stops.
//     public void Run()
//     {
//         running = true;
//         ExecutorTask? task;
//         while(running)
//         {
//             bool hadTask;
//             (int, DateTime) priority;
//             lock(tasks){
//                 hadTask = tasks.TryDequeue(out task, out priority);
//             }
//             if(hadTask && task is not null)
//             {
//                 task.Execute();
//             }
//             if(!hadTask)
//             {
//                 outsiderWaits.Set();
//                 executorWaits.WaitOne(100);
//             }
//         }
// }

//     public void Stop()
//     {
//         running = false;
//         outsiderWaits.Dispose();
//         executorWaits.Dispose();
//     }
//     //NOTE: Priority queue isn't thread safe. That is why there are lock statements everywhere.
//     private PriorityQueue<ExecutorTask, (int, DateTime)> tasks;
//     volatile private bool running;
//     private EventWaitHandle executorWaits;
//     private EventWaitHandle outsiderWaits;
//     public ExecutorTask QueueTask(Action? task, int priority, string name)
//     {
//         var t = new ExecutorTask(task, name);
//         lock(tasks)this.tasks.Enqueue(t, (priority, DateTime.Now));
//         executorWaits.Set();
//         outsiderWaits.Reset();
//         return t;
//     }

//     public ExecutorTask<TResult> QueueTask<TResult>(Func<TResult>? task, int priority, string name)
//     {
//         var t = new ExecutorTask<TResult>(task, name);
//         lock(tasks)this.tasks.Enqueue(t, (priority, DateTime.Now));
//         //Tell the main thread that there is a new task to do.
//         executorWaits.Set();
//         outsiderWaits.Reset();
//         return t;
//     }

//     public void WaitUntilQueueEmpty()
//     {
//         outsiderWaits.WaitOne();
//     }
// }
