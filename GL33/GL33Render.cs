using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using StbImageSharp;

using vmodel;

namespace VRender.GL33;
class GL33Render : IRender{

    public GL33Render(RenderSettings settings)
    {
        try{
            _delayedEntities = new Stack<GL33Entity>();
            _delayedEntityRemovals = new Stack<GL33Entity>();
            _settings = settings;
            _deletedMeshes = new List<GL33MeshHandle>();
            _deletedTextures = new List<GL33TextureHandle>();
            _directEntities = new List<GL33Entity>();
            _entities = new List<GL33Entity?>();
            _freeEntitySlots = new List<int>();
            _directTexture = new RenderImage((uint)settings.Size.X, (uint)settings.Size.Y);


            _window = new NativeWindow(
                new NativeWindowSettings(){
                    API = ContextAPI.OpenGL,
                    APIVersion = new System.Version(3, 3), //OpenGL 3.3
                    AutoLoadBindings = true,
                    NumberOfSamples = 0,
                    Profile = ContextProfile.Core,
                    Size = settings.Size,
                    StartFocused = false,
                    StartVisible = true,
                    Title = settings.WindowTitle,
                }
            );

            _window.MakeCurrent();
            EAttribute[] directAttributes = new EAttribute[]{EAttribute.vec2};
            _directShader = new GL33Shader(dvsc, dfsc, true);
            _directMesh = new GL33Mesh(directAttributes, new float[]{1f, 1f, 1f, -1f, -1f, 1f, -1f, -1f}, new uint[]{0, 1, 2, 1, 2, 3});
            _directTextureGPU = new GL33Texture(_directTexture);
            SpawnEntity(EntityPosition.Zero, _directShader, _directMesh, _directTextureGPU, false, null);
            //the NativeWindow class has no way to do this, so we directly ask GLFW for it
            if(!settings.VSync){
                OpenTK.Windowing.GraphicsLibraryFramework.GLFW.SwapInterval(0);
            } else{
                OpenTK.Windowing.GraphicsLibraryFramework.GLFW.SwapInterval(1);
            }
            

            _window.Resize += OnResize;

            _window.KeyDown += OnKeyDownFunc;
            _window.KeyUp += OnKeyUpFunc;
            _window.MouseDown += OnMouseDownFunc;
            _window.MouseUp += OnMouseUpFunc;
            GL.Enable(EnableCap.DepthTest);
            //GL.Enable(EnableCap.CullFace); //Sadly, OpenGLs face culling system was designed to be extremely simple and not easily worked with.
            //My culling system is based on surface normals, so this simply won't do.

            IRender.CurrentRender = this;
            IRender.CurrentRenderType = ERenderType.GL33;
        } catch (Exception e){
            throw new Exception("Error creating OpenGL 3.3 (GL33) window.\n\n", e);
        }        
    }
    public RenderSettings Settings {get => _settings;}

    public bool DebugRendering{
        get => _debugRendering;
        set => _debugRendering = value;
    }

    public Action<double>? OnUpdate {get; set;}
    public Action<double>? OnRender {get; set;}

    public Action<KeyboardKeyEventArgs>? OnKeyDown {get; set;}
    public Action<KeyboardKeyEventArgs>? OnKeyUp {get; set;}
    public Action<MouseButtonEventArgs>? OnMouseDown {get; set;}
    public Action<MouseButtonEventArgs>? OnMouseUp {get; set;}

    private void OnKeyDownFunc(KeyboardKeyEventArgs args){
        if(OnKeyDown != null)OnKeyDown.Invoke(args);
    }
    private void OnKeyUpFunc(KeyboardKeyEventArgs args){
        if(OnKeyUp != null)OnKeyUp.Invoke(args);
    }
    private void OnMouseDownFunc(MouseButtonEventArgs args){
        if(OnMouseDown != null)OnMouseDown.Invoke(args);
    }
    private void OnMouseUpFunc(MouseButtonEventArgs args){
        if(OnMouseUp != null)OnMouseUp.Invoke(args);
    }
    
