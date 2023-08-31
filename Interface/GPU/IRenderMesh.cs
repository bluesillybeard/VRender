using vmodel;

namespace VRenderLib.Interface;

public interface IRenderMesh : IDisposable
{
    /**
    <summary>
        Returns the attributes for this mesh.
    </summary>
    */
    Attributes GetAttributes();
    /**
    <summary>
        Downloads the mesh data from the GPU and then returns it.
        Don't use this method as it is rather slow.
        It will not return any triangle-face mappings for block models, as those are entirely a CPU concept.
    </summary>
    */
    VMesh GetData();
    /**
    <summary>
        Sets the data of this mesh
        It has to upload the data to the GPU, so avoid using this if you can.
    </summary>
    */
    void SetData(VMesh mesh);
    /**
    <summary>
        Returns if this mesh has already been disposed.
        Doing anything with a mesh that has been disposed of is undefined behavior.
    </summary>
    */
    bool IsDisposed();
}