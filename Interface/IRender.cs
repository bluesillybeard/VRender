namespace VRender.Interface;

using Threading;

using StbImageSharp;

using vmodel;

using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Common;
using OpenTK.Mathematics;

public enum RenderType
{
    GL33
    //TODO: 'modern' OpenGL and Vulcan implementations.
}

/**
    IRender is the interface that all rendering backends ultimately implement.
    It is a minimal rendering API, meaning that it does not add a lot of complex visual functionality.
    Its main purpose is simply to provide a way to build a command queue and then actually draw the command queue,
    Similar to MonoGame's SpriteBatch class, except for 3D.
    It also provides the main loop, because it requires access to the main thread.

    Note that once Run() is called, the main thread is hidden away and all logic is done on a secondary thread.
    In other words, don't rely on the main thread.
*/
public interface IRender : IDisposable
{
    public const int DisposePriority = 10;
    public const int DefaultPriority = 0;
    public const int RenderPriority = -10;
    public static IRender CurrentRender => VRenderLib.Render;


    //Texture loading functions
    IRenderTexture LoadTexture(ImageResult image);
    IRenderTexture LoadTexture(ImageResult image, int priority);
    ///<summary>set the dynamic flag to true if the texture will be frequently modified.</summary>
    IRenderTexture LoadTexture(ImageResult image, bool dynamic);
    ///<summary>set the dynamic flag to true if the texture will be frequently modified.</summary>
    IRenderTexture LoadTexture(ImageResult image, bool dynamic, int priority);
    IRenderTexture? LoadTexture(string path, out Exception? error);
    IRenderTexture? LoadTexture(string path, out Exception? error, int priority);
    ///<summary>set the dynamic flag to true if it will be frequently modified.</summary>
    IRenderTexture? LoadTexture(string path, bool dynamic, out Exception? error);
    ///<summary>set the dynamic flag to true if it will be frequently modified.</summary>
    IRenderTexture? LoadTexture(string path, bool dynamic, out Exception? error, int priority);

    //asynchronous texture loading functions
    ExecutorTask<IRenderTexture> LoadTextureAsync(ImageResult image);
    ExecutorTask<IRenderTexture> LoadTextureAsync(ImageResult image, int priority);
    //C# Tuple FTW
    ///<summary>set the dynamic flag to true if the texture will be frequently modified.</summary>
    ExecutorTask<IRenderTexture> LoadTextureAsync(ImageResult image, bool dynamic);
    ///<summary>set the dynamic flag to true if the texture will be frequently modified.</summary>

    ExecutorTask<IRenderTexture> LoadTextureAsync(ImageResult image, bool dynamic, int priority);
    ExecutorTask<(IRenderTexture?, Exception? error)> LoadTextureAsync(string path);
    ExecutorTask<(IRenderTexture?, Exception? error)> LoadTextureAsync(string path, int priority);
    ///<summary>set the dynamic flag to true if it will be frequently modified.</summary>
    ExecutorTask<(IRenderTexture?, Exception? error)> LoadTextureAsync(string path, bool dynamic);
    ///<summary>set the dynamic flag to true if it will be frequently modified.</summary>
    ExecutorTask<(IRenderTexture?, Exception? error)> LoadTextureAsync(string path, bool dynamic, int priority);

    //mesh loading functions
    IRenderMesh LoadMesh(VMesh mesh);
    IRenderMesh LoadMesh(VMesh mesh, int priority);
    ///<summary>set the dynamic flag to true if it will be frequently modified.</summary>
    IRenderMesh LoadMesh(VMesh mesh, bool dynamic);
    ///<summary>set the dynamic flag to true if it will be frequently modified.</summary>
    IRenderMesh LoadMesh(VMesh mesh, bool dynamic, int priority);
    IRenderMesh? LoadMesh(string vmeshPath, out Exception? error);
    IRenderMesh? LoadMesh(string vmeshPath, out Exception? error, int priority);
    ///<summary>set the dynamic flag to true if it will be frequently modified.</summary>
    IRenderMesh? LoadMesh(string vmeshPath, out Exception? error, bool dynamic);
    ///<summary>set the dynamic flag to true if it will be frequently modified.</summary>
    IRenderMesh? LoadMesh(string vmeshPath, out Exception? error, bool dynamic, int priority);

    //async mesh loading functions
    ExecutorTask<IRenderMesh> LoadMeshAsync(VMesh mesh);
    ExecutorTask<IRenderMesh> LoadMeshAsync(VMesh mesh, int priority);
    ///<summary>set the dynamic flag to true if it will be frequently modified.</summary>
    ExecutorTask<IRenderMesh> LoadMeshAsync(VMesh mesh, bool dynamic);
    ///<summary>set the dynamic flag to true if it will be frequently modified.</summary>
    ExecutorTask<IRenderMesh> LoadMeshAsync(VMesh mesh, bool dynamic, int priority);
    ExecutorTask<(IRenderMesh?, Exception? error)> LoadMeshAsync(string vmeshPath);
    ///<summary>set the dynamic flag to true if it will be frequently modified.</summary>
    ExecutorTask<(IRenderMesh?, Exception? error)> LoadMeshAsync(string vmeshPath, int priority);
    ///<summary>set the dynamic flag to true if it will be frequently modified.</summary>
    ExecutorTask<(IRenderMesh?, Exception? error)> LoadMeshAsync(string vmeshPath, bool dynamic);
    ///<summary>set the dynamic flag to true if it will be frequently modified.</summary>
    ExecutorTask<(IRenderMesh?, Exception? error)> LoadMeshAsync(string vmeshPath, bool dynamic, int priority);