    public void Run(){
        //I implimented my own game loop, because OpenTKs GameWindow doesn't update the keyboard state properly for the external OnUpdate event.
        long lastRenderTime = DateTime.Now.Ticks;
        _lastUpdateTime = lastRenderTime;
        while(!_window.IsExiting){
            bool didSomething = false;
            long time = DateTime.Now.Ticks; //10.000 ticks is 1ms. 10.000.000 ticks is 1s.
            if(time - lastRenderTime > 10_000_000*_settings.TargetFrameTime){
                Render(lastRenderTime, time);
                lastRenderTime = time;
                didSomething = true;
            }
            if(time - _lastUpdateTime > 10_000_000*RenderUtils.UpdateTime){
                Update(time, time - _lastUpdateTime);
                _lastUpdateTime = time;
                didSomething = true;
            }
            if(!didSomething){
                System.Threading.Thread.Sleep(1); //sleep for 1 ms. This stops us from stealing all of the CPU time.
            }
        }
        Exit();
    }

    private void Exit()
    {
        foreach(var entity in _entities)
        {
            if(entity is not null)
            {
                this.DeleteEntity(entity);
                entity._mesh.Dispose();
                entity._shader.Dispose();
                entity._texture.Dispose();
            }
        }
        _window.Close();
    }

    public Vector2i WindowSize(){
        return _window.Size;
    }

    public uint EntityCount(){
        return (uint)(_entities.Count - _freeEntitySlots.Count);
    }

    public uint EntityCapacity(){
        return (uint)_entities.Count;
    }

    //meshes
    public IRenderMesh? LoadMesh(string path, out Exception? err){
        VMesh? mesh = VModelUtils.LoadMesh(path, out err);
        if(mesh == null)return null;
        return new GL33Mesh(mesh.Value);
    }

    public IRenderMesh? LoadMesh(string path, bool dynamic, out Exception? err){
        VMesh? mesh = VModelUtils.LoadMesh(path, out err);
        if(mesh == null)return null;
        return new GL33Mesh(mesh.Value, dynamic);
    }

    public IRenderMesh LoadMesh(VMesh mesh){
        return new GL33Mesh(mesh);
    }
    public IRenderMesh LoadMesh(VMesh mesh, bool dynamic){
        return new GL33Mesh(mesh, dynamic);
    }
    public IRenderMesh LoadMesh(float[] vertices, uint[] indices, EAttribute[] attributes){
        return new GL33Mesh(attributes, vertices, indices);
    }

    public IRenderMesh LoadMesh(float[] vertices, uint[] indices, EAttribute[] attributes, bool dynamic){
        return new GL33Mesh(attributes, vertices, indices, dynamic);
    }

    public void DeleteMesh(IRenderMesh mesh){
        ((GL33Mesh)mesh).Dispose(); //any time this code is run, it can be safely cast to a GL33 object, since only GL33Objects can be created with a GL33Render.
    }
    //textures

    public IRenderTexture? LoadTexture(string path, out Exception? error){
        try{
            error = null;
            return new GL33Texture(path);
        } catch (Exception e){
            error = e;
            return null;
        }
    }

    public IRenderTexture LoadTexture(ImageResult image){
        return new GL33Texture(image);
    }
    public IRenderTexture LoadTexture(float r, float g, float b, float a){
        return new GL33Texture(r, g, b, a);
    }

    public IRenderTexture LoadTexture(IntPtr pixels, int width, int height, int channels){
        ImageResult image = new ImageResult();
        //first we marshall the data into an actual array
        image.Data = new byte[width*height*channels];
        Marshal.Copy(pixels, image.Data, 0, width*height*channels);

        image.Comp = (ColorComponents)channels;

        image.Height = height;
        image.Width = width;
        image.SourceComp = image.Comp;
        return new GL33Texture(image);
    }

    public void DeleteTexture(IRenderTexture texture){
        ((GL33Texture)texture).Dispose(); //any time this code is run, it can be safely cast to a GL33 object, since only GL33Objects can be created with a GL33Render.
    }
    //shaders

    public IRenderShader? LoadShader(string shader, out Exception? err){
        try{
            err = null;
            return new GL33Shader(shader + "vertex.glsl", shader + "fragment.glsl");
        }catch(Exception e){
            err = e;
            return null;
        }
    }

    public void DeleteShader(IRenderShader shader){
        ((GL33Shader)shader).Dispose();
    }

    //models
    public RenderEntityModel? LoadModel(string file, out List<VError>? err){
        //load the model data
        VModel? model = VModelUtils.LoadModel(file, out err);//new VModel(folder, file, out var ignored, out ICollection<string>? err);
        if(model == null)return null;
        //send it to the GPU
        GL33Mesh mesh = new GL33Mesh(model.Value.mesh);
        GL33Texture texture = new GL33Texture(model.Value.texture);

        if(err != null){
            RenderUtils.PrintErrLn(string.Join("/n", err));
        }
        return new RenderEntityModel(mesh, texture);
    }

