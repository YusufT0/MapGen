using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text;
using System.Globalization;
using System;

public static class FBXtoOBJExporter
{
    // Converts an external FBX file to OBJ and MTL files in the Assets/Temp folder
    public static (string objPath, string mtlPath) ConvertExternalFBX(string externalFbxPath)
    {
        // Eðer kullanýcý sadece dosya adý yazdýysa ve bu yol mutlak deðilse
        if (!Path.IsPathRooted(externalFbxPath))
        {
            // Eðer yol zaten "Assets/" ile baþlýyorsa, doðrudan çöz
            if (externalFbxPath.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
            {
                string fullPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, externalFbxPath);

                if (File.Exists(fullPath))
                {
                    externalFbxPath = fullPath;
                }
                else
                {
                    Debug.LogError("Relative path is invalid and not found inside Assets: " + fullPath);
                    return (null, null);
                }
            }
            else
            {
                // Diðer tüm göreli yollar için Assets altýna ekle
                string assumedUnityPath = Path.Combine(Application.dataPath, externalFbxPath);

                if (File.Exists(assumedUnityPath))
                {
                    externalFbxPath = assumedUnityPath;
                }
                else
                {
                    Debug.LogError("Relative path is invalid and not found inside Assets: " + assumedUnityPath);
                    return (null, null);
                }
            }
        }

        // Check if the input FBX file exists
        if (!File.Exists(externalFbxPath))
        {
            Debug.LogError("Invalid FBX path: " + externalFbxPath);
            return (null, null);
        }

        string tempFolder = "Assets/Temp";

        // Create the Temp folder if it does not exist
        if (!Directory.Exists(tempFolder))
            Directory.CreateDirectory(tempFolder);

        string fbxFileName = Path.GetFileName(externalFbxPath);
        string tempFbxPath = Path.Combine(tempFolder, fbxFileName);

        // Copy the FBX file into the Temp folder (overwrite if exists)
        File.Copy(externalFbxPath, tempFbxPath, true);

        // Import the copied asset so Unity recognizes it
        AssetDatabase.ImportAsset(tempFbxPath);
        AssetDatabase.Refresh();

        // Load the FBX file as a GameObject asset
        GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(tempFbxPath);
        if (go == null)
        {
            Debug.LogError("Failed to load FBX asset.");
            return (null, null);
        }

        // Get the MeshFilter and Renderer components from the imported asset
        MeshFilter meshFilter = go.GetComponentInChildren<MeshFilter>();
        Renderer renderer = go.GetComponentInChildren<Renderer>();

        // Check if mesh or renderer components are missing
        if (meshFilter == null || renderer == null)
        {
            Debug.LogError("MeshFilter or Renderer not found.");
            return (null, null);
        }

        // Access the mesh and material data
        Mesh mesh = meshFilter.sharedMesh;
        Material material = renderer.sharedMaterial;

        // Validate mesh and material
        if (mesh == null || material == null)
        {
            Debug.LogError("Mesh or Material is null.");
            return (null, null);
        }

        // Prepare output file paths for OBJ and MTL files
        string baseName = Path.GetFileNameWithoutExtension(fbxFileName);
        string objPath = Path.Combine(tempFolder, baseName + ".obj");
        string mtlPath = Path.Combine(tempFolder, baseName + ".mtl");

        // Write the MTL (material) file
        File.WriteAllText(mtlPath, CreateMTL(material));

        // Write the OBJ file, referencing the MTL file and mesh data
        using (StreamWriter sw = new StreamWriter(objPath, false, Encoding.UTF8))
        {
            sw.WriteLine($"mtllib {Path.GetFileName(mtlPath)}");            // Reference the material file
            sw.WriteLine($"usemtl {material.name.Replace(" ", "_")}");     // Use the material name
            sw.Write(MeshToString(mesh, go.transform));                    // Write vertex, normal, UV, and face data
        }

        // Refresh AssetDatabase so Unity recognizes the new files
        AssetDatabase.Refresh();

        Debug.Log("OBJ and MTL files created:");
        Debug.Log("OBJ: " + objPath);
        Debug.Log("MTL: " + mtlPath);

        // Return absolute paths for further use
        return (Path.GetFullPath(objPath), Path.GetFullPath(mtlPath));
    }

    // Creates the content for the MTL file based on the material properties
    private static string CreateMTL(Material mat)
    {
        StringBuilder sb = new StringBuilder();
        string matName = mat.name.Replace(" ", "_");

        sb.AppendLine($"newmtl {matName}");

        // Use the material color if available, otherwise default to white
        Color color = mat.HasProperty("_Color") ? mat.color : Color.white;
        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "Kd {0} {1} {2}", color.r, color.g, color.b)); // Diffuse color
        sb.AppendLine("Ka 0.000 0.000 0.000"); // Ambient color
        sb.AppendLine("Ks 0.000 0.000 0.000"); // Specular color
        sb.AppendLine("Ns 10.000");             // Specular exponent
        sb.AppendLine("d 1.0");                 // Dissolve (opacity)

        return sb.ToString();
    }

    // Converts the mesh data into the OBJ file format as a string
    private static string MeshToString(Mesh mesh, Transform transform)
    {
        StringBuilder sb = new StringBuilder();
        CultureInfo ci = CultureInfo.InvariantCulture;

        // Write vertices (v)
        foreach (Vector3 v in mesh.vertices)
        {
            Vector3 wv = transform.TransformPoint(v);
            sb.AppendLine(string.Format(ci, "v {0} {1} {2}", wv.x, wv.y, wv.z));
        }

        // Write vertex normals (vn)
        foreach (Vector3 n in mesh.normals)
        {
            Vector3 wn = transform.TransformDirection(n);
            sb.AppendLine(string.Format(ci, "vn {0} {1} {2}", wn.x, wn.y, wn.z));
        }

        // Write texture coordinates (vt)
        foreach (Vector2 uv in mesh.uv)
        {
            sb.AppendLine(string.Format(ci, "vt {0} {1}", uv.x, uv.y));
        }

        // Write faces (f) per submesh, triangles grouped by 3 indices
        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            int[] triangles = mesh.GetTriangles(i);
            for (int j = 0; j < triangles.Length; j += 3)
            {
                int a = triangles[j] + 1;
                int b = triangles[j + 1] + 1;
                int c = triangles[j + 2] + 1;
                // OBJ indices are 1-based and include vertex/uv/normal indices
                sb.AppendLine($"f {a}/{a}/{a} {b}/{b}/{b} {c}/{c}/{c}");
            }
        }

        return sb.ToString();
    }
}