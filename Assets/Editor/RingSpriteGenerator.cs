using System.IO;
using UnityEditor;
using UnityEngine;

public class RingSpriteGenerator : EditorWindow
{
    [MenuItem("Tools/Generate Cooldown Ring Sprite")]
    public static void GenerateRing()
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        float center = (size - 1) * 0.5f;
        float outerRadius = size * 0.45f;
        float innerRadius = size * 0.34f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));

                if (dist <= outerRadius && dist >= innerRadius)
                {
                    float outerAlpha = Mathf.Clamp01((outerRadius - dist) * 1.5f);
                    float innerAlpha = Mathf.Clamp01((dist - innerRadius) * 1.5f);
                    float alpha = Mathf.Min(outerAlpha, innerAlpha);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }

        tex.Apply();

        string folderPath = "Assets/Sprites";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "Sprites");
        }

        string filePath = Path.Combine(folderPath, "CooldownRing.png");
        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        DestroyImmediate(tex);

        AssetDatabase.Refresh();

        TextureImporter importer = AssetImporter.GetAtPath(filePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        Debug.Log("Sprite cincin berhasil dibuat di: " + filePath);
    }
}