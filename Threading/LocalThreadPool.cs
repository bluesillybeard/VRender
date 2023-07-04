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

    This thread pool will execute tasks in the same order that they are enqueued.
</summary>
*/

public sealed class LocalThreadPool
{
    AutoResetEvent poolThreadWaits;
    ConcurrentQueue<ExecutorTask> tasks;
    Thread[] threads;
    bool running;
    bool paused;
    uint tasksRunning = 0;

    public LocalThreadPool(int numThreads)
    {
        poolThreadWaits = new AutoResetEvent(false);
        tasks = new ConcurrentQueue<ExecutorTask>();
        threads = new Thread[numThreads];
        running = true;
        paused = false;
        for(int i=0; i<threads.Length; i++)
        {
            Thread poolThread = new Thread(PoolThreadMain);
            poolThread.Name = "LocalPoolThread" + i;
            threads[i] = poolThread;
            poolThread.Start();
        }
    }

    public LocalThreadPool() : this(Environment.ProcessorCount){}

    private void PoolThreadMain()
    {
        while(running)
        {
            if(!paused && tasks.TryDequeue(out var task))
            {
                Interlocked.Increment(ref tasksRunning);
                try{
                    task.Execute();
                } catch(Exception e)
                {
                    System.Console.Error.WriteLine("Pool thread got an exception:" + e.Message + "\n" + e.StackTrace);
                }
                Interlocked.Decrement(ref tasksRunning);
            }
            else
            {
                poolThreadWaits.WaitOne(100);
            }
            
        }
    }

    public void Pause()
    {
        paused = true;
        //We want to wait for all of the currently running tasks to finish.
        while(tasksRunning > 0)
        {
            Thread.Yield();
        }
    }

    public void Unpause()
    {
        paused = false;
        poolThreadWaits.Set();
    }

    public ExecutorTask SubmitTask(Action? func, string name)
    {
        ExecutorTask task = new ExecutorTask(func, name);
        tasks.Enqueue(task);
        poolThreadWaits.Set();
        return task;
    }

    public ExecutorTask<TResult> SubmitTask<TResult>(Func<TResult>? func, string name)
    {
        ExecutorTask<TResult> task = new ExecutorTask<TResult>(func, name);
        tasks.Enqueue(task);
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
