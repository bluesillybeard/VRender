namespace VRenderLib.Interface;

using vmodel;
public interface IRenderShader : IDisposable
{
    Attributes GetAttributes();

    /**
    <summary>
        Returns if this shader has already been disposed.
        Doing anything with a shader that has been disposed of is undefined behavior.
    </summary>
    */
    bool IsDisposed();

    ShaderFeatures? GetFeatures();
}

//A simple shader features, for simple shaders.
public struct ShaderFeatures
{
    public ShaderFeatures(Attributes attributes, bool applyModelTransform, bool applyCameraTransform)
    {
        this.attributes = attributes;
        this.applyCameraTransform = applyCameraTransform;
        this.applyModelTransform = applyModelTransform;
    }
    /**
    <summary>
        The vertex attributes the shader should support.
        Note that basic attributes (scalar, vec2, vec3, vec4) are not supported;
        they must have a proper purpose.
    </summary>
    */
    public Attributes attributes;
    public bool applyModelTransform;
    public bool applyCameraTransform;
}

