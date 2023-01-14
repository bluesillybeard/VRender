namespace VRender{
    /**
    <summary>
    Represents an object that is contained within a Render.
    Anything that directly extends from IRenderObject is simply a specific type of Rendering object. 
    The specific Render implimentation runs parallel with the IRenderObject types

    the IRenderObject types give a layer that allows a single game to use multiple APIs, and interact with them at the same level.
    It is technically possible to use function pointers, but this is C#, not C.
    </summary>
    */
    public interface IRenderObject{
        ERenderType Type();
        int Id();
    }
}