using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class Texturas_empaquetar:EditorWindow {
    Texture2D albedo;
    Texture2D rugosidad;
    Texture2D normal;
    Texture2D oclusionAmbiental;
    Texture2D altura;
    Texture2D especular; // Placeholder por si decides usarlo luego

    string carpeta = "Assets/";
    string carpeta_interna = "test/";
    string nombre = "";
    bool rutaAutomatica = true;

    [MenuItem("Tools/Texturas/Empaquetar")]
    public static void MostrarVentana() {
        GetWindow<Texturas_empaquetar>("Texturas Empaquetar");
    }

    void OnGUI() {
        GUILayout.Label("Herramienta de Empaquetado de Canales", EditorStyles.boldLabel);

        // Campos de textura
        albedo = (Texture2D)EditorGUILayout.ObjectField("Albedo (Color)", albedo, typeof(Texture2D), false);
        rugosidad = (Texture2D)EditorGUILayout.ObjectField("Rugosidad (Gris)", rugosidad, typeof(Texture2D), false);
        normal = (Texture2D)EditorGUILayout.ObjectField("Mapa Normal", normal, typeof(Texture2D), false);
        especular = (Texture2D)EditorGUILayout.ObjectField("Especular (Opcional)", especular, typeof(Texture2D), false);
        oclusionAmbiental = (Texture2D)EditorGUILayout.ObjectField("Oclusión (Gris)", oclusionAmbiental, typeof(Texture2D), false);
        altura = (Texture2D)EditorGUILayout.ObjectField("Altura (Gris)", altura, typeof(Texture2D), false);

        GUILayout.Space(10);

        // Gestión de rutas
        EditorGUI.BeginChangeCheck();
        carpeta = EditorGUILayout.TextField("Ruta de guardado", carpeta);
        if (EditorGUI.EndChangeCheck()) {
            rutaAutomatica = false;
        }

        carpeta_interna = EditorGUILayout.TextField("Carpeta interna", carpeta_interna);
        nombre = EditorGUILayout.TextField("Nombre base", nombre);
        rutaAutomatica = EditorGUILayout.Toggle("Ruta automática", rutaAutomatica);

        if (rutaAutomatica && albedo != null) {
            carpeta = ObtenerRutaTextura(albedo);
            nombre = LimpiarNombreTextura(ObtenerNombreTextura(albedo));
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Generar Texturas Empaquetadas", GUILayout.Height(30))) {
            Generar();
        }
    }

    void Generar() {
        if (albedo == null || normal == null || rugosidad == null) {
            Debug.LogError("Error: Albedo, Normal y Rugosidad son obligatorios para el empaquetado.");
            return;
        }

        // 1. Forzar que las texturas sean legibles en memoria como datos puros (Linear)
        Texture2D rAlbedo = ForceReadable(albedo);
        Texture2D rNormal = ForceReadable(normal);
        Texture2D rRugosidad = ForceReadable(rugosidad);
        Texture2D rOcclusion = (oclusionAmbiental != null) ? ForceReadable(oclusionAmbiental) : null;
        Texture2D rAltura = (altura != null) ? ForceReadable(altura) : null;

        int w = rAlbedo.width;
        int h = rAlbedo.height;

        // 2. Obtener arrays de píxeles
        Color[] pixAlbedo = rAlbedo.GetPixels();
        Color[] pixNormal = rNormal.GetPixels();
        Color[] pixRugosidad = rRugosidad.GetPixels();
        Color[] pixOcclusion = (rOcclusion != null) ? rOcclusion.GetPixels() : null;
        Color[] pixAltura = (rAltura != null) ? rAltura.GetPixels() : null;

        Color[] resultado1 = new Color[pixAlbedo.Length];
        Color[] resultado2 = new Color[pixAlbedo.Length];

        for (int i = 0; i < pixAlbedo.Length; i++) {
            // Pack 1: RGB + Rugosidad (Alfa)
            resultado1[i] = new Color(pixAlbedo[i].r, pixAlbedo[i].g, pixAlbedo[i].b, pixRugosidad[i].r);

            // --- CORRECCIÓN PARA EL CANAL R (NORMAL) ---
            // Si la normal viene de un TextureType "Normal Map", el rojo real está en el Alfa.
            // Si viene de una textura "Default", el rojo está en el Rojo.
            // Esta lógica intenta detectar dónde hay datos:
            float nx = (pixNormal[i].a < 1.0f) ? pixNormal[i].a : pixNormal[i].r;
            float ny = pixNormal[i].g;

            float ao = (pixOcclusion != null) ? pixOcclusion[i].r : 1f;
            float ht = (pixAltura != null) ? pixAltura[i].r : 0f;

            resultado2[i] = new Color(nx, ny, ao, ht);
        }

        // 3. Crear y Guardar texturas finales
        Texture2D tex1 = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex1.SetPixels(resultado1);
        tex1.Apply();

        Texture2D tex2 = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex2.SetPixels(resultado2);
        tex2.Apply();

        GuardarTextura(tex1, nombre + "_CR.png");
        GuardarTextura(tex2, nombre + "_NOH.png");

        // Limpiar texturas temporales de la RAM
        DestroyImmediate(rAlbedo);
        DestroyImmediate(rNormal);
        DestroyImmediate(rRugosidad);
        if (rOcclusion) DestroyImmediate(rOcclusion);
        if (rAltura) DestroyImmediate(rAltura);
        DestroyImmediate(tex1);
        DestroyImmediate(tex2);

        AssetDatabase.Refresh();
        Debug.Log("Empaquetado finalizado con éxito.");
    }

    // Procesa la textura para que sea legible independientemente de su configuración de importación
    Texture2D ForceReadable(Texture2D tex) {
        RenderTexture tmp = RenderTexture.GetTemporary(
            tex.width, tex.height, 0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear);

        Graphics.Blit(tex, tmp);
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = tmp;

        Texture2D result = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
        result.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(tmp);
        return result;
    }

    void GuardarTextura(Texture2D tex, string nombreArchivo) {
        string rutaDirectorio = Path.Combine(carpeta, carpeta_interna);
        if (!Directory.Exists(rutaDirectorio)) Directory.CreateDirectory(rutaDirectorio);

        string rutaCompleta = Path.Combine(rutaDirectorio, nombreArchivo);
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(rutaCompleta, bytes);

        AssetDatabase.ImportAsset(rutaCompleta);
        TextureImporter importer = AssetImporter.GetAtPath(rutaCompleta) as TextureImporter;
        if (importer != null) {
            // Forzamos tipo "Default" y sRGB desactivado para que los datos de normal y máscaras sean exactos
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.alphaIsTransparency = false;
            importer.SaveAndReimport();
        }
    }

    string ObtenerNombreTextura(Texture2D textura) {
        return Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(textura));
    }

    string ObtenerRutaTextura(Texture2D textura) {
        string path = AssetDatabase.GetAssetPath(textura);
        return Path.GetDirectoryName(path).Replace("\\", "/") + "/";
    }

    string LimpiarNombreTextura(string nombreOriginal) {
        string[] sufijos = { "maskmap", "ao", "oclusion", "normal", "smoothness", "suavisado", "specular", "metal", "height", "altura", "roughness", "albedo", "diffuse", "basecolor" };
        string resultado = nombreOriginal;
        foreach (string sufijo in sufijos) {
            string patron = @"(?i)_" + sufijo + @"[\d_]*";
            resultado = Regex.Replace(resultado, patron, "");
        }
        return resultado;
    }
}