    /**
    <summary>
        Returns a shader given a set of required functionality.
        It reuses shaders when possible.
    </summary>
    */
    IRenderShader GetShader(ShaderFeatures features);
    /**
    <summary>
        Returns a shader given a set of required functionality.
        It reuses shaders when possible.
    </summary>
    */
    IRenderShader GetShader(ShaderFeatures features, int priority);
    /**
    <summary>
        Returns a shader given a set of required functionality.
        It reuses shaders when possible.
    </summary>
    */
    IRenderShader GetShader(string GLSLVertexCode, string GLSLFragmentCode, Attributes attributes);
    /**
    <summary>
        Returns a shader given a set of required functionality.
        It reuses shaders when possible.
    </summary>
    */
    IRenderShader GetShader(string GLSLVertexCode, string GLSLFragmentCode, Attributes attributes, int priority);
    /**
    <summary>
        Returns a shader given a set of required functionality.
        It reuses shaders when possible.
    </summary>
    */
    ExecutorTask<IRenderShader> GetShaderAsync(ShaderFeatures features);
    /**
    <summary>
        Returns a shader given a set of required functionality.
        It reuses shaders when possible.
    </summary>
    */
    ExecutorTask<IRenderShader> GetShaderAsync(ShaderFeatures features, int priority);

    /**
    <summary>
        Returns a shader given a set of required functionality.
        It reuses shaders when possible.
    </summary>
    */
    ExecutorTask<IRenderShader> GetShaderAsync(string GLSLVertexCode, string GLSLFragmentCode, Attributes attributes);
    /**
    <summary>
        Returns a shader given a set of required functionality.
        It reuses shaders when possible.
    </summary>
    */
    ExecutorTask<IRenderShader> GetShaderAsync(string GLSLVertexCode, string GLSLFragmentCode, Attributes attributes, int priority);

    RenderModel LoadModel(VModel model);

    RenderModel LoadModel(VModel model, int priority);
    RenderModel? LoadModel(string vmfPath, out List<VError>? errors);
    RenderModel? LoadModel(string vmfPath, out List<VError>? errors, int priority);

    ExecutorTask<RenderModel> LoadModelAsync(VModel model);
    ExecutorTask<RenderModel> LoadModelAsync(VModel model, int priority);
    ExecutorTask<(RenderModel?, List<VError>? errors)> LoadModelAsync(string vmfPath);
    ExecutorTask<(RenderModel?, List<VError>? errors)> LoadModelAsync(string vmfPath, int priority);



    //Rendering functionality
    // These don't have priority options since they are always priority -10
    /**
    <summary>
        Begins a new render queue.
        Must only be called when there is no other render queue being generated.
    </summary>
    */
    void BeginRenderQueue();
    /**
    <summary>
        Adds a draw call to the render queue.
        Must be called during an active queue, in other words after a 'BeginRenderQueue' and before a 'EndRenderQueue'.
        It is thread safe.
    </summary>
    */
    void Draw(
        IRenderTexture texture, IRenderMesh mesh, IRenderShader shader,
        IEnumerable<KeyValuePair<string, object>> uniforms,
        bool depthTest
    );
    /**
    <summary>
        Ends a render queue.
        This will wait until the render thread is finished rendering,
        and the render thread will wait until this is called to swap the buffers.
    </summary>
    */
    void EndRenderQueue();


    //System stuff
    KeyboardState Keyboard();

    MouseState Mouse();

    string GetClipboard();
    void SetClipboard(string clip);

    bool CursorLocked{get; set;}

    Vector2i WindowSize();

    RenderType GetRenderType();

    //Events
    /**
    <summary>
        Begins the main loop. This will not return until the rendering window closes.
        Call from the main thread, otherwise weird things might happen.
    </summary>
    */
    void Run();

    /**
    <summary>
        called every input update
        Game logic should be done here.
        Note that this might be called while a render queue is being generated.
    </summary>
    */
    Action<TimeSpan>? OnUpdate {get;set;}
    /**
    <summary>
        Called when it's time to generate a render queue.
        Note that this might be called while the game logic is running.
    </summary>
    */
    Action<TimeSpan>? OnDraw {get;set;}

    /**
    <summary>
        Called when it is time to start the game.
        Put loading code and stuff in here.
    </summary>
    */
    Action? OnStart {get;set;}

    Action? OnCleanup{get;set;}

    Action<KeyboardKeyEventArgs>? OnKeyDown {get; set;}
    Action<KeyboardKeyEventArgs>? OnKeyUp {get; set;}
    Action<MouseButtonEventArgs>? OnMouseDown {get; set;}
    Action<MouseButtonEventArgs>? OnMouseUp {get; set;}
    Action<ResizeEventArgs>? OnWindowResize {get; set;}

    bool IsDisposed();

    public ExecutorTask SubmitToQueue(Action task, int priority);
    public ExecutorTask<TResult> SubmitToQueue<TResult>(Func<TResult> task, int priority);
}
