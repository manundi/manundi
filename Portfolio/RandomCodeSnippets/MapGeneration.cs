
/*

Testing landmass generation in Unity. 
Inspired by awesome youtube videos of: https://github.com/SebLague/
This creates an island.

*/


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MapGenerator : MonoBehaviour
{
    public float rantaKorkeus = 1, vihreeKorkeus  = 3, ruskeeKorkeus =  5, vuoriKorkeus= 8;
    public List<float> limits;
    public Renderer rend;
    public MeshFilter meshF;
    public MeshFilter pohjaMeshF;
    public Renderer pohjaRend;
    public int seed;
    public bool autoUpdate;
    public int mapHeight;
    public int mapWidth;
    public float scale;

    public float[,] noiseMap;

    public int octaves;
    public float persistance;
    public float lacunarity;

    public float heightMultiplyer;
    public float middleHeightMultiplyer;
    public Vector2 offset;
    Vector2[] octaveOffsets;
    public GameObject player;

    private void Awake()
    {

       
        Generate();
        
    }
    // Start is called before the first frame update
    public void Generate()
    {
        seed = (int)Random.Range(1, 1000);
        System.Random prng = new System.Random(seed);
        octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) + offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }


        rend.transform.localScale = new Vector3(mapWidth, 0, mapHeight);
        //pohjaRend.transform.localScale = new Vector3(mapWidth, 0, mapHeight);
        
        GenerateMap();
        //DrawNoiseMap();
        Mesh terrainMesh = GenerateTerrainMesh(noiseMap, heightMultiplyer/10, middleHeightMultiplyer/100, rantaKorkeus, vihreeKorkeus, ruskeeKorkeus, vuoriKorkeus).CreateMesh();
       
        meshF.sharedMesh = terrainMesh;
        pohjaMeshF.sharedMesh = terrainMesh;

        MeshCollider[] mesCols = meshF.gameObject.GetComponents<MeshCollider>();
        if (mesCols.Length > 0)
        {

            foreach (var item in mesCols)
            {
                DestroyImmediate(item);

            }


        }
        player.SetActive(false);
        //Time.timeScale = 0;
        //hack joka korjaa jos collider piirtyy v��rin 
        StartCoroutine(AddCollider());
    }

    IEnumerator AddCollider()
    {
        yield return new WaitForSeconds(1.5f);
        if (meshF.gameObject.GetComponent<MeshCollider>()  == null){
            MeshCollider col = meshF.gameObject.AddComponent<MeshCollider>();
            col.sharedMesh = null;
            col.sharedMesh = meshF.sharedMesh;
            col.cookingOptions &= ~MeshColliderCookingOptions.UseFastMidphase;
        } 
       
    
        Time.timeScale = 1;
        FindObjectOfType<InsertProps>().GetComponent<InsertProps>().Props();
        player.SetActive(true);

    }

    void DrawNoiseMap()
    {
        Texture2D mapText = new Texture2D(mapWidth, mapHeight);
        Color[] colorMap = new Color[mapHeight * mapWidth];

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                colorMap[y * mapWidth + x] = Color.Lerp(Color.black, Color.white, noiseMap[x, y]);
            }
        }
        mapText.SetPixels(colorMap);
        mapText.Apply();
        //rend.sharedMaterial.mainTexture = mapText;
    }

    void GenerateMap()
    {
      
        noiseMap = new float[mapWidth,mapHeight];
        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;
                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = x / scale * frequency + octaveOffsets[i].x;
                    float sampleY = y / scale *frequency+ + octaveOffsets[i].y;
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;
                    amplitude *= persistance;
                    frequency *= lacunarity;
                }
                if (noiseHeight > maxNoiseHeight) maxNoiseHeight = noiseHeight;
                if (noiseHeight < minNoiseHeight) minNoiseHeight = noiseHeight;
                noiseMap[x, y] = noiseHeight;
            }
        }

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
            }
        }
        print("Noisekoko");
        print(noiseMap.Length);
    }

    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplyer, float middleHeightMultiplyer, float rantaKorkeus, float vihreeKorkeus, float ruskeeKorkeus, float vuoriKorkeus)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (height - 1) / 2f;

        MeshData meshData = new MeshData(width*2, height*2);
        int vertexIndex = 0;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float newHeight = heightMap[x, y] * (heightMultiplyer / 100 * ((width / 2 - Mathf.Abs(width / 2 - x)) * (height / 2 - Mathf.Abs(height / 2 - y))) * middleHeightMultiplyer);
                meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, newHeight, topLeftZ - y);
                meshData.uvs[vertexIndex] = new Vector2(x / (float)width, y / (float)height);

                if (x < width - 1 && y < height - 1)
                {
                    meshData.AddTriangle(vertexIndex, vertexIndex + width + 1, vertexIndex + width, newHeight, rantaKorkeus, vihreeKorkeus, ruskeeKorkeus, vuoriKorkeus); 
                    meshData.AddTriangle(vertexIndex + width + 1, vertexIndex, vertexIndex + 1, newHeight, rantaKorkeus, vihreeKorkeus, ruskeeKorkeus, vuoriKorkeus);
                }

                vertexIndex++;
            }
        }

        return meshData;

    }
}

