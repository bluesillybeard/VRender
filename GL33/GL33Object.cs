namespace VRender.GL33{
    class GL33Object: IRenderObject{
        public ERenderType Type(){
            return ERenderType.GL33;
        }

        public int Id(){
            return _id;
        }

        protected int _id;
    }
}