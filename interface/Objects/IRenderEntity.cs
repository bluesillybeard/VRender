using OpenTK.Mathematics;

namespace Render{
    public interface IRenderEntity{
        EntityPosition Position{get; set;}
        Vector3 Location{get; set;}
        Vector3 Rotation{get; set;}
        Vector3 Scale{get; set;}

        float LocationX{get; set;}
        float LocationY{get; set;}
        float LocationZ{get; set;}

        float RotationX{get; set;}
        float RotationY{get; set;}
        float RotationZ{get; set;}

        float ScaleX{get; set;}
        float ScaleY{get; set;}
        float ScaleZ{get; set;}

        Matrix4 GetTransform();

        IRenderMesh Mesh{get; set;}
        IRenderShader Shader{get; set;}

        IRenderTexture Texture{get; set;}

        bool DepthTest{get; set;}

        IEntityBehavior? Behavior {get; set;}
    }
}