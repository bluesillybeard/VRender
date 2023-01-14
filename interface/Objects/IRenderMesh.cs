using vmodel;

using System;
namespace Render{
    public interface IRenderMesh: IRenderObject{
        int ElementCount();
        int VertexCount();
        void ReData(float[] vertices, uint[] indices);
        void AddData(float[] vertices, uint[] indices);

        EAttribute[] Attributes(); //Attributes are non-modifiable since a mesh shouldn't change structure or function. By the time the attributes change, you should reallt just make a new mesh.
    }
}