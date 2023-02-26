namespace VRender.Interface;

public struct RenderModel
{
    public RenderModel(IRenderMesh mesh, IRenderTexture texture)
    {
        this.mesh = mesh;
        this.texture = texture;
    }
    public IRenderMesh mesh;
    public IRenderTexture texture;
}