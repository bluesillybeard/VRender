namespace VRenderLib.Implementation.GL33;

using Interface;

using StbImageSharp;

using vmodel;

using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;

using Threading;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Diagnostics.Tracing;

//Here is an explanation of how the command queue works, in text form:
// When Run() is called, a new thread is spawned called the "logic" thread - it does all of the game logic
// The main thread becomes the worker thread for the command queue
// since the main thread is required to do the windowing and OpenGL calls.
// The new thread is in change of calling events to the game logic, while the main thread's sole purpose is to interact with OpenGL.
// When an interaction with the window or OpenGL is required, a callback is placed into the command queue to be executed in order.
// It is worth mentioning that it is possible that if multiple threads are placing tasks into the queue,
// the OpenGL context could get mixed up and things get rendered incorrectly,
// so commands are self-containing and shouldn't rely on the OpenGL context to keep track of state.

// All functions that require an OpenGl context are run through the command queue, so synchronous ones block until that task has been finished.

public sealed class GL33Render : IRender
{
    //Must be run from main thread.
    public GL33Render(RenderSettings settings)
    {
        this.settings = settings;
        NativeWindowSettings windowSettings = new()
        {
            StartVisible = true,
            Title = settings.WindowTitle,
            Size = settings.size,
            API = ContextAPI.OpenGL,
            APIVersion = new Version(3, 3),
            AutoLoadBindings = true,
            Profile = ContextProfile.Core,
            Vsync = settings.VSync ? VSyncMode.On : VSyncMode.Off,
        };
        window = new NativeWindow(windowSettings);
        customShaders = new Dictionary<(string, string, Attributes), GL33Shader>();
        window.Resize += OnResize;
        mainThreadWaits = new AutoResetEvent(false);
        gameThreadWaits = new AutoResetEvent(false);
        lowTasks = new ConcurrentQueue<ExecutorTask>();
        normalTasks = new ConcurrentQueue<ExecutorTask>();
        priorityTasks = new ConcurrentQueue<ExecutorTask>();
        freeCommandQueues = new List<GL33DrawCommandQueue>();
        //TODO: redo models and mesh generation to support winding-based face culling
        //GL.Enable(EnableCap.CullFace);
    }
     //Texture loading functions
    public IRenderTexture LoadTexture(ImageResult image)
    {
        return LoadTexture(image, false);
    }
    public IRenderTexture LoadTexture(ImageResult image, bool dynamic)
    {
        if(Environment.CurrentManagedThreadId == 1)
        {
            return new GL33Texture(image);
        }
        var task = LoadTextureAsync(image, dynamic);
        task.WaitUntilDone();
        #nullable disable
        return task.GetResult();
        #nullable enable
    }
    public IRenderTexture? LoadTexture(string path, out Exception? error)
    {
        return LoadTexture(path, false, out error);
    }
    public IRenderTexture? LoadTexture(string path, bool dynamic, out Exception? error)
    {
        if(Environment.CurrentManagedThreadId == 1)
        {
            var raw = LoadTextureRaw(path, dynamic);
            error = raw.error;
            return raw.Item1;
        }
        var task = LoadTextureAsync(path, dynamic);
        task.WaitUntilDone();
        var result = task.GetResult();
        error = result.error;
        return result.Item1;
    }

    //asynchronous texture loading functions
    public ExecutorTask<IRenderTexture> LoadTextureAsync(ImageResult image)
    {
        return LoadTextureAsync(image, false);
    }
    //C# Tuple FTW
    public ExecutorTask<IRenderTexture> LoadTextureAsync(ImageResult image, bool dynamic)
    {
        return SubmitToQueue(() => {
            //I need to cast it here since C# doesn't like it when I cast objects with generics.
            return (IRenderTexture)new GL33Texture(image);
        }, "LoadTexture");
    }
    public ExecutorTask<(IRenderTexture?, Exception? error)> LoadTextureAsync(string path)
    {
        return LoadTextureAsync(path, false);
    }
    public ExecutorTask<(IRenderTexture?, Exception? error)> LoadTextureAsync(string path, bool dynamic)
    {
        return SubmitToQueue(() => LoadTextureRaw(path, dynamic), "LoadTexture " + path);
    }

    private static (IRenderTexture?, Exception? error) LoadTextureRaw(string path, bool dynamic)
    {
        try{
            ImageResult img = ImageResult.FromStream(new FileStream(path, FileMode.Open));
            return ((IRenderTexture) new GL33Texture(img), null);
        } catch (Exception e)
        {
            return (null, e);
        }
    }

