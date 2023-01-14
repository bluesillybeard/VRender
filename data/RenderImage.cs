//a texture stored on the CPU.
//Must be RGBA (no rgb, no grayscale, just 8 bpc RGBA)

using StbImageSharp;

public struct RenderImage
{
    public RenderImage(uint width, uint height)
    {
        pixels = new uint[width*height];
        this.width = width;
        this.height = height;
    }
    
    public static void ColorFromRGBA(out byte r, out byte g, out byte b, out byte a, uint rgba)
    {
        r = (byte)((rgba>>24)&0xFF);
        g = (byte)((rgba>>16)&0xFF);
        b = (byte)((rgba>>8)&0xFF);
        a = (byte)(rgba&0xFF);
    }
    public static uint RGBAFromColor(byte r, byte g, byte b, byte a)
    {
        uint rgba = 0;
        rgba |= (uint)(r>>24);
        rgba |= (uint)(g>>16);
        rgba |= (uint)(b>>8);
        rgba |= (uint)a;
        return rgba;
    }
    public static void ColorFromARGB(out byte r, out byte g, out byte b, out byte a, uint argb)
    {
        a = (byte)((argb>>24)&0xFF);
        r = (byte)((argb>>16)&0xFF);
        g = (byte)((argb>>8)&0xFF);
        b = (byte)(argb&0xFF);
    }
    //Apparently ARGB is common too, so these functions help with that
    public static uint ARGBFromColor(byte r, byte g, byte b, byte a)
    {
        uint rgba = 0;
        rgba |= (uint)(a>>24);
        rgba |= (uint)(r>>16);
        rgba |= (uint)(g>>8);
        rgba |= (uint)b;
        return rgba;
    }
    //RGBA values.
    public uint[] pixels;
    public uint width;
    public uint height;

    public uint ReadPixel(int x, int y)
    {
        int index = (int)((height-y)*width + x);
        if(index > pixels.Length || index < 0) return 0;
        return pixels[index];
    }

    public bool WritePixel(int x, int y, uint color)
    {
        int index = (int)((height-y)*width + x);
        if(index >= pixels.Length || index < 0) return false;
        pixels[index] = color;
        return true;
    }
    //For converting an image from StbImage to Render.
    public RenderImage(ImageResult stbImage)
    {
        this.width = (uint)stbImage.Width;
        this.height = (uint)stbImage.Height;
        this.pixels = new uint[width*height];
        for(int i = 0; i<pixels.Length; i++)
        {
            byte r = 0;
            byte g = 0;
            byte b = 0;
            byte a = 0;
            int stbIndex = i * (int)stbImage.Comp;
            switch(stbImage.Comp)
            {
                case ColorComponents.Grey:
                    r = g = b = stbImage.Data[i];
                    a = 0xff;
                    break;
                case ColorComponents.GreyAlpha:
                    a = stbImage.Data[i*2];
                    r = g = b = stbImage.Data[i*2+1];
                    break;
                case ColorComponents.RedGreenBlue:
                    r = stbImage.Data[i*3];
                    g = stbImage.Data[i*3+1];
                    b = stbImage.Data[i*3+2];
                    a = 0xff;
                    break;
                case ColorComponents.RedGreenBlueAlpha:
                    r = stbImage.Data[i*4];
                    g = stbImage.Data[i*4+1];
                    b = stbImage.Data[i*4+2];
                    a = stbImage.Data[i*4+3];
                    break;
                case ColorComponents.Default:
                    break;
            }
        }
    }
}