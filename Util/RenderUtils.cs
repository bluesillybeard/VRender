using System;
using System.Threading;
using System.Collections.Generic;

using vmodel;
using OpenTK.Mathematics;

using Render.GL33;

namespace Render
{
    public static class RenderUtils{
        public const double UpdateTime = 1.0/30.0;
        public const double Pid = 3.141592653589793238462643383279502884197169399375105820974944592307816406286;
        public const float Pif = (float)Pid;
        public const double DegreesToRadiansd = (2*Pid)/180;
        public const double RadiansToDegreesd = 180/(2*Pid);
        public const float DegreesToRadiansf = (2*Pif)/180;
        public const float RadiansToDegreesf = 180/(2*Pif);
        private static object _printMutex = new object(); //makes sure that the print messages don't get screwed up by concurrency

        public const ConsoleColor DefaultBack = ConsoleColor.Black;
        public const ConsoleColor DefaultFront = ConsoleColor.White;

        public const ConsoleColor WarnBack = ConsoleColor.Black;
        public const ConsoleColor WarnFront = ConsoleColor.Yellow;

        public const ConsoleColor ErrorBack = ConsoleColor.Black;
        public const ConsoleColor ErrorFront = ConsoleColor.Red;

        public static void Print(object message){
            lock(_printMutex){
                Console.BackgroundColor = DefaultBack;
                Console.ForegroundColor = DefaultFront;
                Console.Write($"[{Thread.CurrentThread.Name}] {message}");
                Console.ResetColor();
            }
        }

        public static void PrintLn(object message){
            Print($"{message}\n");
        }

        public static void PrintWarn(object message){
            lock(_printMutex){
                Console.BackgroundColor = WarnBack;
                Console.ForegroundColor = WarnFront;
                Console.Write($"[{Thread.CurrentThread.Name}] {message}");
                Console.ResetColor();
            }
        }

        public static void PrintWarnLn(object message){
            PrintWarn($"{message}\n");
        }

        public static void PrintErr(object message){
            lock(_printMutex){
                Console.BackgroundColor = ErrorBack;
                Console.ForegroundColor = ErrorFront;
                Console.Write($"[{Thread.CurrentThread.Name}] {message}");
                Console.ResetColor();
            }
        }

        public static void PrintErrLn(object message){
            PrintErr($"{message}\n");
        }

        public static bool MeshCollides(VMesh mesh, Vector2 pos, Matrix4 transform){
            //TODO: modify for new mesh system. Technically it still works for 3-2-3 pos-tex-norm meshes, but anything else will either crash or have weird behavior.

            pos.Y *= -1;

            //in the Java version, I used temporary variables since they are always on the Heap anyway, so cache locality was an unfixable problem.
            // In C# however, Vectors are stack allocated (reference by value), and thus using temp variables on the Heap would actually result in WORSE performance
            // since it would have to access data from the Heap, which is less cache friendly.

            uint[] indices = mesh.indices;
            float[] vertices = mesh.vertices;
            const int elements = 8; //8 elements per vertex]
            for(int i=0; i<indices.Length/3; i++){ //each triangle in the mesh
                //get the triangle vertices and transform the triangle to the screen coordinates.
                //We use Vector4s for the matrix transformation to work.

                uint t = elements*indices[3*i];
                Vector3 v1 = Vector3.TransformPerspective(new Vector3(vertices[t], vertices[t+1], vertices[t+2]), transform);

                t = elements*indices[3*i+1];
                Vector3 v2 = Vector3.TransformPerspective(new Vector3(vertices[t], vertices[t+1], vertices[t+2]), transform);
                
                t = elements*indices[3*i+2];
                Vector3 v3 = Vector3.TransformPerspective(new Vector3(vertices[t], vertices[t+1], vertices[t+2]), transform);
                //if the triangle isn't behind the camera, and it touches the point, return true.
                if(v1.Z < 1.0f && v2.Z < 1.0f && v3.Z < 1.0f && IsInside(v1.Xy, v2.Xy, v3.Xy, pos)) {
                    return true;
                }
            }
            return false;
        }


        //thanks to https://www.tutorialspoint.com/Check-whether-a-given-point-lies-inside-a-Triangle for the following code
        //I adapted it to fit my code better, and to fix a bug related to float precision

        public static double TriangleArea(Vector2 A, Vector2 B, Vector2 C) {
            return Math.Abs((A.X * (B.Y - C.Y) + B.X * (C.Y - A.Y) + C.X * (A.Y - B.Y)) / 2.0);
        }

        public static bool IsInside(Vector2 A, Vector2 B, Vector2 C, Vector2 p) {
            double area  = TriangleArea(A, B, C) + .0000177;//area of triangle ABC with a tiny bit of extra to avoid issues related to float precision errors
            double area1 = TriangleArea(p, B, C);           //area of PBC
            double area2 = TriangleArea(A, p, C);           //area of APC
            double area3 = TriangleArea(A, B, p);           //area of ABP

            return (area >= area1 + area2 + area3);        //when three triangles are forming the whole triangle
            //I changed it to >= because floats cannot be trusted to hold perfectly accurate data,
        }

        public static IRender? CreateIdealRender(RenderSettings settings, out Exception? e){
            try{
                e = null;
                return new GL33Render(settings);
            }
            catch (Exception ex)
            {
                e = ex;
                return null;
            }
            //TODO: update when adding new Render implementations
        }
        //This method is a faster way for people who don't give a crap about error handling.
        public static IRender CreateIdealRenderOrDie(RenderSettings settings){
            IRender? render = CreateIdealRender(settings, out var e);
            if(render is null)
            {
                if(e is null)
                {
                    throw new Exception("null Render, no error, much confused");
                }
                throw new Exception("Your render didn't get made properly.", e);
            }
            return render;
        }
    }
}