namespace VRenderLib.Interface;

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


    Some things to know:
    -synchronous methods that rely on the main thread (such as LoadTexture or LoadModel) will actually run on the main thread and are thread safe. They can also be safely called from the main thread.
*/
public interface IRender : IDisposable
{
    public static IRender CurrentRender => VRender.Render;

    //Texture loading functions
    IRenderTexture LoadTexture(ImageResult image);
    ///<summary>set the dynamic flag to true if the texture will be frequently modified.</summary>
    IRenderTexture LoadTexture(ImageResult image, bool dynamic);
    IRenderTexture? LoadTexture(string path, out Exception? error);
    ///<summary>set the dynamic flag to true if it will be frequently modified.</summary>
    IRenderTexture? LoadTexture(string path, bool dynamic, out Exception? error);
    ///<summary>set the dynamic flag to true if it will be frequently modified.</summary>
    //asynchronous texture loading functions
    ExecutorTask<IRenderTexture> LoadTextureAsync(ImageResult image);
    //C# Tuple FTW
    ///<summary>set the dynamic flag to true if the texture will be frequently modified.</summary>
    ExecutorTask<IRenderTexture> LoadTextureAsync(ImageResult image, bool dynamic);
    ExecutorTask<(IRenderTexture?, Exception? error)> LoadTextureAsync(string path);

    ///<summary>set the dynamic flag to true if it will be frequently modified.</summary>
    ExecutorTask<(IRenderTexture?, Exception? error)> LoadTextureAsync(string path, bool dynamic);

    //mesh loading functions
    IRenderMesh LoadMesh(VMesh mesh);
    ///<summary>set the dynamic flag to true if it will be frequently modified.</summary>
    IRenderMesh LoadMesh(VMesh mesh, bool dynamic);
    IRenderMesh? LoadMesh(string vmeshPath, out Exception? error);
    ///<summary>set the dynamic flag to true if it will be frequently modified.</summary>
    IRenderMesh? LoadMesh(string vmeshPath, out Exception? error, bool dynamic);

    //async mesh loading functions
    ExecutorTask<IRenderMesh> LoadMeshAsync(VMesh mesh);
    ///<summary>set the dynamic flag to true if it will be frequently modified.</summary>
    ExecutorTask<IRenderMesh> LoadMeshAsync(VMesh mesh, bool dynamic);
    ExecutorTask<(IRenderMesh?, Exception? error)> LoadMeshAsync(string vmeshPath);
    ///<summary>set the dynamic flag to true if it will be frequently modified.</summary>
    ExecutorTask<(IRenderMesh?, Exception? error)> LoadMeshAsync(string vmeshPath, bool dynamic);

    //TODO: determine Attributes from the shader code instead instead of taking it as a parameter
    /**
    <summary>
        Returns a compiled shader from the given GLSL code.
        Accepts GLSL "#version 330" and older.
        It reuses shaders when possible.
    </summary>
    */
    IRenderShader GetShader(string GLSLVertexCode, string GLSLFragmentCode, Attributes attributes);

    /**
    <summary>
        Returns a compiled shader from the given GLSL code.
        Accepts GLSL "#version 330" and older.
        The only value given by the Render itself is uniform Sampler2D tex.
    </summary>
    */
    ExecutorTask<IRenderShader> GetShaderAsync(string GLSLVertexCode, string GLSLFragmentCode, Attributes attributes);

    RenderModel LoadModel(VModel model);

    RenderModel? LoadModel(string vmfPath, out List<VError>? errors);

    ExecutorTask<RenderModel> LoadModelAsync(VModel model);
    ExecutorTask<(RenderModel?, List<VError>? errors)> LoadModelAsync(string vmfPath);

    //rendering

    //System stuff
    KeyboardState Keyboard();

    MouseState Mouse();

    string GetClipboard();
    void SetClipboard(string clip);

    CursorState CursorState{get; set;}

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
    Action<TimeSpan, IDrawCommandQueue>? OnDraw {get;set;}

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

    public ExecutorTask SubmitToQueue(Action task, string name);
    public ExecutorTask SubmitToQueueHighPriority(Action task, string name);
    public ExecutorTask SubmitToQueueLowPriority(Action task, string name);
    public ExecutorTask<TResult> SubmitToQueue<TResult>(Func<TResult> task, string name);
    public ExecutorTask<TResult> SubmitToQueueHighPriority<TResult>(Func<TResult> task, string name);
    public ExecutorTask<TResult> SubmitToQueueLowPriority<TResult>(Func<TResult> task, string name);
}
