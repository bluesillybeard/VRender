using System.Collections;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Desktop;
using VRenderLib.Interface;

namespace VRenderLib.Implementation.GL33;

class GL33DrawCommandQueue : IDrawCommandQueue
{
    readonly List<Action> drawCommands;

    public GL33DrawCommandQueue()
    {
        drawCommands = new List<Action>();
    }

    public void Draw(
        IRenderTexture texture, IRenderMesh mesh, IRenderShader shader,
        IEnumerable<KeyValuePair<string, object>> uniforms,
        bool depthTest
    )
    {
        drawCommands.Add(() => DrawRaw((GL33Texture)texture, (GL33Mesh)mesh, (GL33Shader)shader, uniforms, depthTest));
    }

    public void DrawDirect(IRenderTexture texture, IRenderMesh mesh, IRenderShader shader,
        IEnumerable<KeyValuePair<string, object>> uniforms,
        bool depthTest)
    {
        DrawRaw((GL33Texture)texture, (GL33Mesh)mesh, (GL33Shader)shader, uniforms, depthTest);
    }

    private static void DrawRaw(
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
    public void Custom(Action action)
    {
        drawCommands.Add(action);
    }
    public void Finish()
    {
        //TODO: it might be worth optimizing the commands or something
    }

    public void Process(NativeWindow window)
    {
        GL.ClearColor(0, 0, 0, 1);
        GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);
        foreach(var action in drawCommands)
        {
            action.Invoke();
        }
        window.Context.SwapBuffers();
    }

    public void Reset()
    {
        drawCommands.Clear();
    }
}