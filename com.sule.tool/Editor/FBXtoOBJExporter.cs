using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text;
using System.Globalization;
using System;

public static class FBXtoOBJExporter
{
    // Converts an external FBX file to OBJ and MTL files inside the Assets/Temp folder.
    // Returns absolute paths to the generated OBJ and MTL files, or (null, null) on failure.
    public static (string objPath, string mtlPath) ConvertExternalFBX(string externalFbxPath)
    {
        // If the given path is not absolute
        if (!Path.IsPathRooted(externalFbxPath))
        {
            // If the path starts with "Assets", resolve it relative to the project root folder
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
                // For all other relative paths, assume they are inside the Assets folder
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

        // Check if the input FBX file exists at the resolved path
        if (!File.Exists(externalFbxPath))
        {
            Debug.LogError("Invalid FBX path: " + externalFbxPath);
            return (null, null);
        }

        string tempFolder = "Assets/Temp";

        // Create the Temp folder if it does not already exist
        if (!Directory.Exists(tempFolder))
            Directory.CreateDirectory(tempFolder);

        string fbxFileName = Path.GetFileName(externalFbxPath);
        string tempFbxPath = Path.Combine(tempFolder, fbxFileName);

        // Copy the FBX file into the Temp folder (overwrite if it already exists)
        File.Copy(externalFbxPath, tempFbxPath, true);

        // Import the copied asset so Unity recognizes it in the project
        AssetDatabase.ImportAsset(tempFbxPath);
        AssetDatabase.Refresh();

        // Load the FBX file as a GameObject asset
        GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(tempFbxPath);
        if (go == null)
        {
            Debug.LogError("Failed to load FBX asset.");
            return (null, null);
        }

        // Retrieve MeshFilter and Renderer components from the imported GameObject
        MeshFilter meshFilter = go.GetComponentInChildren<MeshFilter>();
        Renderer renderer = go.GetComponentInChildren<Renderer>();

        // Verify that mesh and renderer components are found
        if (meshFilter == null || renderer == null)
        {
            Debug.LogError("MeshFilter or Renderer not found.");
            return (null, null);
        }

        // Access the mesh and material data from the components
        Mesh mesh = meshFilter.sharedMesh;
        Material material = renderer.sharedMaterial;

        // Validate mesh and material are not null
        if (mesh == null || material == null)
        {
            Debug.LogError("Mesh or Material is null.");
            return (null, null);
        }

        // Prepare file paths for the output OBJ and MTL files in the Temp folder
        string baseName = Path.GetFileNameWithoutExtension(fbxFileName);
        string objPath = Path.Combine(tempFolder, baseName + ".obj");
        string mtlPath = Path.Combine(tempFolder, baseName + ".mtl");

        // Write the MTL file with material properties
        File.WriteAllText(mtlPath, CreateMTL(material));

        // Write the OBJ file, referencing the MTL file and including mesh data
        using (StreamWriter sw = new StreamWriter(objPath, false, Encoding.UTF8))
        {
            sw.WriteLine($"mtllib {Path.GetFileName(mtlPath)}");          // Reference the material library
            sw.WriteLine($"usemtl {material.name.Replace(" ", "_")}");   // Use the material by name
            sw.Write(MeshToString(mesh, go.transform));                  // Write vertices, normals, UVs, and faces
        }

        // Refresh AssetDatabase so Unity updates the project files view
        AssetDatabase.Refresh();

        Debug.Log("OBJ and MTL files created:");
        Debug.Log("OBJ: " + objPath);
        Debug.Log("MTL: " + mtlPath);

        // Return absolute paths to the created files
        return (Path.GetFullPath(objPath), Path.GetFullPath(mtlPath));
    }

    // Creates the content of the MTL file based on the given material's properties
    private static string CreateMTL(Material mat)
    {
        StringBuilder sb = new StringBuilder();
        string matName = mat.name.Replace(" ", "_");

        sb.AppendLine($"newmtl {matName}");

        // Use the material's color property if it exists, else default to white
        Color color = mat.HasProperty("_Color") ? mat.color : Color.white;

        // Write the diffuse color (Kd) with invariant culture decimal formatting
        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "Kd {0} {1} {2}", color.r, color.g, color.b));
        sb.AppendLine("Ka 0.000 0.000 0.000"); // Ambient color (black)
        sb.AppendLine("Ks 0.000 0.000 0.000"); // Specular color (black)
        sb.AppendLine("Ns 10.000");             // Specular exponent
        sb.AppendLine("d 1.0");                 // Dissolve (opacity) fully opaque

        return sb.ToString();
    }

    // Converts the mesh data into OBJ file format text, including vertices, normals, UVs, and faces
    private static string MeshToString(Mesh mesh, Transform transform)
    {
        StringBuilder sb = new StringBuilder();
        CultureInfo ci = CultureInfo.InvariantCulture;

        // Write vertex positions (v), transformed to world space
        foreach (Vector3 v in mesh.vertices)
        {
            Vector3 wv = transform.TransformPoint(v);
            sb.AppendLine(string.Format(ci, "v {0} {1} {2}", wv.x, wv.y, wv.z));
        }

        // Write vertex normals (vn), transformed to world space directions
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

        // Write faces (f) per submesh - each face defined by vertex/uv/normal indices (1-based)
        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            int[] triangles = mesh.GetTriangles(i);
            for (int j = 0; j < triangles.Length; j += 3)
            {
                int a = triangles[j] + 1;
                int b = triangles[j + 1] + 1;
                int c = triangles[j + 2] + 1;
                sb.AppendLine($"f {a}/{a}/{a} {b}/{b}/{b} {c}/{c}/{c}");
            }
        }

        return sb.ToString();
    }
}
