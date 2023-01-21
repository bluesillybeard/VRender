using System;
using System.Collections.Generic;

using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Common;

using StbImageSharp;

using vmodel;
namespace VRender{
    public interface IRender{
        #pragma warning disable //disable the null warning, since the CurrentRender will NEVER be null in any valid runtime.
        public static IRender CurrentRender;
        #pragma warning enable
        public static ERenderType CurrentRenderType;

        //mixed bits

        void Run();

        bool DebugRendering{get;set;}

        /**
        <summary>
        This action is called every update - 30 times each second.
        Entities are automatically updated.
        In case it's not 30 times per second (laggy conditions for example), the double input is the delta time.
        </summary>
        */
        Action<double>? OnUpdate {get; set;}
        Action<double>? OnRender {get; set;}

        Action<KeyboardKeyEventArgs>? OnKeyDown {get; set;}
        Action<KeyboardKeyEventArgs>? OnKeyUp {get; set;}
        Action<MouseButtonEventArgs>? OnMouseDown {get; set;}
        Action<MouseButtonEventArgs>? OnMouseUp {get; set;}
        RenderSettings Settings{get;}

        //in pixels
        Vector2i WindowSize();

        uint EntityCount();

        uint EntityCapacity();

        //ImGuiController

        //meshes
        IRenderMesh LoadMesh(float[] vertices, uint[] indices, EAttribute[] attributes);
        IRenderMesh LoadMesh(float[] vertices, uint[] indices, EAttribute[] attributes, bool dynamic);
        IRenderMesh LoadMesh(VMesh mesh);
        IRenderMesh LoadMesh(VMesh mesh, bool dynamic);

        /**
         <summary> 
          loads a vmesh file into a GPU-stored mesh.
         </summary>
        */
        IRenderMesh? LoadMesh(string VMFPath, out Exception? err);

        void DeleteMesh(IRenderMesh mesh);
        //textures

        /**
        <summary>
        loads a texture into the GPU.
        Supports png, jpg, jpeg
        </summary>
        */
        IRenderTexture? LoadTexture(string filePath, out Exception? error);

        /**
        <summary>
        loads a texture into the GPU
        </summary>
        */
        IRenderTexture LoadTexture(ImageResult image);

        IRenderTexture LoadTexture(float r, float g, float b, float a);

        /**
        <summary>
        loads a texture into the GPU from an array of RGBA pixels, and a width and height variable.
        </summary>
        */
        IRenderTexture LoadTexture(IntPtr pixels, int width, int height, int channels);

        void DeleteTexture(IRenderTexture texture);

        //shaders

        /**
        <summary>
        loads, compiles, and links a shader program.

        Note that, for a GL33Render for example, "fragment.glsl" and "vertex.glsl" is appended to the shader path for the pixel and vertex shaders respectively.
        </summary>

        */
        IRenderShader? LoadShader(string shaderPath, out Exception? err);

        void DeleteShader(IRenderShader shader);

        //models

        /**
        <summary>
        loads the mesh and texture from a vmf, vemf, or vbmf model
        </summary>
        */
        RenderEntityModel? LoadModel(string file, out List<VError>? err);

        RenderEntityModel LoadModel(VModel model);

        /**
        <summary>
        deletes the internal mesh and texture of a model.
        </summary>
        */

        void DeleteModel(RenderEntityModel model);
        //special draw commands.
        /*
        <summary>
        Directly sets a pixel within the render buffer after the next render call.
        These draw calls write to a temporary texture on the CPU, then at the end of rendering it's uploaded to the GPU and drawn.
        Since it only interacts with the GPU once every frame, performance shouldn't be an issue unless for extraordinary circumstances.
        </summary>
        */
        void WritePixelDirect(uint color, int x, int y);
        /*
        <summary>
        Directly draws a texture within the render buffer after the next render call.
        These draw calls write to a temporary texture on the CPU, then at the end of rendering it's uploaded to the GPU and drawn.
        Since it only interacts with the GPU once every frame, performance shouldn't be an issue unless for extraordinary circumstances.
        </summary>
        */
        void DrawTextureDirect(RenderImage image, int x, int y, int width, int height, int srcx, int srcy, int srcwidth, int srcheight);
        //entities
        IRenderEntity SpawnEntity(EntityPosition pos, IRenderShader shader, IRenderMesh mesh, IRenderTexture texture, bool depthTest, IEntityBehavior? behavior);
        ///<summary>Waits until the end of the update cycle to spawn an entity </summary>
        IRenderEntity SpawnEntityDelayed(EntityPosition pos, IRenderShader shader, IRenderMesh mesh, IRenderTexture texture, bool depthTest, IEntityBehavior? behavior);

        //text entities. A normal entity, but it has text mesh generation built-in.
        IRenderTextEntity SpawnTextEntity(EntityPosition pos, string text, bool centerX, bool centerY, IRenderShader shader, IRenderTexture texture, bool depthTest, IEntityBehavior? behavior);
        ///<summary>Waits until the end of the update cycle to spawn an entity </summary>
        IRenderTextEntity SpawnTextEntityDelayed(EntityPosition pos, string text, bool centerX, bool centerY, IRenderShader shader, IRenderTexture texture, bool depthTest, IEntityBehavior? behavior);

        //Entities are deleted using the same method as normal entities
        /**
        <summary>
        Deletes an entity.
        Note that this can be used to delete both normal and text entities.
        </summary>
        */
        void DeleteEntity(IRenderEntity entity);
        /**
        <summary>
        Waits until the end of the update cycle to delete the entity.
        Note that this can be used to delete both normal and text entities.
        </summary>
        */
        void DeleteEntityDelayed(IRenderEntity entity);

        /**
        <summary>
        Returns the list of entities.
        Note that there WILL be null elements. If an entity is 'null', it means that it has been removed.
        </summary>
        */
        IEnumerable<IRenderEntity?> GetEntities();

        /**
        Directly render a mesh + texure + shader.
        Only call during the OnRender event.
        Will always render BEFORE any entities are rendered, since the OnRender event happens at the beginning of a frame cycle.
        */
        void RenderMeshDirect(EntityPosition pos, IRenderShader shader, IRenderMesh mesh, IRenderTexture texture, bool depthTest);
        //camera

        RenderCamera SpawnCamera(Vector3 position, Vector3 rotation, float fovy);

        void SetCamera(RenderCamera camera);
        RenderCamera? GetCamera();
        void DeleteCamera(RenderCamera camera);

        //input
        KeyboardState Keyboard();

        MouseState Mouse();

        string GetClipboard();
        void SetClipboard(string clip);

        bool CursorLocked{get; set;}
    }
}