    //mesh loading functions
    public IRenderMesh LoadMesh(VMesh mesh)
    {
        return LoadMesh(mesh, false);
    }
    public IRenderMesh LoadMesh(VMesh mesh, bool dynamic)
    {
        if(Environment.CurrentManagedThreadId == 1)
        {
            return new GL33Mesh(mesh, dynamic);
        }
        var task = LoadMeshAsync(mesh, dynamic);
        task.WaitUntilDone();
        #nullable disable
        return task.GetResult();
        #nullable restore
    }
    public IRenderMesh? LoadMesh(string vmeshPath, out Exception? error)
    {
        return LoadMesh(vmeshPath, out error, false);
    }
    public IRenderMesh? LoadMesh(string vmeshPath, out Exception? error, bool dynamic)
    {
        if(Environment.CurrentManagedThreadId == 1)
        {
            VMesh? mesh = VModelUtils.LoadMesh(vmeshPath, out error);
            if(mesh is null){
                return null;
            }
            return (IRenderMesh) new GL33Mesh(mesh.Value, dynamic);
        }
        var task = LoadMeshAsync(vmeshPath, dynamic);
        task.WaitUntilDone();
        var result = task.GetResult();
        error = result.error;
        return result.Item1;
    }

    //async mesh loading functions
    public ExecutorTask<IRenderMesh> LoadMeshAsync(VMesh mesh)
    {
        return LoadMeshAsync(mesh, false);
    }
    public ExecutorTask<IRenderMesh> LoadMeshAsync(VMesh mesh, bool dynamic)
    {
        return SubmitToQueue(() => (IRenderMesh)new GL33Mesh(mesh, dynamic), "LoadMesh");
    }
    public ExecutorTask<(IRenderMesh?, Exception? error)> LoadMeshAsync(string vmeshPath)
    {
        return LoadMeshAsync(vmeshPath, false);
    }
    public ExecutorTask<(IRenderMesh?, Exception? error)> LoadMeshAsync(string vmeshPath, bool dynamic)
    {
        return SubmitToQueue<(IRenderMesh?, Exception? error)>( ()=>{
            VMesh? mesh = VModelUtils.LoadMesh(vmeshPath, out var error);
            if(mesh is null){
                return (null, error);
            }
            return (new GL33Mesh(mesh.Value, dynamic), null);
        }, "LoadMesh");
    }

    public IRenderShader GetShader(string GLSLVertexCode, string GLSLFragmentCode, Attributes attributes)
    {
        if(Environment.CurrentManagedThreadId == 1)
        {
            var shader = new GL33Shader(GLSLFragmentCode, GLSLVertexCode, attributes);
            customShaders.Add((GLSLVertexCode, GLSLFragmentCode, attributes), shader);
            return (IRenderShader) shader;
        }
        var task = GetShaderAsync(GLSLVertexCode, GLSLFragmentCode, attributes);
        task.WaitUntilDone();
        var result = task.GetResult() ?? throw new Exception("Error compiling shader:\nGLSL Vertex:" + GLSLVertexCode + "\nGLSL Fragment:" + GLSLFragmentCode + "]nAttributes:" + attributes, task.GetException());
        return result;
    }

    public ExecutorTask<IRenderShader> GetShaderAsync(string GLSLVertexCode, string GLSLFragmentCode, Attributes attributes)
    {
        if(customShaders.TryGetValue((GLSLVertexCode, GLSLFragmentCode, attributes), out var shader))
        {
            //Make sure the shader hasn't been deleted
            if(!shader.IsDisposed())
            {
                //Since they want a task, we return a task that is already done
                var t = new ExecutorTask<IRenderShader>(shader, "GetShader");
                return t;
            }
        }
        return SubmitToQueue(() => {
            var shader = new GL33Shader(GLSLFragmentCode, GLSLVertexCode, attributes);
            customShaders.Add((GLSLVertexCode, GLSLFragmentCode, attributes), shader);
            return (IRenderShader) shader;
        }, "GetShader");
    }

