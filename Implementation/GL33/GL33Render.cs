namespace VRender.Implementation.GL33;

using Interface;

using StbImageSharp;

using vmodel;

using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;

using Threading;
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


public class GL33Render : IRender
{
    //Must be run from main thread.
    public GL33Render(RenderSettings settings)
    {
        this.settings = settings;
        NativeWindowSettings windowSettings = new NativeWindowSettings()
        {
            StartVisible = true,
            Title = settings.WindowTitle,
            Size = settings.size,
            API = ContextAPI.OpenGL,
            APIVersion = new Version(3, 3),
            AutoLoadBindings = true,
            Profile = ContextProfile.Core,
        };
        window = new NativeWindow(windowSettings);
        mainThread = new SingleThreadedExecutor();
        featuredShaders = new Dictionary<ShaderFeatures, GL33Shader>();
        customShaders = new Dictionary<(string, string, Attributes), GL33Shader>();
        window.Resize += OnResize;
        window.MakeCurrent();
        GLFW.SwapInterval(settings.VSync ? 1 : 0);
    }
    //Texture loading functions
    
    public IRenderTexture LoadTexture(ImageResult image)
    {
        return LoadTexture(image, false);
    }
    public IRenderTexture LoadTexture(ImageResult image, int priority)
    {
        return LoadTexture(image, false, priority);
    }
    public IRenderTexture LoadTexture(ImageResult image, bool dynamic)
    {
        return LoadTexture(image, dynamic, IRender.DefaultPriority);
    }
    public IRenderTexture LoadTexture(ImageResult image, bool dynamic, int priority)
    {
        if(Environment.CurrentManagedThreadId == 1)
        {
            //We are on the main thread, so we do this synchronously
            return (IRenderTexture)new GL33Texture(image);
        }
        var task = LoadTextureAsync(image, dynamic, priority);
        task.WaitUntilDone();
        #nullable disable
        return task.GetResult();
        #nullable enable
    }
    public IRenderTexture? LoadTexture(string path, out Exception? error)
    {
        return LoadTexture(path, false, out error);
    }
    public IRenderTexture? LoadTexture(string path, out Exception? error, int priority)
    {
        return LoadTexture(path, false, out error, priority); 
    }
    public IRenderTexture? LoadTexture(string path, bool dynamic, out Exception? error)
    {
        return LoadTexture(path, dynamic, out error, IRender.DefaultPriority);
    }
    public IRenderTexture? LoadTexture(string path, bool dynamic, out Exception? error, int priority)
    {
        if(Environment.CurrentManagedThreadId == 1)
        {
            //We are on the main thread, so we do this synchronously
            var syncResult = LoadTextureRaw(path, dynamic);
            error = syncResult.error;
            return syncResult.Item1;
        }
        var task = LoadTextureAsync(path, dynamic, priority);
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
    public ExecutorTask<IRenderTexture> LoadTextureAsync(ImageResult image, int priority)
    {
        return LoadTextureAsync(image, false, priority);
    }
    public ExecutorTask<IRenderTexture> LoadTextureAsync(ImageResult image, bool dynamic)
    {
        return LoadTextureAsync(image, dynamic, IRender.DefaultPriority);
    }
    public ExecutorTask<IRenderTexture> LoadTextureAsync(ImageResult image, bool dynamic, int priority)
    {
        return SubmitToQueue(() => {
            //I need to cast it here since C# doesn't like it when I cast objects with generics.
            return (IRenderTexture)new GL33Texture(image);
        }, priority, "LoadTexture");
    }
    public ExecutorTask<(IRenderTexture?, Exception? error)> LoadTextureAsync(string path)
    {
        return LoadTextureAsync(path, false);
    }
    public ExecutorTask<(IRenderTexture?, Exception? error)> LoadTextureAsync(string path, int priority)
    {
        return LoadTextureAsync(path, false, priority);
    }
    public ExecutorTask<(IRenderTexture?, Exception? error)> LoadTextureAsync(string path, bool dynamic)
    {
        return LoadTextureAsync(path, dynamic, IRender.DefaultPriority);
    }
    public ExecutorTask<(IRenderTexture?, Exception? error)> LoadTextureAsync(string path, bool dynamic, int priority)
    {
        return SubmitToQueue( () => {
            return LoadTextureRaw(path, dynamic);
        }, priority, "LoadTexture\"" + path + "\"");
    }

    private (IRenderTexture?, Exception? error) LoadTextureRaw(string path, bool dynamic)
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
    public IRenderMesh LoadMesh(VMesh mesh, int priority)
    {
        return LoadMesh(mesh, false, priority);
    }
    public IRenderMesh LoadMesh(VMesh mesh, bool dynamic)
    {
        return LoadMesh(mesh, dynamic, IRender.DefaultPriority);
    }
    public IRenderMesh LoadMesh(VMesh mesh, bool dynamic, int priority)
    {
        var task = LoadMeshAsync(mesh, dynamic, priority);
        task.WaitUntilDone();
        #nullable disable
        return task.GetResult();
        #nullable restore
    }
    public IRenderMesh? LoadMesh(string vmeshPath, out Exception? error)
    {
        return LoadMesh(vmeshPath, out error, false);
    }
    public IRenderMesh? LoadMesh(string vmeshPath, out Exception? error, int priority)
    {
        return LoadMesh(vmeshPath, out error, false, priority);
    
    }
    public IRenderMesh? LoadMesh(string vmeshPath, out Exception? error, bool dynamic)
    {
        return LoadMesh(vmeshPath, out error, dynamic, IRender.DefaultPriority);
    }
    public IRenderMesh? LoadMesh(string vmeshPath, out Exception? error, bool dynamic, int priority)
    {
        var task = LoadMeshAsync(vmeshPath, dynamic, priority);
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
    public ExecutorTask<IRenderMesh> LoadMeshAsync(VMesh mesh, int priority)
    {
        return LoadMeshAsync(mesh, false, priority);
    }
    public ExecutorTask<IRenderMesh> LoadMeshAsync(VMesh mesh, bool dynamic)
    {
        return LoadMeshAsync(mesh, dynamic, IRender.DefaultPriority);
    }
    public ExecutorTask<IRenderMesh> LoadMeshAsync(VMesh mesh, bool dynamic, int priority)
    {
        return SubmitToQueue( ()=> {
            return (IRenderMesh) new GL33Mesh(mesh, dynamic);
        }, priority, "LoadMesh");
    }
    public ExecutorTask<(IRenderMesh?, Exception? error)> LoadMeshAsync(string vmeshPath)
    {
        return LoadMeshAsync(vmeshPath, false);
    }
    public ExecutorTask<(IRenderMesh?, Exception? error)> LoadMeshAsync(string vmeshPath, int priority)
    {
        return LoadMeshAsync(vmeshPath, false, priority);
    }
    public ExecutorTask<(IRenderMesh?, Exception? error)> LoadMeshAsync(string vmeshPath, bool dynamic)
    {
        return LoadMeshAsync(vmeshPath, dynamic, IRender.DefaultPriority);
    }
    public ExecutorTask<(IRenderMesh?, Exception? error)> LoadMeshAsync(string vmeshPath, bool dynamic, int priority)
    {
        return SubmitToQueue<(IRenderMesh?, Exception? error)>( ()=>{
            VMesh? mesh = VModelUtils.LoadMesh(vmeshPath, out var error);
            if(mesh is null){
                return (null, error);
            }
            return ((IRenderMesh) new GL33Mesh(mesh.Value, dynamic), null);
        }, priority, "LoadMesh\"" + vmeshPath + "\"");
    }
    public IRenderShader GetShader(ShaderFeatures features)
    {
        return GetShader(features, IRender.DefaultPriority);
    }

    public IRenderShader GetShader(ShaderFeatures features, int priority)
    {
        var task = GetShaderAsync(features, priority);
        task.WaitUntilDone();
        var result = task.GetResult();
        if(result is null) throw new Exception("Error creating shader from shader features", task.GetException());
        return result;
    }
    public IRenderShader GetShader(string GLSLVertexCode, string GLSLFragmentCode, Attributes attributes)
    {
        return GetShader(GLSLVertexCode, GLSLFragmentCode, attributes, IRender.DefaultPriority);
    }
    public IRenderShader GetShader(string GLSLVertexCode, string GLSLFragmentCode, Attributes attributes, int priority)
    {
        var task = GetShaderAsync(GLSLVertexCode, GLSLFragmentCode, attributes, priority);
        task.WaitUntilDone();
        var result = task.GetResult();
        if(result is null) throw new Exception("Error compiling shader:\nGLSL Vertex:" + GLSLVertexCode + "\nGLSL Fragment:" + GLSLFragmentCode + "]nAttributes:" + attributes, task.GetException());
        return result;
    }
    public ExecutorTask<IRenderShader> GetShaderAsync(ShaderFeatures features)
    {
        return GetShaderAsync(features, IRender.DefaultPriority);
    }
    public ExecutorTask<IRenderShader> GetShaderAsync(ShaderFeatures features, int priority)
    {
        //Return any shader that already exists
        if(featuredShaders.TryGetValue(features, out var shader))
        {
            //Make sure the shader hasn't been deleted
            if(!shader.IsDisposed())
            {
                //Since they want a task, we return a task that is already done
                var t = new ExecutorTask<IRenderShader>(shader, "");
                return t;
            }
        }
        return SubmitToQueue(() => {
            var shader = new GL33Shader(features);
            featuredShaders.Add(features, shader);
            return (IRenderShader) shader;
        }, priority, "CustomShader");
    }
    public ExecutorTask<IRenderShader> GetShaderAsync(string GLSLVertexCode, string GLSLFragmentCode, Attributes attributes)
    {
        return GetShaderAsync(GLSLVertexCode, GLSLFragmentCode, attributes, IRender.DefaultPriority);
    }
    public ExecutorTask<IRenderShader> GetShaderAsync(string GLSLVertexCode, string GLSLFragmentCode, Attributes attributes, int priority)
    {
        if(customShaders.TryGetValue((GLSLVertexCode, GLSLFragmentCode, attributes), out var shader))
        {
            //Make sure the shader hasn't been deleted
            if(!shader.IsDisposed())
            {
                //Since they want a task, we return a task that is already done
                var t = new ExecutorTask<IRenderShader>(shader, "");
                return t;
            }
        }
        return SubmitToQueue(() => {
            var shader = new GL33Shader(GLSLFragmentCode, GLSLVertexCode, attributes);
            customShaders.Add((GLSLVertexCode, GLSLFragmentCode, attributes), shader);
            return (IRenderShader) shader;
        }, priority, "CustomShaderGLSL");
    }
    public RenderModel LoadModel(VModel model)
    {
        return LoadModel(model, IRender.DefaultPriority);
    }
    public RenderModel LoadModel(VModel model, int priority)
    {
        var task = LoadModelAsync(model, priority);
        task.WaitUntilDone();
        return task.GetResult();
    }
    public RenderModel? LoadModel(string vmfPath, out List<VError>? errors)
    {
        return LoadModel(vmfPath, out errors, IRender.DefaultPriority);
    }
    public RenderModel? LoadModel(string vmfPath, out List<VError>? errors, int priority)
    {
        var task = LoadModelAsync(vmfPath, priority);
        task.WaitUntilDone();
        var res = task.GetResult();
        errors = res.errors;
        return res.Item1;
    }
    public ExecutorTask<RenderModel> LoadModelAsync(VModel model)
    {
        return LoadModelAsync(model, IRender.DefaultPriority);
    }

    public ExecutorTask<RenderModel> LoadModelAsync(VModel model, int priority)
    {
        return SubmitToQueue(()=>{
            return LoadModelRaw(model, false);
        }, priority, "LoadModel");
    }

    private RenderModel LoadModelRaw(VModel model, bool dynamic)
    {
        var mesh = new GL33Mesh(model.mesh, dynamic);
        var texture = new GL33Texture(model.texture);
        return new RenderModel(mesh, texture);
    }
    public ExecutorTask<(RenderModel?, List<VError>? errors)> LoadModelAsync(string vmfPath)
    {
        return LoadModelAsync(vmfPath, IRender.DefaultPriority);
    }
    public ExecutorTask<(RenderModel?, List<VError>? errors)> LoadModelAsync(string vmfPath, int priority)
    {
        return SubmitToQueue<(RenderModel?, List<VError>? errors)>(()=>{
            VModel? model = VModelUtils.LoadModel(vmfPath, out var errors);
            if(model is null)
            {
                return (null, errors);
            }
            return (LoadModelRaw(model.Value, false), null);
        }, priority, "LoadModel\"" + vmfPath + "\"");
    }

    //Rendering functionality
    public void BeginRenderQueue()
    {
        SubmitToQueue(() => {
            GL.ClearColor(0, 0, 0, 1);
            GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);

        }, -10, "BeginRender");
    }
    public void Draw(
        IRenderTexture texture, IRenderMesh mesh, IRenderShader shader,
        IEnumerable<KeyValuePair<string, object>> uniforms,
        bool depthTest
    ){
        SubmitToQueue(() => {
            DrawRaw(
                (GL33Texture)texture, (GL33Mesh)mesh, (GL33Shader)shader, uniforms, depthTest
            );
        }, -10, "Draw");
    }
    private void DrawRaw(
        GL33Texture texture, GL33Mesh mesh, GL33Shader shader,
        IEnumerable<KeyValuePair<string, object>> uniforms,
        bool depthTest
        )
        {
            //use objects
            texture.Use(OpenTK.Graphics.OpenGL.TextureUnit.Texture0);
            mesh.Use();
            shader.Use();
            //set uniforms
            foreach(var uniform in uniforms)
            {
                shader.SetUniform(uniform.Key, uniform.Value, out var error);
                if(error != null) System.Console.Error.WriteLine("Error setting shader uniform: " + error);
            }
            shader.SetInt("tex", 0, out _);
            //other GL variables
            if(depthTest) GL.Enable(EnableCap.DepthTest);
            else GL.Disable(EnableCap.DepthTest);
            GL.DrawElements(BeginMode.Triangles, (int)mesh.ElementCount()*3, DrawElementsType.UnsignedInt, 0);
        }
    public void EndRenderQueue()
    {
        SubmitToQueue(() => {
            window.Context.SwapBuffers();
        }, -10, "EndRender");
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

    public bool CursorLocked{get; set;}

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
        mainThread.Run();
    }

    private void GameThreadMain()
    {
        Thread.CurrentThread.Name = "Game";
        if(OnStart is not null)OnStart.Invoke();
        //The game thread takes over the majority of operations
        // while the main thread spends the rest of its life in a pit of despair
        // doing endless boring repetitive tasks
        DateTime loopTime;
        DateTime lastFrameTime = DateTime.Now;
        DateTime lastUpdateTime = DateTime.Now;
        TimeSpan targetUpdateDelta = TimeSpan.FromSeconds(1.0/30.0);
        TimeSpan targetFrameDelta = TimeSpan.FromSeconds(settings.TargetFrameTime);
        while(!window.IsExiting)
        {
            loopTime = DateTime.Now;
            if(loopTime - lastFrameTime > targetFrameDelta)
            {
                //mainThread.WaitUntilQueueEmpty();
                //Wait until drawing is complete.
                // Waiting until it's fully empty is rather useless.
                var waitTask = mainThread.QueueTask(null, -10, "RenderWait");
                waitTask.WaitUntilDone();
                if(OnDraw is not null)OnDraw.Invoke(loopTime - lastFrameTime);
                lastFrameTime = loopTime;
            }
            if(loopTime - lastUpdateTime > targetUpdateDelta)
            {
                Update(loopTime - lastUpdateTime);
                lastUpdateTime = loopTime;
            }
            Thread.Sleep(0);
        }
        Dispose();
    }

    private void Update(TimeSpan delta)
    {
        var task = SubmitToQueue(() => {
            window.ProcessInputEvents();
            NativeWindow.ProcessWindowEvents(false);
        }, 0);
        task.WaitUntilDone();
        if(OnUpdate is not null)OnUpdate.Invoke(delta);
    }

    public Action<TimeSpan>? OnUpdate {get;set;}
    public Action<TimeSpan>? OnDraw {get;set;}
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
        if(OnWindowResize is not null)OnWindowResize.Invoke(args);
        GL.Viewport(0, 0, args.Width, args.Height);
    }

