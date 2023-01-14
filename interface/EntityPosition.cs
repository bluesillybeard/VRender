using System.Diagnostics.CodeAnalysis;
using OpenTK.Mathematics;

namespace Render{
    public struct EntityPosition{
        public static EntityPosition Zero = new EntityPosition(Vector3.Zero, Vector3.Zero, Vector3.One);

        public EntityPosition(Vector3 location_, Vector3 rotation, Vector3 scale_){
            location = location_;
            this.rotation = rotation;
            scale = scale_;

        }
        public Vector3 location;
        public Vector3 rotation;
        public Vector3 scale;
        public override bool Equals(object? obj)
        {
            if(obj is null)return false;
            if(obj is EntityPosition pos){
                if(this.location == pos.location
                && this.rotation == pos.rotation
                && this.scale == pos.scale) return true;
            }
            return false;
        }
        public override int GetHashCode()
        {
            return location.GetHashCode()*3 + rotation.GetHashCode()*5 + scale.GetHashCode()*7;
        }
        public override string ToString()
        {
            return $"pos:{location} rot:{rotation} s:{scale}";
        }
        public static EntityPosition operator+ (EntityPosition left, EntityPosition right){
            return new EntityPosition(left.location + right.location, left.rotation + right.rotation, left.scale + right.scale);
        }

        public static EntityPosition operator* (EntityPosition left, EntityPosition right){
            return new EntityPosition(left.location * right.location, left.rotation * right.rotation, left.scale * right.scale);
        }

        public static EntityPosition operator* (EntityPosition left, float right){
            return new EntityPosition(left.location * right, left.rotation * right, left.scale * right);
        }
        public static EntityPosition operator* (EntityPosition left, int right){
            return new EntityPosition(left.location * right, left.rotation * right, left.scale * right);
        }
    }
}