using OpenTK.Graphics.OpenGL;

using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;

using VRenderLib.Interface;
using VRenderLib.Threading;

using StbImageSharp;

namespace VRenderLib.Implementation.GL33;

public sealed class GL33Texture : IRenderTexture
{
    /**
    <summary>
        Relies on the GL context.
    </summary>
    */
    public GL33Texture(ImageResult img)
    {
        LoadTexture(img);
    }

    /**
    <summary>
        Relies on the GL context.
    </summary>
    */
    private void LoadTexture(ImageResult img)
    {
        textureId = GL.GenTexture();

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, textureId);

        PixelFormat format;
        PixelInternalFormat internalFormat;
        //todo: add more component types
        switch (img.Comp){
            case ColorComponents.RedGreenBlue: {
                format = PixelFormat.Rgb;
                internalFormat = PixelInternalFormat.Rgb;
                break;
            }
            case ColorComponents.RedGreenBlueAlpha: {
                format = PixelFormat.Rgba;
                internalFormat = PixelInternalFormat.Rgba;
                break;
            }
            default: throw new Exception("invalid image type - only RGB and RGBA are supported.");
        }

        GL.TexImage2D(TextureTarget.Texture2D,
            0,
            internalFormat,
            img.Width,
            img.Height,
            0,
            format,
            PixelType.UnsignedByte,
            img.Data);

        SetParametersAndGenerateMipmaps();
    }
    /**
    <summary>
        Relies on the ExecutorTaskGL context.
    </summary>
    */
    private static void SetParametersAndGenerateMipmaps()
    {
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
    }
    private bool disposed;
    //Disposes on the main thread.
    public void Dispose()
    {
        if(Environment.CurrentManagedThreadId != 1)
        {
            IRender.CurrentRender.SubmitToQueueLowPriority(() => Dispose(), "DisposeTexture");
            // The caller isn't waiting for a result, so we just move on.
            return;
        }
        GL.DeleteTexture(this.textureId);
        this.disposed = true;
    }
    public bool IsDisposed()
    {
        return disposed;
    }

    public void SetData(ImageResult img)
    {
        //Make sure we are on the main thread
        if(Environment.CurrentManagedThreadId != 1)
        {
            //We need to do it on the main thread so we have the OpenGL context.
            var task = IRender.CurrentRender.SubmitToQueue(() => SetData(img), "TextureSetData");
            return;
        }
        //GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, textureId);
        GL.TexSubImage2D(TextureTarget.Texture2D,
            0, 0, 0,
            (int)img.Width,
            (int)img.Height,
            PixelFormat.Rgba,
            PixelType.UnsignedInt,
            img.Data);

        SetParametersAndGenerateMipmaps();
        return;
    }

    public ImageResult GetData()
    {
        throw new NotImplementedException();
    }
    /**
    <summary>
        Relies on the GL context.
    </summary>
    */
    public void Use(TextureUnit index)
    {
        GL.ActiveTexture(index);
        GL.BindTexture(TextureTarget.Texture2D, textureId);
    }

    private int textureId;

    public override int GetHashCode()
    {
        return textureId;
    }
}