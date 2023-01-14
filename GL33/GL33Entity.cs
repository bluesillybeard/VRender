using OpenTK.Mathematics;

namespace Render.GL33{
    class GL33Entity: GL33Object, IRenderEntity{
        public GL33Entity(EntityPosition pos, GL33Mesh mesh, GL33Texture texture, GL33Shader shader, int id, bool depthTest, IEntityBehavior? behavior){
            _position = pos;
            _mesh = mesh;
            _texture = texture;
            _shader = shader;
            _id = id;
            _modified = true;
            _depthTest = depthTest;
            Behavior = behavior;
        }

        public Matrix4 GetTransform(){
            if(_modified){
                _currentTransform = GenerateTransform(_position);
            }
            return _currentTransform;
        }

        public static Matrix4 GenerateTransform(EntityPosition pos){
            return Matrix4.Identity *
                Matrix4.CreateScale(pos.scale) *
                Matrix4.CreateFromQuaternion(new Quaternion(pos.rotation)) * 
                Matrix4.CreateTranslation(pos.location);
        }

        public Matrix4 lastTransform; //modified and read externally (in the Render implimentation). Yes it's spaghetti code, but I can't think of a better way to do it.

        public EntityPosition Position{
            get{return _position;}
            set{
                _position = value;
                _modified = true;
            }
        }
        public Vector3 Location{
            get{return _position.location;}
            set{
                _position.location = value;
                _modified = true;
            }
        }
        public Vector3 Rotation{
            get{return _position.rotation;}
            set{
                _position.rotation = value;
                _modified = true;
            }
        }
        public Vector3 Scale{
            get{return _position.scale;}
            set{
                _position.scale = value;
                _modified = true;
            }
        }

        public float LocationX{
            get{return _position.location.X;} 
            set{
                _position.location.X = value;
                _modified = true;
            }
        }
        public float LocationY{
            get{return _position.location.Y;}
            set{
                _position.location.Y = value;
                _modified = true;
            }
        }
        public float LocationZ{
            get{return _position.location.Z;}
            set{
                _position.location.Z = value;
                _modified = true;
            }
        }

        public float RotationX{
            get{return _position.rotation.X;}
            set{
                _position.rotation.X = value;
                _modified = true;
            }
        }
        public float RotationY{
            get{return _position.rotation.Y;}
            set{
                _position.rotation.Y = value;
                _modified = true;
            }
        }
        public float RotationZ{
            get{return _position.rotation.Z;}
            set{
                _position.rotation.Z = value;
                _modified = true;
            }
        }

        public float ScaleX{
            get{return _position.scale.X;}
            set{
                _position.scale.X = value;
                _modified = true;
            }
        }
        public float ScaleY{
            get{return _position.scale.Y;}
            set{
                _position.scale.Y = value;
                _modified = true;
            }
        }
        public float ScaleZ{
            get{return _position.scale.Z;}
            set{
                _position.scale.Z = value;
                _modified = true;
            }
        }

        public IRenderMesh Mesh{
            get{return _mesh;}
            set{_mesh = (GL33Mesh)value;}
        }
        public IRenderShader Shader{
            get{return _shader;}
            set{_shader = (GL33Shader)value;}
        }

        public IRenderTexture Texture{
            get{return _texture;}
            set{_texture = (GL33Texture)value;}
        }

        public bool DepthTest {
            get => _depthTest;
            set => _depthTest = value;
        }

        public IEntityBehavior? Behavior{
            get => _behavior;
            set {
                if(_behavior != null)_behavior.Detach(this);
                _behavior = value;
                if(_behavior != null)_behavior.Attach(this);
            }
        }

        public GL33Mesh _mesh;
        public GL33Shader _shader;

        public GL33Texture _texture;

        public void Id(int id){
            _id = id;
        }

        private EntityPosition _position;

        private Matrix4 _currentTransform;
        private bool _modified;

        public bool _depthTest;

        private IEntityBehavior? _behavior;
    }
}