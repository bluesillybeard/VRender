using OpenTK.Graphics.OpenGL;

using System;
using System.Collections.Generic;

using vmodel;


namespace VRender.GL33{

    struct GL33MeshHandle{

        public GL33MeshHandle(int indexBuffer, int vertexBufferObject, int vertexArrayObject){
            this.indexBuffer = indexBuffer;
            this.vertexBufferObject = vertexBufferObject;
            this.vertexArrayObject = vertexArrayObject;
        }
        public int indexBuffer;

        public int vertexBufferObject;
        public int vertexArrayObject;
    }

    class GL33Mesh: GL33Object, IRenderMesh, IDisposable{
        bool _deleted; //to tell if a mesh has been disposed of or not
        private int _indexBuffer; //the OpenGL index buffer

        private int _vertexBufferObject; //the OpenGL vertex buffer

        //The vertex array object is in the _id property from IRenderObject.

        private int _indexCount; //the number of indices in the mesh. In other words, the length of the index buffer.

        private int _vertexFloats; //the number of total vertex ATTRIBUTES in the mesh - not the number of actual vertices. In other words, the length of the vertex buffer.

        private EAttribute[] _attributes;

        public EAttribute[] Attributes(){
            return _attributes;
        }

        public GL33Mesh(VMesh mesh){
            _attributes = mesh.attributes;
            LoadMesh(_attributes, mesh.vertices, mesh.indices, false);
        }

        public GL33Mesh(VMesh mesh, bool dynamic){
            _attributes = mesh.attributes;
            LoadMesh(_attributes, mesh.vertices, mesh.indices, dynamic);
        }
        public GL33Mesh(EAttribute[] attributes, float[] vertices, uint[] indices){
            _attributes = attributes;
            LoadMesh(attributes, vertices, indices, false);
        }
        public GL33Mesh(EAttribute[] attributes, float[] vertices, uint[] indices, bool dynamic){
            _attributes = attributes;
            LoadMesh(attributes, vertices, indices, dynamic);
        }
        public GL33Mesh(EAttribute[] attributes, IntPtr vertices, int verticesSizeBytes, IntPtr indices, int indicesSizeBytes){
            _attributes = attributes;
            LoadMesh(attributes, vertices, verticesSizeBytes, indices, indicesSizeBytes, false);
        }
        public GL33Mesh(EAttribute[] attributes, IntPtr vertices, int verticesSizeBytes, IntPtr indices, int indicesSizeBytes, bool dynamic){
            _attributes = attributes;
            LoadMesh(attributes, vertices, verticesSizeBytes, indices, indicesSizeBytes, dynamic);
        }
        public void ReData(VMesh mesh){
            ReData(mesh.vertices, mesh.indices);
        }
        
        public void ReData(float[] vertices, uint[] indices){
            _indexCount = indices.Length;
            _vertexFloats = vertices.Length;
            GL.BindVertexArray(_id);

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.DynamicDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.DynamicDraw);
        }

