using System;
using System.IO;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

//NOTICE: this is a modified version of the shader class from the official OpenTK examples.

namespace Render.GL33{
    class GL33Shader: GL33Object, IRenderShader, IDisposable{
        private readonly Dictionary<string, int> _uniformLocations;
        public readonly string _name;

        private int errors;
        public GL33Shader(string vertPath, string fragPath)
        {
            _name = vertPath + "|" + fragPath;
            // Load vertex shader and compile
            string vertexShader = File.ReadAllText(vertPath);
            // We do the same for the fragment shader.
            string fragmentShader = File.ReadAllText(fragPath);
            LoadShader(vertexShader, fragmentShader, _name, out _id, out _uniformLocations);
        }
        public GL33Shader(string vertex, string fragment, bool direct)
        {
            if(!direct)
            {
                _name = vertex + "|" + fragment;
                // Load vertex shader and compile
                string vertexShader = File.ReadAllText(vertex);
                // We do the same for the fragment shader.
                string fragmentShader = File.ReadAllText(fragment);
                LoadShader(vertexShader, fragmentShader, _name, out _id, out _uniformLocations);
                return;
            }
            _name = "direct#" + vertex.GetHashCode() + "." + fragment.GetHashCode();
            LoadShader(vertex, fragment, _name, out _id, out _uniformLocations);

        }
        // If only I could call other constructors to avoid nulables, instead of using copious amounts of out variables.
        private static void LoadShader(string vertexSource, string fragmentSource, string name, out int id, out Dictionary<string, int> uniforms)
        {

            int vertexShader = MakeShader(ShaderType.VertexShader, vertexSource);
            int fragmentShader = MakeShader(ShaderType.FragmentShader, fragmentSource);

            id = GL.CreateProgram();

            GL.AttachShader(id, vertexShader);
            GL.AttachShader(id, fragmentShader);
            LinkProgram(id, name);

            // When the shader program is linked, it no longer needs the individual shaders attached to it; the compiled code is copied into the shader program.
            // Detach them, and then delete them.
            GL.DetachShader(id, vertexShader);
            GL.DetachShader(id, fragmentShader);
            GL.DeleteShader(fragmentShader);
            GL.DeleteShader(vertexShader);

            // The shader is now ready to go, but first, we're going to cache all the shader uniform locations.
            // Querying this from the shader is very slow, so we do it once on initialization and reuse those values
            // later.

            // First, we have to get the number of active uniforms in the shader.
            GL.GetProgram(id, GetProgramParameterName.ActiveUniforms, out int numberOfUniforms);

            // Next, allocate the dictionary to hold the locations.
            uniforms = new Dictionary<string, int>();

            // Loop over all the uniforms,
            for (int i = 0; i < numberOfUniforms; i++)
            {
                // get the name of this uniform,
                string key = GL.GetActiveUniform(id, i, out _, out _);

                // get the location,
                int location = GL.GetUniformLocation(id, key);

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

        private static void LinkProgram(int program, string name)
        {
            // We link the program
            GL.LinkProgram(program);

            // Check for linking errors
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int code);
            if (code != (int)All.True)
            {
                // We can use `GL.GetProgramInfoLog(program)` to get information about the error.
                string infoLog = GL.GetProgramInfoLog(program);
                throw new Exception($"ERROR: unable to link Program({program}).\n\n{infoLog}\n\n{name}");
            }
        }

        // A wrapper function that enables the shader program.
        public void Use()
        {
            GL.UseProgram(_id);
        }

        // The shader sources provided with this project use hardcoded layout(location)s. If you want to do it dynamically,
        // you can omit the layout(location=X) lines in the vertex shader, and use this in VertexAttribPointer instead of the hardcoded values.
        public int GetAttribLocation(string attribName)
        {
            return GL.GetAttribLocation(_id, attribName);
        }

        // Uniform setters
        // Uniforms are variables that can be set by user code, instead of reading them from the VBO.
        // You use VBOs for vertex-related data, and uniforms for almost everything else.

        // Setting a uniform is almost always the exact same, so I'll explain it here once, instead of in every method:
        //     1. Bind the program you want to set the uniform on
        //     2. Get a handle to the location of the uniform with GL.GetUniformLocation.
        //     3. Use the appropriate GL.Uniform* function to set the uniform.

        /// <summary>
        /// Set a uniform int on this shader.
        /// </summary>
        /// <param name="name">The name of the uniform</param>
        /// <param name="data">The data to set</param>
        public void SetInt(string name, int data, bool printErrors){
            if(_uniformLocations.TryGetValue(name, out int uniformID)){
                GL.UseProgram(_id);
                GL.Uniform1(uniformID, data);
            } else {
                if(printErrors && errors<50)RenderUtils.PrintWarnLn($"ERROR: shader uniform \"{name}\" doesn't exist in {this._name}");
                errors++;
            }
        }

        /// <summary>
        /// Set a uniform float on this shader.
        /// </summary>
        /// <param name="name">The name of the uniform</param>
        /// <param name="data">The data to set</param>
        public void SetFloat(string name, float data, bool printErrors)
        {
            if(_uniformLocations.TryGetValue(name, out int uniformID)){
                GL.UseProgram(_id);
                GL.Uniform1(uniformID, data);
            } else {
                if(printErrors && errors<50)RenderUtils.PrintWarnLn($"ERROR: shader uniform \"{name}\" doesn't exist in {this._name}");
                errors++;
            }
        }

        /// <summary>
        /// Set a uniform Matrix4 on this shader
        /// </summary>
        /// <param name="name">The name of the uniform</param>
        /// <param name="data">The data to set</param>
        /// <remarks>
        ///   <para>
        ///   The matrix is transposed before being sent to the shader.
        ///   </para>
        /// </remarks>
        public void SetMatrix4(string name, Matrix4 data, bool printErrors)
        {
            if(_uniformLocations.TryGetValue(name, out int uniformID)){
                GL.UseProgram(_id);
                GL.UniformMatrix4(uniformID, true, ref data);
            } else {
                if(printErrors && errors<50)RenderUtils.PrintWarnLn($"ERROR: shader uniform \"{name}\" doesn't exist in {this._name}");
                errors++;
            }
        }

        /// <summary>
        /// Set a uniform Vector3 on this shader.
        /// </summary>
        /// <param name="name">The name of the uniform</param>
        /// <param name="data">The data to set</param>
        public void SetVector3(string name, Vector3 data, bool printErrors)
        {
            if(_uniformLocations.TryGetValue(name, out int uniformID)){
                GL.UseProgram(_id);
                GL.Uniform3(uniformID, data);
            } else {
                if(printErrors && errors<50)RenderUtils.PrintWarnLn($"ERROR: shader uniform \"{name}\" doesn't exist in {this._name}");
                errors++;
            }
        }

        public void Dispose(){
            GL.DeleteProgram(this._id);
        }
    }
}