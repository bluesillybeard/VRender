namespace VRender;

using Interface;
using Implementation.GL33;
//the VRender "main" class.
public static class VRender
{
    /**
    <summary>
        This will initialize a rendering API to be used.
        If this method is not called from the main thread, strange problems are very likely to occur.
    </summary>
    */
    public static IRender InitRender(RenderSettings settings)
    {
        //TODO: add more render APIs.
        render = new GL33Render(settings);
        renderType = render.GetRenderType();
        return render;
    }
    private static IRender? render;
    private static RenderType renderType;

    /**
    <summary>
        The initialized rendering system.
        This will throw an exception if InitRender() hasn't been called.
    </summary>
    */
    public static IRender Render {
        get {
            if(render is null) throw new Exception("Make sure to call InitRender() before trying to use Render!");
            return render;
        }
    }
    public static RenderType RenderType{get => renderType;}

    //Some useful utility functions
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
}