namespace VRender.Implementation.GL33;

using Interface;

using vmodel;

using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

using System.Text;


public class GL33Shader : IRenderShader
{
    public GL33Shader(ShaderFeatures features)
    {
        //generate vertex shader
        string vertexCode = GenerateVertexCode(features);
        string fragmentCode = GenerateFragmentCode(features);
        //load the shader
        LoadShader(vertexCode, fragmentCode, out program, out uniforms);

    }

    //TODO: these code generators are incomplete, as they don't have RGB or RGBA color support.
    public static string GenerateFragmentCode(ShaderFeatures features)
    {
        StringBuilder code = new StringBuilder();
        code.Append("#version 330 core\n");
        code.Append("out vec4 colorOut;\n");
        //In vars
        bool hasTextureCoords = features.attributes.Contains(EAttribute.textureCoords);
        if(hasTextureCoords)
        {
            code.Append("in vec2 texCoord;\n");
        }
        //Texture uniform - this is always absolutely required
        code.Append("uniform sampler2D tex;\n");
        //Actual program
        code.Append("void main(){\n");
        //color output
        code.Append("colorOut = ");
        if(hasTextureCoords)
        {
            code.Append("texture(tex, texCoord);\n");
        } else
        {
            code.Append("vec4(0, 0, 0, 0);\n");
        }
        code.Append("if(colorOut.a < .5)discard;\n");
        code.Append("}");

        return code.ToString();
    }

    public static string GenerateVertexCode(ShaderFeatures features)
    {
        StringBuilder vertexShader = new StringBuilder();
        vertexShader.Append("#version 330 core\n");
        //Keep track of some things for later
        int texCoordLayout = -1;
        int positionLayout = -1;
        //First, generate the GLSL layouts
        for(int layout=0; layout<features.attributes.Length; layout++)
        {
            EAttribute attributeFull = features.attributes[layout];
            string name = Enum.GetName(typeof(EAttribute), attributeFull) ?? "float";
            //Convert the general Attribute into just its type.
            // Since these are named based on what they are using the same naming sceheme as GLSL,
            // I can simply just take its type name and use that.
            EAttribute type = (EAttribute)((int)attributeFull % 5);
            string typeName = Enum.GetName(typeof(EAttribute), type) ?? "float";
            if(typeName == "scalar") typeName = "float";
            //                                       |layout idx  |type     |variable name
            vertexShader.Append($"layout(location = {layout}) in {typeName} v{layout}{name};\n");

            //Keep track of some things for later
            if(attributeFull is EAttribute.position)
            {
                positionLayout = layout;
            } else if(attributeFull is EAttribute.textureCoords)
            {
                texCoordLayout = layout;
            }
        }
        //generate out variables.
        // For now, the output will always be just a texture coordinate (If the attributes even have texture coordinates!)
        // TODO: add more out variables based on attributes (hardest part is in the fragment shader actually)
        bool hasTextureCoords = features.attributes.Contains(EAttribute.textureCoords);
        if(hasTextureCoords)
        {
            vertexShader.Append("out vec2 texCoord;\n");
        }
        //Generate uniforms
        if(features.applyCameraTransform)
        {
            vertexShader.Append("uniform mat4 camera;\n");
        }
        if(features.applyModelTransform)
        {
            vertexShader.Append("uniform mat4 model;\n");
        }
        //generate the shader code itself
        vertexShader.Append("void main(){\n");

        //Set output vars
        if(hasTextureCoords)
        {
            vertexShader.Append($"texCoord = v{texCoordLayout}textureCoords;\n");
        }
        //set gl_Position
        if(positionLayout != -1)
        {
            vertexShader.Append($"gl_Position = vec4(v{positionLayout}position, 1.0)");
        }
        else{
            //This probably shouldn't be a thing, but I am doing it anyway.
            vertexShader.Append($"gl_Position = vec4(0, 0, 0, 0)");
        }
        if(features.applyModelTransform)
        {
            vertexShader.Append($" * model");
        }
        if(features.applyCameraTransform)
        {
            vertexShader.Append($" * camera");
        }
        vertexShader.Append(";\n");

        //We are done!
        vertexShader.Append("}\n");

        return vertexShader.ToString();
    }
    /**
    <summary>
        Relies on the GL context
    </summary>
    */
    public GL33Shader(string fragmentCode, string vertexCode, ShaderFeatures features)
    {
        LoadShader(vertexCode, fragmentCode, out program, out uniforms);
        this.features.attributes = features.attributes;
        this.features = features;
    }
    public ReadOnlySpan<EAttribute> GetAttributes()
    {
        return features.attributes.AsSpan();
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
        } else if(data is Vector3 vector)
        {
            SetVector3(name, vector, out error);
        } else 
        {
            error = "Data is not one of the supported types";
        }

    }

    public void Dispose(){
        if(Environment.CurrentManagedThreadId != 1)
        {
            //Needs to be on main thread
            IRender.CurrentRender.SubmitToQueue( () => {
                Dispose();
            });
            return;
        }
        GL.DeleteProgram(this.program);
        disposed = true;
    }

    public ShaderFeatures GetFeatures()
    {
        return features;
    }

    private int program;
    private bool disposed;
    private ShaderFeatures features;
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
}