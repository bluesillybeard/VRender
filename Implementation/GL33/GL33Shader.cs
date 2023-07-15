namespace VRenderLib.Implementation.GL33;

using Interface;

using vmodel;

using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

using System.Text;
using System.Text.RegularExpressions;

public class GL33Shader : IRenderShader
{

    /**
    <summary>
        Relies on the GL context
    </summary>
    */
    public GL33Shader(string fragmentCode, string vertexCode, Attributes attributes)
    {
        LoadShader(vertexCode, fragmentCode, out program, out uniforms);
        this.attributes = attributes;
    }
    public Attributes GetAttributes()
    {
        return attributes;
    }
    public bool IsDisposed()
    {
        return disposed;
    }

    /**
    <summary>
        Relies on the GL context
    </summary>
    */
    public void Use()
    {
        GL.UseProgram(program);
    }

    /// <summary>
    /// Set a uniform int on this shader.
    /// Relies on the GL context.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    public void SetInt(string name, int data, out string? error){
        if(uniforms.TryGetValue(name, out int uniformID)){
            GL.UseProgram(program);
            GL.Uniform1(uniformID, data);
            error = null;
        } else {
            error = $"ERROR: shader uniform \"{name}\" doesn't exist in {this.program}";
        }
    }

    /// <summary>
    /// Set a uniform float on this shader.
    /// Relies on the GL context.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    public void SetFloat(string name, float data, out string? error)
    {
        if(uniforms.TryGetValue(name, out int uniformID)){
            GL.UseProgram(program);
            GL.Uniform1(uniformID, data);
            error = null;
        } else {
            error = $"ERROR: shader uniform \"{name}\" doesn't exist in {this.program}";
        }
    }

    /// <summary>
    /// Set a uniform Matrix4 on this shader
    /// Relies on the GL context.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    /// <remarks>
    ///   <para>
    ///   The matrix is transposed before being sent to the shader.
    ///   </para>
    /// </remarks>
    public void SetMatrix4(string name, Matrix4 data, out string? error)
    {
        if(uniforms.TryGetValue(name, out int uniformID)){
            GL.UseProgram(program);
            GL.UniformMatrix4(uniformID, true, ref data);
            error = null;
        } else {
            error = $"ERROR: shader uniform \"{name}\" doesn't exist in {this.program}";
        }
    }

    /// <summary>
    /// Set a uniform Vector3 on this shader.
    /// Relies on the GL context.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    public void SetVector3(string name, Vector3 data, out string? error)
    {
        if(uniforms.TryGetValue(name, out int uniformID)){
            GL.UseProgram(program);
            GL.Uniform3(uniformID, data);
            error = null;
        } else {
            error = $"ERROR: shader uniform \"{name}\" doesn't exist in {this.program}";
        }
    }

    /// <summary>
    /// Set a uniform Vector3 on this shader.
    /// Relies on the GL context.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    public void SetVector4(string name, Vector4 data, out string? error)
    {
        if(uniforms.TryGetValue(name, out int uniformID)){
            GL.UseProgram(program);
            GL.Uniform4(uniformID, data);
            error = null;
        } else {
            error = $"ERROR: shader uniform \"{name}\" doesn't exist in {this.program}";
        }
    }

    public void SetUniform(string name, object data, out string? error)
    {
        if(data is int integer)
        {
            SetInt(name, integer, out error);
        }
        else if(data is float floater)
        {
            SetFloat(name, floater, out error);
        } else if(data is Matrix4 matrix)
        {
            SetMatrix4(name, matrix, out error);
        } else if(data is Vector3 vector3)
        {
            SetVector3(name, vector3, out error);
        }else if(data is Vector4 vector4)
        {
            SetVector4(name, vector4, out error);
        } else 
        {
            error = name + " is not one of the supported types";
        }

    }

    public void Dispose(){
        if(Environment.CurrentManagedThreadId != 1)
        {
            //Needs to be on main thread
            IRender.CurrentRender.SubmitToQueueLowPriority( () => {
                Dispose();
            }, "DisposeShader");
            return;
        }
        GL.DeleteProgram(this.program);
        disposed = true;
    }

    private int program;
    private bool disposed;
    private Attributes attributes;
    private Dictionary<string, int> uniforms;
    
    private static void LoadShader(string vertexSource, string fragmentSource, out int program, out Dictionary<string, int> uniforms)
    {

        int vertexShader = MakeShader(ShaderType.VertexShader, vertexSource);
        int fragmentShader = MakeShader(ShaderType.FragmentShader, fragmentSource);

        program = GL.CreateProgram();

        GL.AttachShader(program, vertexShader);
        GL.AttachShader(program, fragmentShader);
        LinkProgram(program);

        // When the shader program is linked, it no longer needs the individual shaders attached to it; the compiled code is copied into the shader program.
        // Detach them, and then delete them.
        GL.DetachShader(program, vertexShader);
        GL.DetachShader(program, fragmentShader);
        GL.DeleteShader(fragmentShader);
        GL.DeleteShader(vertexShader);

        // The shader is now ready to go, but first, we're going to cache all the shader uniform locations.
        // Querying this from the shader is very slow, so we do it once on initialization and reuse those values
        // later.

        // First, we have to get the number of active uniforms in the shader.
        GL.GetProgram(program, GetProgramParameterName.ActiveUniforms, out int numberOfUniforms);

        // Next, allocate the dictionary to hold the locations.
        uniforms = new Dictionary<string, int>();

        // Loop over all the uniforms,
        for (int i = 0; i < numberOfUniforms; i++)
        {
            // get the name of this uniform,
            string key = GL.GetActiveUniform(program, i, out _, out _);

            // get the location,
            int location = GL.GetUniformLocation(program, key);

            // and then add it to the dictionary.
            uniforms.Add(key, location);
        }
    }

    private static int MakeShader(ShaderType type, string shaderSource)
    {
        //non-ascii characters are problematic
        shaderSource = Regex.Replace(shaderSource, @"[^\u0000-\u007F]+", string.Empty);
        int shader = GL.CreateShader(type);

        //bind the GLSL source code
        GL.ShaderSource(shader, shaderSource);
        // Try to compile the shader
        GL.CompileShader(shader);

        // Check for compilation errors
        GL.GetShader(shader, ShaderParameter.CompileStatus, out int code);
        if (code != (int)All.True)
        {
            // We can use `GL.GetShaderInfoLog(shader)` to get information about the error.
            string infoLog = GL.GetShaderInfoLog(shader);
            throw new Exception($"Error occurred whilst compiling Shader({shader}).\n\n{infoLog}");
        }

        return shader;
    }

    private static void LinkProgram(int program)
    {
        // We link the program
        GL.LinkProgram(program);

        // Check for linking errors
        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int code);
        if (code != (int)All.True)
        {
            // We can use `GL.GetProgramInfoLog(program)` to get information about the error.
            string infoLog = GL.GetProgramInfoLog(program);
            throw new Exception($"ERROR: unable to link Program({program}).\n\n{infoLog}");
        }
    }

    public override int GetHashCode()
    {
        return program;
    }
}