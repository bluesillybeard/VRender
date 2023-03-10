namespace VRender.Implementation.GL33;

using Interface;

using vmodel;

using OpenTK.Graphics.OpenGL;

//TODO: refactor all of this to use the command queue
class GL33Mesh : IRenderMesh
{
    public Attributes GetAttributes()
    {
        return attributes;
    }
    public VMesh GetData()
    {
        //Don't forget to make sure it's going to run on the main thread
        throw new NotImplementedException();
    }

    public void SetData(VMesh data)
    {
        //Don't forget to make sure it's going to run on the main thread
        //TODO: This is pretty important actually
        throw new NotImplementedException();
    }

    public bool IsDisposed()
    {
        return disposed;
    }

    public void Dispose()
    {
        if(Environment.CurrentManagedThreadId != 1)
        {
            //Needs to be on main thread
            IRender.CurrentRender.SubmitToQueue( () => {
                Dispose();
            });
            return;
        }
        disposed = true;
        GL.DeleteBuffer(vertexBufferObject);
        GL.DeleteBuffer(indexBufferObject);
        GL.DeleteVertexArray(vertexArrayObject);
    }

    public void Use(){
        GL.BindVertexArray(vertexArrayObject);
    }

    public uint ElementCount(){
        return indexCount/3;
    }

    public uint VertexCount(){
        return vertexFloatCount;
    }
    private bool disposed;
    private int indexBufferObject;
    private int vertexBufferObject;
    private int vertexArrayObject;
    private uint indexCount;
    private uint vertexFloatCount;
    private Attributes attributes;
    public GL33Mesh(VMesh mesh, bool dynamic)
    {
        this.attributes = mesh.attributes;
        LoadMesh(mesh.vertices, mesh.indices, dynamic);
    }

    public GL33Mesh(Attributes attributes, float[] vertices, uint[] indices, bool dynamic)
    {
        this.attributes = attributes;
        LoadMesh(vertices, indices, dynamic);
    }
    private void LoadMesh(float[] vertices, uint[] indices, bool dynamic)
    {
        indexCount = (uint)indices.Length;
        vertexFloatCount = (uint)vertices.Length;
        vertexArrayObject = GL.GenVertexArray();
        GL.BindVertexArray(vertexArrayObject);

        vertexBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferObject);
        if(dynamic)GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.DynamicDraw);
        else GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        indexBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferObject);
        if(dynamic)GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.DynamicDraw);
        else GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

        int totalAttrib = (int)attributes.TotalAttributes();

        int runningTotalAttrib = 0;
        for(int i=0; i<attributes.Length; i++){
            int attrib = (int)attributes[i] % 5;
            GL.EnableVertexAttribArray(i);
            GL.VertexAttribPointer(i, attrib, VertexAttribPointerType.Float, false, totalAttrib*sizeof(float), runningTotalAttrib*sizeof(float));
            runningTotalAttrib += attrib;
        }
    }
    public override int GetHashCode()
    {
        return vertexArrayObject;
    }
}

//TODO: memory leak avoidance.
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