    public RenderEntityModel LoadModel(VModel model){
        //send it to the GPU
        GL33Mesh mesh = new GL33Mesh(model.mesh);
        GL33Texture texture = new GL33Texture(model.texture);
        return new RenderEntityModel(mesh, texture);         
    }

    public void DeleteModel(RenderEntityModel model){
        ((GL33Mesh)model.mesh).Dispose();
        ((GL33Texture)model.texture).Dispose();
    }
    //special draw commands.
    /*
    <summary>
    Directly sets a pixel within the render buffer after the next render call.
    These draw calls write to a temporary texture on the CPU, then at the end of rendering it's uploaded to the GPU and drawn.
    Since it only interacts with the GPU once every frame, performance shouldn't be an issue unless for extraordinary circumstances.
    </summary>
    */
    public void WritePixelDirect(uint color, int x, int y)
    {
        _directTexture.WritePixel(x, y, color);
    }
    /*
    <summary>
    Directly draws a texture within the render buffer after the next render call.
    These draw calls write to a temporary texture on the CPU, then at the end of rendering it's uploaded to the GPU and drawn.
    Since it only interacts with the GPU once every frame, performance shouldn't be an issue unless for extraordinary circumstances.
    </summary>
    */
    public void DrawTextureDirect(RenderImage image, int x, int y, int width, int height, int srcx, int srcy, int srcwidth, int srcheight)
    {
        for(int xi=0; xi<width; xi++)
        {
            for(int yi=0; yi<height; yi++)
            {
                //Take one sample of the source and call it a day. Ideally we would get a list of the overlapping pixels, but that's complicated.
                int readX = xi*width/srcwidth+srcx;
                int readY = yi*height/srcheight+srcy;
                uint color = image.ReadPixel(readX, readY);
                _directTexture.WritePixel(xi+x, yi+y, color);
            }
        }
    }
    //entities
    public IRenderEntity SpawnEntity(EntityPosition pos, IRenderShader shader, IRenderMesh mesh, IRenderTexture texture, bool depthTest, IEntityBehavior? behavior){
        GL33Entity entity = new GL33Entity(pos, (GL33Mesh)mesh, (GL33Texture)texture, (GL33Shader)shader, 0, depthTest, behavior);
        _AddEntity(entity);
        return entity;
    }

    public IRenderEntity SpawnEntityDelayed(EntityPosition pos, IRenderShader shader, IRenderMesh mesh, IRenderTexture texture, bool depthTest, IEntityBehavior? behavior){
        GL33Entity entity = new GL33Entity(pos, (GL33Mesh)mesh, (GL33Texture)texture, (GL33Shader)shader, 0, depthTest, behavior);
        _delayedEntities.Push(entity);
        return entity;
    }

    public IRenderTextEntity SpawnTextEntity(EntityPosition pos, string text, bool centerX, bool centerY, IRenderShader shader, IRenderTexture texture, bool depthTest, IEntityBehavior? behavior){
        GL33TextEntity entity = new GL33TextEntity(pos, text, centerX, centerY, (GL33Texture)texture, (GL33Shader)shader, 0, depthTest, behavior);
        _AddEntity(entity);
        return entity;
    }
    public IRenderTextEntity SpawnTextEntityDelayed(EntityPosition pos, string text, bool centerX, bool centerY, IRenderShader shader, IRenderTexture texture, bool depthTest, IEntityBehavior? behavior){
        GL33TextEntity entity = new GL33TextEntity(pos, text, centerX, centerY, (GL33Texture)texture, (GL33Shader)shader, 0, depthTest, behavior);
        _delayedEntities.Push(entity);
        return entity;
    }

    public void DeleteEntity(IRenderEntity entity){
        GL33Entity glEntity = (GL33Entity)entity;
        if(glEntity.Id() < 0){
            RenderUtils.PrintErrLn("ERROR: entity index is negative! This should be impossible.");
            return;
        }
        _entities[glEntity.Id()] = null;//remove the entity
        _freeEntitySlots.Add(glEntity.Id()); //add its empty spot to the list
    }

    public void DeleteEntityDelayed(IRenderEntity entity){
        GL33Entity glEntity = (GL33Entity)entity;
        if(glEntity.Id() < 0){
            RenderUtils.PrintErrLn("ERROR: entity index is negative! This should be impossible.");
            return;
        }
        _delayedEntityRemovals.Push(glEntity);
    }