    public RenderModel LoadModel(VModel model)
    {
        if(Environment.CurrentManagedThreadId == 1)
        {
            return LoadModelRaw(model, false);
        }
        var task = LoadModelAsync(model);
        task.WaitUntilDone();
        return task.GetResult();
    }
    public RenderModel? LoadModel(string vmfPath, out List<VError>? errors)
    {
        if(Environment.CurrentManagedThreadId == 1)
        {
            VModel? model = VModelUtils.LoadModel(vmfPath, out errors);
            if(model is null)
            {
                return null;
            }
            return LoadModelRaw(model.Value, false);
        }
        var task = LoadModelAsync(vmfPath);
        task.WaitUntilDone();
        var res = task.GetResult();
        errors = res.errors;
        return res.Item1;
    }

    public ExecutorTask<RenderModel> LoadModelAsync(VModel model)
    {
        return SubmitToQueue(() => LoadModelRaw(model, false), "LoadModel");
    }

    private static RenderModel LoadModelRaw(VModel model, bool dynamic)
    {
        var mesh = new GL33Mesh(model.mesh, dynamic);
        var texture = new GL33Texture(model.texture);
        return new RenderModel(mesh, texture);
    }
    public ExecutorTask<(RenderModel?, List<VError>? errors)> LoadModelAsync(string vmfPath)
    {
        return SubmitToQueue<(RenderModel?, List<VError>? errors)>(()=>{
            VModel? model = VModelUtils.LoadModel(vmfPath, out var errors);
            if(model is null)
            {
                return (null, errors);
            }
            return (LoadModelRaw(model.Value, false), null);
        }, "LoadModel " + vmfPath);
    }

    //System stuff
    public KeyboardState Keyboard()
    {
        return window.KeyboardState;
    }

    public MouseState Mouse()
    {
        return window.MouseState;
    }

    public string GetClipboard()
    {
        return window.ClipboardString;
    }
    public void SetClipboard(string clip)
    {
        window.ClipboardString = clip;
    }

    public CursorState CursorState{
        get
        {
            return window.CursorState;
        }
        set
        {
            window.CursorState = value;
        }
    }

    public Vector2i WindowSize()
    {
        return window.Size;
    }

    public RenderType GetRenderType()
    {
        return RenderType.GL33;
    }

    //Events
    public void Run()
    {
        //This is the method where things get WhaCkyY
        //First we create the game thread
        gameThread = new Thread(GameThreadMain);
        gameThread.Start();
        //The main thread goes into its little hidey hole known as the "render queue"
        window.MakeCurrent();
        MainThreadMain();
    }

    private GL33DrawCommandQueue GetFreeDrawCommandQueue()
    {
        lock(freeCommandQueues)
        {
            if(freeCommandQueues.Count > 0)
            {
                var queue = freeCommandQueues[0];
                freeCommandQueues.RemoveAt(0);
                return queue;
            }
            return new GL33DrawCommandQueue();
        }
    }
    private void GameThreadMain()
    {
        Thread.CurrentThread.Name = "Game";
        OnStart?.Invoke();
        //The game thread takes over the majority of operations
        // while the main thread spends the rest of its life in a pit of despair
        // doing endless boring repetitive tasks
        Stopwatch frameTimer = new();
        frameTimer.Start();
        Stopwatch updateTimer = new();
        updateTimer.Start();
        TimeSpan targetUpdateDelta = TimeSpan.FromSeconds(1.0/30.0);
        TimeSpan targetFrameDelta = TimeSpan.FromSeconds(settings.TargetFrameTime);
        while(!window.IsExiting)
        {
            var frameDelta = frameTimer.Elapsed;
            if(frameDelta > targetFrameDelta)
            {
                frameTimer.Restart();
                priorityTasksOnly = true;
                var drawCommandQueue = GetFreeDrawCommandQueue();
                OnDraw?.Invoke(frameDelta, drawCommandQueue);
                gameThreadWaits.WaitOne();
                SubmitToQueueHighPriority(() => {
                    drawCommandQueue.Process(window);
                    drawCommandQueue.Reset();
                    lock(freeCommandQueues) freeCommandQueues.Add(drawCommandQueue);
                    }, "DrawFrame");
                // TODO: possibly use queue to have multiple frames in process at once to increase performance
                priorityTasksOnly = false;
            }
            var updateElapsed = updateTimer.Elapsed;
            if(updateElapsed > targetUpdateDelta)
            {
                updateTimer.Restart();
                Update(updateElapsed);
            }
            Thread.Yield();
        }
        Dispose();
    }

