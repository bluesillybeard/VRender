namespace VRender;

using OpenTK.Windowing.GraphicsLibraryFramework;
public interface IEntityBehavior{
    void Update(double absoluteTime, double deltaTime, IRenderEntity entity, KeyboardState keyboard, MouseState mouse);
    //Todo: finish laying out the interface

    void Attach(IRenderEntity entity);

    void Detach(IRenderEntity entity);
}