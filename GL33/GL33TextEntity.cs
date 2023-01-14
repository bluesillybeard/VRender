namespace VRender.GL33;

using VRender.Util;
using vmodel;
class GL33TextEntity : GL33Entity, IRenderTextEntity{
    public string Text{
        get => _text;
        set {
            _text = value;
            VMesh? newMesh = MeshGenerators.BasicText(_text, _CenterX, _CenterY, _mesh.Attributes(), 0, 1, out var err);
            if(err != null)RenderUtils.PrintErrLn("Error updating text entity:" + err);
            if(newMesh != null)_mesh.ReData(newMesh.Value);
        }
    }

    public bool CenterX{get => _CenterX; set=>_CenterX=value;}
    public bool CenterY{get => _CenterY; set=>_CenterY=value;}
    
    private bool _CenterX;
    private bool _CenterY;
    private string _text;
    //No mesh is provided since we generate that ourselves.

    //I think being forced to call the super constructor as the very first thing is a little dumb, because (like in this case) there is code that would normally need to run before the constructor.
    #pragma warning disable //disable the null warning, because the mesh is IMMEDIATELY set to a non-null value after the super constructor is called.
    public GL33TextEntity(EntityPosition pos, string text, bool centerX, bool centerY, GL33Texture texture, GL33Shader shader, int id, bool depthTest, IEntityBehavior? behavior)
    :base(pos, null, texture, shader, id, depthTest, behavior){
        #pragma warning enable
        _CenterX = centerX;
        _CenterY = centerY;
        _mesh = new GL33Mesh(new EAttribute[]{EAttribute.vec3, EAttribute.vec2}, new float[24], new uint[3]);
        Text = text; //keep in mind the also calls the _mesh.ReData method
    }
}