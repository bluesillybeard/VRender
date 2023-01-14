using OpenTK.Graphics.OpenGL;

using System;
using System.Collections.Generic;
using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;
using System.IO;

using StbImageSharp;
//NOTICE: this is a modified version of the texture class from the official OpenTK examples.

namespace VRender.GL33{

    //This is used to clean up leaked textures from the GPU.
    struct GL33TextureHandle{
        public GL33TextureHandle(int id){
            this.id = id;
        }
        public int id;
    }
    
    class GL33Texture: GL33Object, IRenderTexture, IDisposable
    {
        private bool _deleted;
        public GL33Texture(string path)
        {
            ImageResult img = ImageResult.FromMemory(File.ReadAllBytes(path));
            LoadTexture(img);
        }

        public GL33Texture(ImageResult image){
            LoadTexture(image);
        }

        public GL33Texture(RenderImage image){
            LoadTexture(image);
        }

        public void Reload(string path)
        {
            ImageResult img = ImageResult.FromMemory(File.ReadAllBytes(path));
            LoadTexture(img);
        }

        public void Reload(ImageResult image){
            LoadTexture(image);
        }

        public void Reload(RenderImage image){
            LoadTexture(image);
        }
        public GL33Texture(float r, float g, float b, float a)
        {
            RenderImage image = new RenderImage(20, 20);
            image.WritePixel(0, 0, RenderImage.RGBAFromColor((byte)(r*256),(byte)(g*256),(byte)(b*256),(byte)(a*256)));
            LoadTexture(image);
        }
        private void LoadTexture(ImageResult img){
            // Generate handle
            _id = GL.GenTexture();

            // Bind the handle
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _id);

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

        private void LoadTexture(RenderImage img){
            // Generate handle
            _id = GL.GenTexture();

            // Bind the handle
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _id);

            GL.TexImage2D(TextureTarget.Texture2D,
                0,
                PixelInternalFormat.Rgba,
                (int)img.width,
                (int)img.height,
                0,
                PixelFormat.Rgba,
                //haha endianess go BRRRRRRR
                PixelType.UnsignedInt8888,
                img.pixels);

            SetParametersAndGenerateMipmaps();
        }

        private void SetParametersAndGenerateMipmaps(){
            // Now that our texture is loaded, we can set a few settings to affect how the image appears on rendering.

            // First, we set the min and mag filter. These are used for when the texture is scaled down and up, respectively.
            // Here, we use Linear for both. This means that OpenGL will try to blend pixels, meaning that textures scaled too far will look blurred.
            // You could also use (amongst other options) Nearest, which just grabs the nearest pixel, which makes the texture look pixelated if scaled too far.
            // NOTE: The default settings for both of these are LinearMipmap. If you leave these as default but don't generate mipmaps,
            // your image will fail to render at all (usually resulting in pure black instead).
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

            // Now, set the wrapping mode. S is for the X axis, and T is for the Y axis.
            // We set this to Repeat so that textures will repeat when wrapped. Not demonstrated here since the texture coordinates exactly match
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            // Next, generate mipmaps.
            // Mipmaps are smaller copies of the texture, scaled down. Each mipmap level is half the size of the previous one
            // Generated mipmaps go all the way down to just one pixel.
            // OpenGL will automatically switch between mipmaps when an object gets sufficiently far away.
            // This prevents moiré effects, as well as saving on texture bandwidth.
            // Here you can see and read about the morié effect https://en.wikipedia.org/wiki/Moir%C3%A9_pattern
            // Here is an example of mips in action https://en.wikipedia.org/wiki/File:Mipmap_Aliasing_Comparison.png
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }


        // Activate texture
        // Multiple textures can be bound, if your shader needs more than just one.
        // If you want to do that, use GL.ActiveTexture to set which slot GL.BindTexture binds to.
        // The OpenGL standard requires that there be at least 16, but there can be more depending on your graphics card.
        public void Use(TextureUnit unit)
        {
            GL.ActiveTexture(unit);
            GL.BindTexture(TextureTarget.Texture2D, _id);
        }

        public void Dispose(){
            GL.DeleteTexture(this._id);
            this._deleted = true;
        }

        public static void Dispose(GL33TextureHandle texture){
            GL.DeleteTexture(texture.id);
        }

        ~GL33Texture(){
            //check to see if it's already deleted - if not, it's been leaked and should be taken care of.
            if(!_deleted){
                //add it to the deleted textures buffer, since the C# garbage collector won't have the OpenGl context.
                //I am aware of the fact this is spaghetti code. I just can't think of a better way to do it.
                //any time this code is used, it can be safely cast to a GL33 object, since only GL33Objects can be created with a GL33Render.
                List<GL33TextureHandle> deletedTextures = ((GL33Render)IRender.CurrentRender)._deletedTextures;
                lock(deletedTextures)
                    deletedTextures.Add(new GL33TextureHandle(_id));
            }
        }
    }
}
