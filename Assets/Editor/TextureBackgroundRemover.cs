using UnityEngine;
using UnityEditor;
using System.IO;

public class TextureBackgroundRemover : EditorWindow
{
    [MenuItem("Tools/Remove Solid Background from tui do")]
    public static void RemoveBackground()
    {
        string path = "Assets/tainguyen/item/túi đồ.png";
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("Không tìm thấy file ảnh túi đồ tại path: " + path);
            return;
        }

        // Tạm thời bật isReadable và tắt compression để có thể đọc/ghi pixel
        bool wasReadable = importer.isReadable;
        TextureImporterCompression oldCompression = importer.textureCompression;
        importer.isReadable = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        AssetDatabase.Refresh();

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex == null)
        {
            Debug.LogError("Không thể load Texture2D của túi đồ!");
            return;
        }

        // Tạo Texture mới để thay đổi pixel
        Texture2D newTex = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
        Color[] pixels = tex.GetPixels();
        
        // Lấy màu nền từ góc trái dưới (0, 0)
        Color bgColor = pixels[0];
        
        // Ngưỡng sai số màu (Tolerance) - Có thể điều chỉnh nếu cần tách rộng hơn hoặc hẹp hơn
        float tolerance = 0.12f;
        int changedCount = 0;

        for (int i = 0; i < pixels.Length; i++)
        {
            Color c = pixels[i];
            
            // Tính toán khoảng cách màu (Euclidean Distance trong không gian RGB)
            float dist = Mathf.Sqrt(
                Mathf.Pow(c.r - bgColor.r, 2) +
                Mathf.Pow(c.g - bgColor.g, 2) +
                Mathf.Pow(c.b - bgColor.b, 2)
            );

            // Nếu màu trùng hoặc rất gần màu nền, đặt độ trong suốt alpha = 0 (tách nền)
            if (dist < tolerance)
            {
                pixels[i] = new Color(0f, 0f, 0f, 0f);
                changedCount++;
            }
        }

        newTex.SetPixels(pixels);
        newTex.Apply();

        // Lưu đè lại file PNG
        byte[] pngData = newTex.EncodeToPNG();
        File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), path), pngData);

        // Khôi phục lại cài đặt TextureImporter ban đầu
        importer.isReadable = wasReadable;
        importer.textureCompression = oldCompression;
        importer.textureType = TextureImporterType.Sprite; // Đảm bảo vẫn là Sprite
        importer.filterMode = FilterMode.Point; // Giữ nguyên Point Filter cho nét pixel
        importer.SaveAndReimport();
        AssetDatabase.Refresh();

        Debug.Log($"<color=green>[TextureBackgroundRemover]</color> Đã tách nền thành công cho túi đồ! Đã chuyển đổi {changedCount} pixel nền sang trong suốt.");
    }
}
