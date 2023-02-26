namespace VRender;

using Interface;
using Implementation.GL33;
//the VRender "main" class.
public static class VRenderLib
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

}