    private void _AddEntity(GL33Entity entity){
        if(_freeEntitySlots.Count > 0){
            int id = _freeEntitySlots[_freeEntitySlots.Count-1];
            _freeEntitySlots.RemoveAt(_freeEntitySlots.Count-1);
            _entities[id] = entity;
            entity.Id(id);
        } else {
            entity.Id(_entities.Count);
            _entities.Add(entity);
        }
    }

    public IEnumerable<IRenderEntity?> GetEntities(){
        return _entities;
    }

    public void RenderMeshDirect(EntityPosition pos, IRenderShader shader, IRenderMesh mesh, IRenderTexture texture, bool depthTest){
        _directEntities.Add(new GL33Entity(pos, (GL33Mesh)mesh, (GL33Texture)texture, (GL33Shader)shader, -1, depthTest, null)); //lazy solution, I don't care.
    }

    //camera
    public RenderCamera SpawnCamera(Vector3 position, Vector3 rotation, float fovy){
        return new RenderCamera(position, rotation, fovy, _window.ClientSize);
    }
    public void SetCamera(RenderCamera camera){
        _camera = camera;
    }
    public RenderCamera? GetCamera(){
        return _camera;
    }
    public void DeleteCamera(RenderCamera camera){
        if(camera == _camera){
            RenderUtils.PrintWarnLn("Cannot delete the active camera!");
        }
        //Cameras are handled by the C# runtime. Technically, this method is completely pointless.
    }
    //Input
    public KeyboardState Keyboard(){
        return _window.KeyboardState;
    }

    public MouseState Mouse(){
        return _window.MouseState;
    }

    public string GetClipboard()
    {
        return _window.ClipboardString;
    }

    public void SetClipboard(string clip)
    {
        _window.ClipboardString = clip;
    }

