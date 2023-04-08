namespace VRenderLib.Interface;
using OpenTK.Mathematics;
public struct RenderSettings
{
    public double TargetFrameTime;
    public bool VSync;
    public string WindowTitle;
    public uint BackgroundColor; //RGBA clear color. the R channel is the most significant byte,
    public Vector2i size;
}