    private void Update(TimeSpan delta)
    {
        var task = SubmitToQueueHighPriority(() => {
            window.ProcessInputEvents();
            NativeWindow.ProcessWindowEvents(false);
        }, "ProcessEvents");
        task.WaitUntilDone();
        OnUpdate?.Invoke(delta);
    }
    private bool priorityTasksOnly;
    private bool mainThreadRunning;
    private readonly AutoResetEvent mainThreadWaits;
    private readonly AutoResetEvent gameThreadWaits;
    private void MainThreadMain()
    {
        Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
        mainThreadRunning = true;
        try{
            while(mainThreadRunning)
            {
                if(priorityTasks.TryDequeue(out var task))
                {
                    task.Execute();
                }
                else if(!priorityTasksOnly && normalTasks.TryDequeue(out task))
                {
                    task.Execute();
                }
                else if(!priorityTasksOnly && lowTasks.TryDequeue(out task))
                {
                    task.Execute();
                }
                else
                {
                    //There was no task. ):
                    gameThreadWaits.Set();
                    mainThreadWaits.WaitOne(1000);
                }
            }
        } catch (Exception e)
        {
            System.Console.Error.WriteLine("Exception:" + e.Message + "\n" + e.StackTrace);
        }
    }

    public Action<TimeSpan>? OnUpdate {get;set;}
    public Action<TimeSpan, IDrawCommandQueue>? OnDraw {get;set;}
    public Action? OnStart {get;set;}
    public Action? OnCleanup {get;set;}
    //TODO: IO events
    public Action<KeyboardKeyEventArgs>? OnKeyDown {get; set;}
    public Action<KeyboardKeyEventArgs>? OnKeyUp {get; set;}
    public Action<MouseButtonEventArgs>? OnMouseDown {get; set;}
    public Action<MouseButtonEventArgs>? OnMouseUp {get; set;}
    public Action<ResizeEventArgs>? OnWindowResize {get; set;}

    private void OnResize(ResizeEventArgs args)
    {
        OnWindowResize?.Invoke(args);
        GL.Viewport(0, 0, args.Width, args.Height);
    }

    public void Dispose()
    {
        if(OnCleanup is not null)OnCleanup();
        var destroyWindowTask = SubmitToQueue(() => window.Dispose(), "DisposeRender");
        destroyWindowTask.WaitUntilDone();
        mainThreadRunning = false;
        mainThreadWaits.Set();
        gameThreadWaits.Set();
        gameThreadWaits.Dispose();
        mainThreadWaits.Dispose();
        disposed = true;
    }
    public bool IsDisposed()
    {
        return disposed;
    }

    public ExecutorTask SubmitToQueue(Action task, string name)
    {
        return SubmitToQueue(task, normalTasks, name);
    }

    public ExecutorTask SubmitToQueueHighPriority(Action task, string name)
    {
        return SubmitToQueue(task, priorityTasks, name);
    }
    public ExecutorTask SubmitToQueueLowPriority(Action task, string name)
    {
        return SubmitToQueue(task, lowTasks, name);
    }
    public ExecutorTask SubmitToQueue(Action task, ConcurrentQueue<ExecutorTask> queue, string name)
    {
        var t = new ExecutorTask(task, name);
        queue.Enqueue(t);
        mainThreadWaits.Set();
        return t;
    }

    public ExecutorTask<TResult> SubmitToQueue<TResult>(Func<TResult> task, string name)
    {
        return SubmitToQueue(task, normalTasks, name);
    }

    public ExecutorTask<TResult> SubmitToQueueHighPriority<TResult>(Func<TResult> task, string name)
    {
        return SubmitToQueue(task, priorityTasks, name);
    }
    public ExecutorTask<TResult> SubmitToQueueLowPriority<TResult>(Func<TResult> task, string name)
    {
        return SubmitToQueue(task, lowTasks, name);
    }
    public ExecutorTask<TResult> SubmitToQueue<TResult>(Func<TResult> task, ConcurrentQueue<ExecutorTask> queue, string name)
    {
        var t = new ExecutorTask<TResult>(task, name);
        queue.Enqueue(t);
        mainThreadWaits.Set();
        return t;
    }
    private bool disposed = false;

    private readonly NativeWindow window;
    private readonly Dictionary<(string, string, Attributes), GL33Shader> customShaders;

    private Thread? gameThread;
    private readonly ConcurrentQueue<ExecutorTask> lowTasks;
    private readonly ConcurrentQueue<ExecutorTask> normalTasks;
    private readonly ConcurrentQueue<ExecutorTask> priorityTasks;
    private RenderSettings settings;
    private readonly List<GL33DrawCommandQueue> freeCommandQueues;
}
