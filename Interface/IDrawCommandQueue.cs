//Represents a set of draw commands to be rendered.

using System.Collections;
using System.Runtime.CompilerServices;

namespace VRenderLib.Interface;

public interface IDrawCommandQueue
{
    /**
    <summary>
        Adds a draw call to the command queue.
    </summary>
    */
    void Draw(
        IRenderTexture texture, IRenderMesh mesh, IRenderShader shader,
        IEnumerable<KeyValuePair<string, object>> uniforms,
        bool depthTest
    );
    void Custom(Action action);
    /**
    <summary>
        Finishes a render queue
        
    </summary>
    */
    void Finish();
}