namespace VRenderLib.Interface;

using StbImageSharp;

using Threading;
public interface IRenderTexture : IDisposable
{
    /**
    <summary>
        Returns if this texture has already been disposed.
        Doing anything with a texture that has been disposed of is undefined behavior.
    </summary>
    */
    bool IsDisposed();
    /**
    <summary>
        Gets the image data from the GPU and returns it.
        Don't use this unless for extraordinary circumstances, as it is quite slow.
    </summary>
    */
    ImageResult GetData();
    /**
    <summary>
        resets the image data for this texture.
        It has to upload the data to the GPU, so avoid using this if you can.
    </summary>
    */
    void SetData(ImageResult image);
}