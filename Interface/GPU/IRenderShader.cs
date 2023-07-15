namespace VRenderLib.Interface;

using vmodel;
public interface IRenderShader : IDisposable
{
    Attributes GetAttributes();

    /**
    <summary>
        Returns if this shader has already been disposed.
        Doing anything with a shader that has been disposed of is undefined behavior.
    </summary>
    */
    bool IsDisposed();
}