    public bool CursorLocked{
        get => _cursorLocked;
        set{
            _cursorLocked = value;
            _window.CursorVisible = !_cursorLocked;
            _window.CursorGrabbed = _cursorLocked;
        }
    }
    private void Update(long timeTicks, long deltaTicks){
        _window.ProcessEvents();
        //clear out deletion buffers

        //meshes
        lock(_deletedMeshes){
            if(_deletedMeshes.Count > 0){
                RenderUtils.PrintWarnLn($"Clearing {_deletedMeshes.Count} leaked meshes - somebody (probably me) forgot to delete their meshes!");
                foreach(GL33MeshHandle mesh in _deletedMeshes){
                    GL33Mesh.Dispose(mesh);
                }
            _deletedMeshes.Clear();
            }
        }

        //textures
        lock(_deletedTextures){
            if(_deletedTextures.Count > 0){
                RenderUtils.PrintWarnLn($"Clearing {_deletedTextures.Count} leaked textures - somebody (probably me) forgot to delete their textures!");
                foreach(GL33TextureHandle mesh in _deletedTextures){
                    GL33Texture.Dispose(mesh);
                }
            _deletedTextures.Clear();
            }
        }
        //I don't bother with shaders (yet) since they are usually small, and very few of them ever exist.
        //If it becomes an issue, then i'll add the deletion buffer for that.


        foreach(GL33Entity? entity in _entities){
            //update previous matrix values
            if(entity == null) continue;
            entity.lastTransform = entity.GetTransform();
        }

        if(_camera != null) _camera.lastTransform = _camera.GetTransform();

        //update events
        if(OnUpdate != null)OnUpdate.Invoke(deltaTicks/10_000_000.0);
        //update entity behaviors
        KeyboardState keyboard = Keyboard();
        MouseState mouse = Mouse();
        foreach(GL33Entity? entity in _entities){
            if(entity is null)continue;
            if(entity.Behavior is null)continue;
            entity.Behavior.Update(timeTicks/10_000_000.0, deltaTicks/10_000_000.0, entity, keyboard, mouse);
        }

        //process delayed entities
        while(_delayedEntities.Count > 0){
            _AddEntity(_delayedEntities.Pop());
        }
        while(_delayedEntityRemovals.Count > 0){
            DeleteEntity(_delayedEntityRemovals.Pop());
        }
    }
    private void Render(long lastRender, long now)
    {
        _window.MakeCurrent(); //make sure the window context is current.
        //invoke OnRender event first.
        float delta = (now - _lastUpdateTime)/10_000_000.0f;
        if(OnRender != null)OnRender.Invoke(delta);

        //upload CPU render texture
        _directTextureGPU.Reload(_directTexture);
        float weight = (float) (delta/RenderUtils.UpdateTime); //0=only last, 1=fully current'
        Matrix4 interpolatedCamera;
        if(_camera is not null){
            interpolatedCamera = InterpolateMatrix(_camera.GetTransform(), _camera.lastTransform, weight);
        } else {
            interpolatedCamera = Matrix4.Identity;
        }

        //Render entities from the RenderEntityDirect method
        foreach(GL33Entity entity in _directEntities){
            DrawEntity(entity, weight, interpolatedCamera);
        }
        _directEntities.Clear();
        foreach(GL33Entity? entity in _entities){
            DrawEntity(entity, weight, interpolatedCamera);
        }
        _window.Context.SwapBuffers();
        // Clear all of the stuff
        RenderImage.ColorFromRGBA(out byte r, out byte g, out byte b, out byte a, _settings.BackgroundColor);
        GL.ClearColor(r/256f, g/256f, b/256f, a/256f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        Array.Fill<uint>(_directTexture.pixels, 0x00000000);
    }

    private void DrawEntity(GL33Entity? entity, float weight, Matrix4 camera)
    {
        if(entity is null)return;
        //bind objects
        entity._mesh.Bind();
        entity._texture.Use(TextureUnit.Texture0);
        entity._shader.Use();
        //set uniforms
        //For now, only one texture slot is ever used.
        entity._shader.SetInt("tex", 0, false);
        Matrix4 interpolatedEntityView = InterpolateMatrix(entity.GetTransform(), entity.lastTransform, weight);
        entity._shader.SetMatrix4("model", interpolatedEntityView, false);
        entity._shader.SetMatrix4("camera", camera, false);
        //set other variables
        if(entity._depthTest)GL.Enable(EnableCap.DepthTest);
        else GL.Disable(EnableCap.DepthTest);
        //draw
        GL.DrawElements(BeginMode.Triangles, entity._mesh.ElementCount()*3, DrawElementsType.UnsignedInt, 0);
    }

    private static Matrix4 InterpolateMatrix(Matrix4 left, Matrix4 right, float weight)
    {
        float rweight = 1-weight;
        return new Matrix4(
            left.Row0*weight + right.Row0*rweight,
            left.Row1*weight + right.Row1*rweight,
            left.Row2*weight + right.Row2*rweight,
            left.Row3*weight + right.Row3*rweight
        );
    }
    private void OnResize(ResizeEventArgs args){
        GL.Viewport(0, 0, args.Width, args.Height);
        if(_camera != null)_camera.Aspect = (float)args.Width/(float)args.Height;
        _directTexture = new RenderImage((uint)args.Width, (uint)args.Height);
    }

    public List<GL33TextureHandle> _deletedTextures;

    public List<GL33MeshHandle> _deletedMeshes;

    private long _lastUpdateTime;

    private RenderSettings _settings;
    private NativeWindow _window;
    private List<GL33Entity> _directEntities;

    private List<GL33Entity?> _entities;

    private List<int> _freeEntitySlots;

    private RenderCamera? _camera;

    private bool _cursorLocked;
    private bool _debugRendering;

    private Stack<GL33Entity> _delayedEntities;
    private Stack<GL33Entity> _delayedEntityRemovals;
    private RenderImage _directTexture;
    private GL33Texture _directTextureGPU;
    //vertex thingy for rendering direct texture. Yep. You aren't blind, this is possibly the simplest vertex shader possible.
    // I need to do this because OpenGL 3.3 deprecated some (very useful imo) functions for interoping CPU and GPU rendering.
    // Ideally I would just render everything with OpenGL and never use the CPU for that,
    // but I don't feel like throwing away my custom GUI library quite yet.
    private const string dvsc = @"
    #version 330 core
    layout (location=0) in vec2 pos;
    void main(){
        gl_Position = vec4(pos, 1.0, 1.0);
    }
    ";
    private const string dfsc = @"
    #version 330 core
    uniform sampler2D tex;
    out vec4 outputColor;
    void main(){
        outputColor = texelFetch(tex, ivec2(gl_FragCoord.xy), 0);
        if(outputColor.a < .5)discard;
    }
    ";

    private GL33Shader _directShader;
    private GL33Mesh _directMesh;
}