public class MeshData
{
    public Vector3[] vertices;
    public int[] triangles1;
    public int[] triangles2;
    public int[] triangles3;
    public int[] triangles4;
    public int[] triangles5;
    public Vector2[] uvs;
    public float[] heights;

    int triangleIndex1;
    int triangleIndex2;
    int triangleIndex3;
    int triangleIndex4;
    int triangleIndex5;

    public MeshData(int meshWidth, int meshHeight)
    {
        vertices = new Vector3[meshWidth * meshHeight];
        uvs = new Vector2[meshWidth * meshHeight];
        triangles1 = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
        triangles2 = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
        triangles3 = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
        triangles4 = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
        triangles5 = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
    }

    public void AddTriangle(int a, int b, int c,float height, float rantaKorkeus, float vihreeKorkeus, float ruskeeKorkeus, float vuoriKorkeus)
    {
        //rantahiekka
        if(height<= rantaKorkeus)
        {
            triangles1[triangleIndex1] = a;
            triangles1[triangleIndex1 + 1] = b;
            triangles1[triangleIndex1 + 2] = c;
            triangleIndex1 += 3;
        }
        if (height <= vihreeKorkeus && height > rantaKorkeus)
        {
            triangles2[triangleIndex2] = a;
            triangles2[triangleIndex2 + 1] = b;
            triangles2[triangleIndex2 + 2] = c;
            triangleIndex2 += 3;
        }
        if (height <= ruskeeKorkeus && height >vihreeKorkeus)
        {
            triangles3[triangleIndex3] = a;
            triangles3[triangleIndex3 + 1] = b;
            triangles3[triangleIndex3 + 2] = c;
            triangleIndex3 += 3;
        }
        if (height <= vuoriKorkeus && height > ruskeeKorkeus)
        {
            triangles4[triangleIndex4] = a;
            triangles4[triangleIndex4 + 1] = b;
            triangles4[triangleIndex4 + 2] = c;
            triangleIndex4 += 3;
        }
        //lumi
        if ( height > vuoriKorkeus)
        {
            triangles5[triangleIndex5] = a;
            triangles5[triangleIndex5 + 1] = b;
            triangles5[triangleIndex5 + 2] = c;
            triangleIndex5 += 3;
        }


    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.subMeshCount = 5;
        mesh.vertices = vertices;
        mesh.SetTriangles(triangles1, 0);
        mesh.SetTriangles(triangles2, 1);
        mesh.SetTriangles(triangles3, 2);
        mesh.SetTriangles(triangles4, 3);
        mesh.SetTriangles(triangles5, 4);
        
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        return mesh;
    }
}
