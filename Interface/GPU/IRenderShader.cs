namespace VRender.Interface;

using vmodel;
public interface IRenderShader : IDisposable
{
    /**
    <summary>
        Returns a span containing the attributes that this shader expects for its mesh input.
    </summary>
    */
    ReadOnlySpan<EAttribute> GetAttributes();

    /**
    <summary>
        Returns if this shader has already been disposed.
        Doing anything with a shader that has been disposed of is undefined behavior.
    </summary>
    */
    bool IsDisposed();

    ShaderFeatures GetFeatures();
}

public struct ShaderFeatures
{
    public ShaderFeatures(EAttribute[] attributes, bool applyModelTransform, bool applyCameraTransform)
    {
        this.attributes = attributes;
        this.applyCameraTransform = applyCameraTransform;
        this.applyModelTransform = applyCameraTransform;
    }
    /**
    <summary>
        The vertex attributes the shader should support.
        Note that basic attributes (scalar, vec2, vec3, vec4) are not supported;
        they must have a proper purpose.
    </summary>
    */
    public EAttribute[] attributes;
    public bool applyModelTransform;
    public bool applyCameraTransform;
    //TODO: bool applyNormalBackfaceCulling;
}