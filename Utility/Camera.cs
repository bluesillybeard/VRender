using OpenTK.Mathematics;
using System;

//This would be modified code from the OpenTK example, but their Camera class was so bad that I decided to simply write my own.
namespace VRenderLib.Utility;
public class Camera
{
    public const float radToDeg = 180.0f/MathF.PI;
    public const float degToRad = MathF.PI/180.0f;
    public Vector3 Position{get => _position; set{_position = value; _modified = true;}}
    public Vector3 Rotation{
        get => _rotation*radToDeg;
        set{
        _rotation.X = MathHelper.Clamp(value.X, -90f, 90f);
        _rotation.Y = value.Y;
        if(_rotation.Y > 360) _rotation.Y -= 360;
        if(_rotation.Y < 0) _rotation.Y += 360;
        if(_rotation.Z > 360) _rotation.Z -= 360;
        if(_rotation.Z < 0) _rotation.Z += 360;
        _rotation.Z = value.Z;
        _rotation *= degToRad;
        _modified = true;
        }
    }
    public float Fovy{
        get => _fovy * radToDeg;
        set{
            _fovy = MathHelper.Clamp(value, 1f, 179f) * degToRad;
            _modified = true;
        }
    }
    public void SetAspect(Vector2i size)
    {
        _aspect = size.X / ((float)size.Y);
        _modified = true;
    }
    public float Aspect{get => _aspect; set{_aspect = value; _modified = true;}}
    bool _modified;
    private Vector3 _position;
    private Vector3 _rotation;
    private float _fovy; //fov
    private float _aspect; // y/x
    private Matrix4 _transform;

    public Camera(Vector3 position, Vector3 rotation, float fovy, float aspect){
        _position = position;
        Rotation = rotation; //set the rotation using the external set
        Fovy = fovy; //set the fov with the external one
        _aspect = aspect;
        UpdateTransform();
    }

    public Camera(Vector3 position, Vector3 rotation, float fovy, Vector2i size){
        _position = position;
        Rotation = rotation; //set the rotation using the external set
        Fovy = fovy; //set the fov with the external one
        _aspect = ((float)size.X)/((float)size.Y);
        UpdateTransform();
    }
    /**
    <summary>
    Moves the window according to the movement variable.
    X moves right, Y moves up, Z moves forward.
    Note that the Z rotation component is ignored
    </summary>
    */
    public void Move(Vector3 movement){
        if (movement.Z != 0) {
            _position.X -= MathF.Sin(_rotation.Y) * movement.Z;
            _position.Z += MathF.Cos(_rotation.Y) * movement.Z;
        }
        if (movement.X != 0) {
            _position.X += MathF.Cos(_rotation.Y) * movement.X;
            _position.Z += MathF.Sin(_rotation.Y) * movement.X;
        }
        //apply Y movement properly? Not required.
        _position.Y += movement.Y;
        UpdateTransform();
    }

    public Matrix4 GetTransform(){
        if(_modified){
            UpdateTransform();
            _modified = false;
        }
        return _transform;
    }

    private void UpdateTransform(){
        //do the most performance-friendly way of setting the matrix, since it's not gonnna make sense to anybody anyway.
        //initialize our 4x4 "matrix"
        float m00, m01, m02, m03;
        float m10, m11, m12, m13;
        float m20, m21, m22, m23;
        float m30, m31, m32, m33;

        //first, set as identity
        m00 = 1.0f;
        m01 = 0.0f;
        m02 = 0.0f;
        m03 = 0.0f;
        m10 = 0.0f;
        //m11 = 1.0f; //set immediately in next step
        //m12 = 0.0f; //set immediately in next step
        m13 = 0.0f;
        m20 = 0.0f;
        //m21 = 0.0f; //set immediately in next step
        //m22 = 1.0f; //set immediately in next step
        m23 = 0.0f;
        m30 = 0.0f;
        m31 = 0.0f;
        m32 = 0.0f;
        m33 = 1.0f;

        //X rotation - relatively simple since it's identity.
        float sin = MathF.Sin(_rotation.X);
        float cos = MathF.Cos(_rotation.X);
        m11 = cos;
        m12 = sin;
        m21 = -sin;
        m22 = cos;

        //Y rotation - a lot more complicated
        sin = MathF.Sin(_rotation.Y);
        cos = MathF.Cos(_rotation.Y);
        // add temporaries for dependent values
        float nm00 = m00 * cos + m20 * -sin;
        float nm01 = m01 * cos + m21 * -sin;
        float nm02 = m02 * cos + m22 * -sin;
        float nm03 = m03 * cos + m23 * -sin;
        // set non-dependent values directly
        m20 = m00 * sin + m20 * cos;
        m21 = m01 * sin + m21 * cos;
        m22 = m02 * sin + m22 * cos;
        m23 = m03 * sin + m23 * cos;
        // set other values
        m00 = nm00;
        m01 = nm01;
        m02 = nm02;
        m03 = nm03;

        //Z rotation - pretty similar to the Y rotation.
        sin = MathF.Sin(_rotation.Z);
        cos = MathF.Cos(_rotation.Z);
        nm00 = m00 * cos + m10 * sin;
        nm01 = m01 * cos + m11 * sin;
        nm02 = m02 * cos + m12 * sin;
        nm03 = m03 * cos + m13 * sin;
        m10 = m00 * -sin + m10 * cos;
        m11 = m01 * -sin + m11 * cos;
        m12 = m02 * -sin + m12 * cos;
        m13 = m03 * -sin + m13 * cos;
        m00 = nm00;
        m01 = nm01;
        m02 = nm02;
        m03 = nm03;

        //finally, the translation.
        //It's inverted because we are moving the object, not the camera.
        m30 = MathF.FusedMultiplyAdd(m00, -_position.X, MathF.FusedMultiplyAdd(m10, -_position.Y, MathF.FusedMultiplyAdd(m20, -_position.Z, m30)));
        m31 = MathF.FusedMultiplyAdd(m01, -_position.X, MathF.FusedMultiplyAdd(m11, -_position.Y, MathF.FusedMultiplyAdd(m21, -_position.Z, m31)));
        m32 = MathF.FusedMultiplyAdd(m02, -_position.X, MathF.FusedMultiplyAdd(m12, -_position.Y, MathF.FusedMultiplyAdd(m22, -_position.Z, m32)));
        m33 = MathF.FusedMultiplyAdd(m03, -_position.X, MathF.FusedMultiplyAdd(m13, -_position.Y, MathF.FusedMultiplyAdd(m23, -_position.Z, m33)));
        //actually finally, set our matrix to the new values, and multiply by the perspective transform.
        _transform = new Matrix4(
            m00, m01, m02, m03,
            m10, m11, m12, m13,
            m20, m21, m22, m23,
            m30, m31, m32, m33
        ) * Matrix4.CreatePerspectiveFieldOfView(_fovy, _aspect, 1.0f/256.0f, 8192f);
        //OK but seriously: whose idea was it to use matrices? because it's the most idiotic stupid thing ever, and it's genius.
        // Most normal people would just use ordinary math.
    }
}