namespace Render{
    public struct RenderEntityModel{
        public IRenderMesh mesh;
        public IRenderTexture texture;

        public RenderEntityModel(IRenderMesh mesh, IRenderTexture texture){
            this.mesh = mesh;
            this.texture = texture;
        }
    }
}