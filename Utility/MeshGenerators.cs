namespace VRender.Utility;

using vmodel;

using System.Collections.Generic;
public static class MeshGenerators{
    public static readonly Attributes defaultTextAttributes = new Attributes(new EAttribute[]{EAttribute.position, EAttribute.textureCoords});
    public const int defaultTextPosAttrib = 0;
    public const int defaultTextTexAttrib = 1;
    private const int cacheLimit = 1000;
    //Creates a text mesh using the default attributes - see 'defaultTextAttributes'
    //text is the text to convert
    //centerx and centery tell weather or not to center in certain directions
    //error is a string that tells what went wrong if this function returns null
    public static VMesh? BasicText(string text, bool centerX, bool centerY, out string? error)
    {
        return BasicText(text, centerX, centerY, defaultTextAttributes, defaultTextPosAttrib, defaultTextTexAttrib, out error);
    }
    //text is the text to convert
    //centerx and centery tell weather or not to center in certain directions
    //attributes are the output attributes of the mesh.
    //posAttrib and texAttrib are the indices of the position and texture coordinate attributes respectively
    //error is a string that tells what went wrong if this function returns null
    public static VMesh? BasicText(string text, bool centerX, bool centerY, Attributes attributes, int posAttrib, int texAttrib, out string? error)
    {
        //NOTE: this code is not great.

        //COMMENT FROM MODEL REFORM: The model reform made this a little but more complicated,
        // but it's still the same underlying algorithm as in the original Java code.

        //First we need to look in the cache if it already exists
        int hash = (text, attributes).GetHashCode();
        if(stringMeshCache.TryGetValue(hash, out var cachedMesh))
        {
            //Since the mesh already exists in the cache, we can just return it
            error = null;
            return cachedMesh;
        }

        //Check that the attribute target is valid
        if(posAttrib >= attributes.Length || texAttrib >= attributes.Length){
            error = ("Invalid attribute index " + posAttrib + "/" + texAttrib + " for attributes {" + string.Join(", ", attributes) + "}");
            return null;
        }
        //sort the text to a more readable form
        List<List<char>> lines = new List<List<char>>();
        List<char> line = new List<char>();
        int numCharacters = 0;
        //split into lines
        foreach(char character in text)
        {
            if(character == '\n')
            {
                lines.Add(line);
                line = new List<char>();
            }
            else
            {
                line.Add(character);
                numCharacters++;
            }
        }
        lines.Add(line);
        //generate the actual mesh

        //Initialize some values to help the algorithm understand the desired attribute output
        int totalAttrib = 0;
        int posAttribOffset = 0;
        int texAttribOffset = 0;
        for(int i=0; i<attributes.Length; i++)
        {
            EAttribute attribute = attributes[i];
            totalAttrib += (int)attribute % 5;
            if(i < posAttrib)
            {
                posAttribOffset += (int)attribute % 5;
            }
            if(i < texAttrib)
            {
                texAttribOffset += (int)attribute % 5;
            }
        }
        if(totalAttrib < 4)
        {
            error = "Not enough attributes";
            return null;
        }
        int extraAttrib  = totalAttrib - 4;
        //We use the previously mentioned values to create a mapping
        int[] mapping = new int[totalAttrib];
        Array.Fill(mapping, -1);//initialize values to -1
        mapping[posAttribOffset  ] = 0;
        mapping[posAttribOffset+1] = 1;
        mapping[texAttribOffset  ] = 2;
        mapping[texAttribOffset+1] = 3;

        MeshBuilder builder = new MeshBuilder(attributes, numCharacters*4*totalAttrib, numCharacters*6);
        float YStart = centerY ? -lines.Count/2f : 0; //the farthest up coordinate of the text.
        for(int i=0; i < lines.Count; i++)
        {
            float XStart = centerX ? -lines[i].Count/2f : 0; //the farthest left coordinate of the text.
            for(int j=0; j<lines[i].Count; j++)
            {
                char character = lines[i][j];
                int column = character & 15;
                int row = character >> 4 & 15; //get the last 4 bits and first 4 bits (row and column from the ASCII texture)
                float iXStart = XStart+j;
                float iYStart = YStart-i;
                float UVXPosition = column*0.0625f;
                float UVYPosition = row*0.0625f; //get the actual UV coordinates of the top left corner
                //Calculate vertices
                float[] topLeft     = new float[]{iXStart  , iYStart  , UVXPosition        , UVYPosition        };
                float[] topRight    = new float[]{iXStart+1, iYStart  , UVXPosition+0.0625f, UVYPosition        };
                float[] bottomLeft  = new float[]{iXStart  , iYStart-1, UVXPosition        , UVYPosition+0.0625f};
                float[] bottomRight = new float[]{iXStart+1, iYStart-1, UVXPosition+0.0625f, UVYPosition+0.0625f};
                //FIRST TRIANGLE
                builder.AddVertex(mapping, topLeft);
                builder.AddVertex(mapping, topRight);
                builder.AddVertex(mapping, bottomLeft);
                //SECOND TRIANGLE
                builder.AddVertex(mapping, topRight);
                builder.AddVertex(mapping, bottomLeft);
                builder.AddVertex(mapping, bottomRight);
            }
        }
        error = null;
        VMesh mesh = builder.ToMesh();
        // We need to add this mesh to the cache
        // First, add the item to the cache and cache limit
        // We can reuse the hash from earlier
        stringMeshCache.Add(hash, mesh);
        cacheLimiter.Append(hash);
        //Then we remove one if the cache is "full"
        if(cacheLimiter.Count > cacheLimit)
        {
            #nullable disable
            int hashToRemove = cacheLimiter.Dequeue();
            stringMeshCache.Remove(hashToRemove);
            #nullable restore
        }
        return mesh;
    }
    private static Dictionary<int, VMesh> stringMeshCache = new Dictionary<int, VMesh>();
    // We need to keep the cache from constantly increasing memory usage
    // I would prefer to use a priority system, but I am simply too lazy for that
    private static Queue<int> cacheLimiter = new Queue<int>();
}