    public void Dispose()
    {
        if(OnCleanup is not null)OnCleanup();
        var destroyWindowTask = SubmitToQueue(() => {
            window.Dispose();
        }, 10, "DisposeRender");
        destroyWindowTask.WaitUntilDone();
        mainThread.Stop();
        disposed = true;
    }
    public bool IsDisposed()
    {
        return disposed;
    }


    public ExecutorTask SubmitToQueue(Action task, int priority)
    {
        return mainThread.QueueTask(task, priority, "CustomTask");
    }

    public ExecutorTask SubmitToQueue(Action task, int priority, string name)
    {
        return mainThread.QueueTask(task, priority, name);
    }
    public ExecutorTask<TResult> SubmitToQueue<TResult>(Func<TResult> task, int priority)
    {
        return mainThread.QueueTask(task, priority, "CustomTask");
    }
    public ExecutorTask<TResult> SubmitToQueue<TResult>(Func<TResult> task, int priority, string name)
    {
        return mainThread.QueueTask(task, priority, name);
    }
    private SingleThreadedExecutor mainThread;
    private bool disposed = false;

    private NativeWindow window;

    private Dictionary<ShaderFeatures, GL33Shader> featuredShaders;
    private Dictionary<(string, string, Attributes), GL33Shader> customShaders;

    private Thread? gameThread;

    private RenderSettings settings;
}