        public void ReData(IntPtr vertices, int verticesSizeBytes, IntPtr indices, int indicesSizeBytes){
            _indexCount = indicesSizeBytes / sizeof(uint);
            _vertexFloats = verticesSizeBytes / sizeof(float);
            GL.BindVertexArray(_id);

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, verticesSizeBytes, vertices, BufferUsageHint.DynamicDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indicesSizeBytes, indices, BufferUsageHint.DynamicDraw);
        }

        public void AddData(VMesh mesh){
            AddData(mesh.vertices, mesh.indices);
        }
        
        //Implementing this function was a huge pain.
        //Mostly since OpenGL doesn't do it for me.
        public void AddData(float[] vertices, uint[] indices){
            //modify the indices so we can haphazardly add them on.
            for(int i=0; i<indices.Length; i++){
                indices[i] += (uint)_indexCount;
            }
            //The fact that OpenGL has no built-in way to expand the size of a buffer without overriding it is annoying.
            
            GL.BindVertexArray(_id);
            {
                //This is in a separate block to keep the stack from leaking.
                //I'm so tired that leaking doesn't even look like a word anymore.
                GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
                //load the buffer into memory
                float[] bufferVertices = new float[_vertexFloats + vertices.Length];
                GL.GetBufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, _vertexFloats*sizeof(float), bufferVertices);
                //add the new data
                for(int i=0; i<vertices.Length; i++){
                    bufferVertices[i+_vertexFloats] = vertices[i];
                }
                //upload the new buffer.
                GL.BufferData(BufferTarget.ArrayBuffer, ((_vertexFloats+vertices.Length)*sizeof(float)), bufferVertices, BufferUsageHint.DynamicDraw);
            }
            {
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
                //load the buffer into memory
                uint[] bufferIndices = new uint[_indexCount + indices.Length];
                GL.GetBufferSubData(BufferTarget.ElementArrayBuffer, IntPtr.Zero, _indexCount*sizeof(uint), bufferIndices);
                //add the new data
                for(int i=0; i<indices.Length; i++){
                    bufferIndices[i+_indexCount] = indices[i];
                }
                //upload the new buffer.
                GL.BufferData(BufferTarget.ElementArrayBuffer, ((_indexCount+indices.Length)*sizeof(uint)), bufferIndices, BufferUsageHint.DynamicDraw);
            }
            _indexCount += indices.Length;
            _vertexFloats += vertices.Length;
        }



        private void LoadMesh(EAttribute[] attributes, float[] vertices, uint[] indices, bool dynamic){
            _indexCount = indices.Length;
            _vertexFloats = vertices.Length;
            _id = GL.GenVertexArray();
            GL.BindVertexArray(_id);

            _vertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
            if(dynamic)GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
            else GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.DynamicDraw);

            _indexBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
            if(dynamic)GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);
            else GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.DynamicDraw);

            int totalAttrib = 0;
            foreach(EAttribute attrib in attributes){
                totalAttrib += (int)attrib % 5;
            }

            int runningTotalAttrib = 0;
            for(int i=0; i<attributes.Length; i++){
                EAttribute attrib = (EAttribute)((int)attributes[i] % 5);
                GL.EnableVertexAttribArray(i);
                GL.VertexAttribPointer(i, (int)attrib, VertexAttribPointerType.Float, false, totalAttrib*sizeof(float), runningTotalAttrib*sizeof(float));
                runningTotalAttrib += (int)attrib;
            }
        }
        private void LoadMesh(EAttribute[] attributes, IntPtr vertexBuffer, int vertexBufferLength, IntPtr indexBuffer, int indexBufferLength, bool dynamic){
            _indexCount = indexBufferLength;
            _vertexFloats = vertexBufferLength;
            _id = GL.GenVertexArray();
            GL.BindVertexArray(_id);

            _vertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
            if(dynamic)GL.BufferData(BufferTarget.ArrayBuffer, vertexBufferLength, vertexBuffer, BufferUsageHint.DynamicDraw);
            else GL.BufferData(BufferTarget.ArrayBuffer, vertexBufferLength, vertexBuffer, BufferUsageHint.StaticDraw);

            _indexBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
            if(dynamic)GL.BufferData(BufferTarget.ElementArrayBuffer, indexBufferLength, indexBuffer, BufferUsageHint.DynamicDraw);
            else GL.BufferData(BufferTarget.ElementArrayBuffer, indexBufferLength, indexBuffer, BufferUsageHint.StaticDraw);

            int totalAttrib = 0;
            foreach(EAttribute attrib in attributes){
                totalAttrib += (int)attrib;
            }

            int runningTotalAttrib = 0;
            for(int i=0; i<attributes.Length; i++){
                EAttribute attrib = attributes[i];
                GL.EnableVertexAttribArray(i);
                GL.VertexAttribPointer(i, (int)attrib, VertexAttribPointerType.Float, false, totalAttrib*sizeof(float), runningTotalAttrib*sizeof(float));
                runningTotalAttrib += (int)attrib;
            }
        }
        public void Bind(){
            GL.BindVertexArray(_id);
        }

        public int ElementCount(){
            return _indexCount/3;
        }

        public int VertexCount(){
            return _vertexFloats;
        }

        ~GL33Mesh(){
            //check to see if it's already deleted - if not, it's been leaked and should be taken care of.
            if(!_deleted){
                //add it to the deleted meshes buffer, since the C# garbage collector won't have the OpenGl context.
                //I am aware of the fact this is spaghetti code. I just can't think of a better way to do it.
                //any time this code is used, it can be safely cast to a GL33 object, since only GL33Objects can be created with a GL33Render.
                List<GL33MeshHandle> deletedMeshes = ((GL33Render)IRender.CurrentRender)._deletedMeshes;
                lock(deletedMeshes)
                    deletedMeshes.Add(new GL33MeshHandle(_indexBuffer, _vertexBufferObject, _id));
            }
        }

        //dispose of a garbage-collected mesh
        public static void Dispose(GL33MeshHandle mesh){
            GL.DeleteBuffer(mesh.vertexBufferObject);
            GL.DeleteBuffer(mesh.indexBuffer);

            GL.DeleteVertexArray(mesh.vertexArrayObject);
        }

        public void Dispose(){
            GL.DeleteBuffer(_vertexBufferObject);
            GL.DeleteBuffer(_indexBuffer);

            GL.DeleteVertexArray(_id);
            _deleted = true;
        }
    }
}