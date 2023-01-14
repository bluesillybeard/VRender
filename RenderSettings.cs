using OpenTK.Mathematics;


namespace Render{
    public struct RenderSettings{
        //default
        public RenderSettings()
        {
            Size = new Vector2i(800, 600);
            Dir = "Resources/";
            Fov = 90*RenderUtils.DegreesToRadiansf;
            TargetFrameTime = 1.0/60.0;
            VSync = false;
            WindowTitle = "Voxelesque window";
            BackgroundColor = 0x666666ff;
        }
        public Vector2i Size;

        ///<summary>contains the starting assets directory</summary>
        public string Dir;

        public float Fov;

        ///<summary> how long each frame should take. Frames may take shorter or longer. Defaults to 1/60 </summary>
        public double TargetFrameTime;

        public bool VSync;

        public string WindowTitle;

        public uint BackgroundColor; //RGBA clear color.
    
    }
}