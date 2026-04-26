using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class Texturas_desempaquetar:EditorWindow {

    public enum Canal { R, G, B, A }

    [System.Serializable]
    public class CanalConfig {
        public bool usar = false;
        public Canal canal = Canal.R;
    }

    [System.Serializable]
    public class SalidaCanal {
        public string nombre = "output";
        public CanalConfig R = new CanalConfig();
        public CanalConfig G = new CanalConfig();
        public CanalConfig B = new CanalConfig();
        public CanalConfig A = new CanalConfig();
    }

    Texture2D input;
    string carpeta = "Assets/";
    string carpeta_interna = "test/";
    string nombre = "";
    bool rutaAutomatica = true;

    List<SalidaCanal> salidas = new List<SalidaCanal>();

    [MenuItem("Tools/Texturas/Desempaquetar")]
    public static void MostrarVentana() {
        GetWindow<Texturas_desempaquetar>("Texturas desempaquetar");
    }

    void OnGUI() {
        GUILayout.Label("Extractor de texturas", EditorStyles.boldLabel);

        input = (Texture2D)EditorGUILayout.ObjectField("Textura Input", input, typeof(Texture2D), false);

        GUILayout.Space(10);

        // Lógica de ruta automática o manual
        EditorGUI.BeginChangeCheck();
        carpeta = EditorGUILayout.TextField("Carpeta salida", carpeta);
        if (EditorGUI.EndChangeCheck()) {
            rutaAutomatica = false;
        }

        carpeta_interna = EditorGUILayout.TextField("Carpeta interna", carpeta_interna);
        nombre = EditorGUILayout.TextField("Nombre", nombre);
        rutaAutomatica = EditorGUILayout.Toggle("Ruta automática", rutaAutomatica);

        if (rutaAutomatica && input != null) {
            carpeta = ObtenerRutaTextura(input);
            nombre = LimpiarNombreTextura(ObtenerNombreTextura(input));
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Es mascara de terreno")) {
            generar_salidas_mascara_terreno();
        }
        if (GUILayout.Button("Agregar textura de salida")) {
            salidas.Add(new SalidaCanal());
        }

        GUILayout.Space(10);

        for (int i = 0; i < salidas.Count; i++) {
            EditorGUILayout.BeginVertical("box");
            salidas[i].nombre = EditorGUILayout.TextField("Nombre", salidas[i].nombre);
            DrawCanal("R", ref salidas[i].R);
            DrawCanal("G", ref salidas[i].G);
            DrawCanal("B", ref salidas[i].B);
            DrawCanal("A", ref salidas[i].A);
            GUILayout.Space(5);
            if (GUILayout.Button("Eliminar")) {
                salidas.RemoveAt(i);
                break;
            }
            EditorGUILayout.EndVertical();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Extraer canales")) {
            if (salidas.Count > 0)
                Generar();
            else
                Debug.Log("Agrega al menos 1 salida primero");
        }
    }

    void DrawCanal(string label, ref CanalConfig config) {
        EditorGUILayout.BeginHorizontal();
        config.usar = EditorGUILayout.Toggle(config.usar, GUILayout.Width(20));
        EditorGUILayout.LabelField(label, GUILayout.Width(20));
        if (config.usar) {
            config.canal = (Canal)EditorGUILayout.EnumPopup(config.canal);
        } else {
            EditorGUILayout.LabelField("OFF");
        }
        EditorGUILayout.EndHorizontal();
    }

    void Generar() {
        if (input == null) {
            Debug.LogError("Falta textura input.");
            return;
        }

        Texture2D readableInput = ForceReadable(input);
        Color[] pixels = readableInput.GetPixels();
        int w = readableInput.width;
        int h = readableInput.height;

        foreach (var s in salidas) {
            Color[] result = new Color[pixels.Length];

            // Usamos estas variables para detectar si la imagen es sólida
            bool tieneVariacion = false;
            float primerValor = -1f;

            for (int i = 0; i < pixels.Length; i++) {
                float valorExtraido = 0f;

                if (s.R.usar) valorExtraido = GetChannel(pixels[i], s.R.canal);
                else if (s.G.usar) valorExtraido = GetChannel(pixels[i], s.G.canal);
                else if (s.B.usar) valorExtraido = GetChannel(pixels[i], s.B.canal);
                else if (s.A.usar) valorExtraido = GetChannel(pixels[i], s.A.canal);

                // Inicializamos con el primer pixel que encontremos
                if (i == 0) primerValor = valorExtraido;

                // Si cualquier pixel es diferente al primero, hay información real (variación)
                // Usamos un pequeño margen para ignorar ruidos ínfimos
                if (Mathf.Abs(valorExtraido - primerValor) > 0.001f) {
                    tieneVariacion = true;
                }

                result[i] = new Color(valorExtraido, valorExtraido, valorExtraido, 1f);
            }

            // --- VALIDACIÓN MEJORADA ---
            // Solo guardamos si la imagen NO es de un solo color sólido (blanco o negro)
            // EXCEPCIÓN: Si quieres guardar un color sólido a propósito, esta lógica lo saltará.
            if (tieneVariacion) {
                Texture2D tex = Crear(w, h, result);
                Guardar(tex, nombre + "_" + s.nombre + ".png");
                DestroyImmediate(tex);
            } else {
                string motivo = (primerValor > 0.9f) ? "Blanco sólido" : "Negro sólido";
                Debug.LogWarning($"La salida '{s.nombre}' se saltó porque es un color {motivo} (Sin información de máscara).");
            }
        }

        DestroyImmediate(readableInput);
        AssetDatabase.Refresh();
        Debug.Log("Extracción completada.");
    }

    Texture2D ForceReadable(Texture2D tex) {
        RenderTexture tmp = RenderTexture.GetTemporary(
            tex.width, tex.height, 0,
            RenderTextureFormat.ARGB32,
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

    float GetChannel(Color c, Canal ch) {
        return ch switch {
            Canal.R => c.r,
            Canal.G => c.g,
            Canal.B => c.b,
            Canal.A => c.a,
            _ => 0f
        };
    }

    Texture2D Crear(int w, int h, Color[] data) {
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.SetPixels(data);
        t.Apply();
        return t;
    }

    void Guardar(Texture2D tex, string nombre) {
        string directiorio = Path.Combine(carpeta, carpeta_interna);
        if (!Directory.Exists(directiorio)) Directory.CreateDirectory(directiorio);

        byte[] bytes = tex.EncodeToPNG();
        string rutaCompleta = Path.Combine(directiorio, nombre);
        File.WriteAllBytes(rutaCompleta, bytes);

        // Configurar el Importador automáticamente
        AssetDatabase.ImportAsset(rutaCompleta);
        TextureImporter importer = AssetImporter.GetAtPath(rutaCompleta) as TextureImporter;
        if (importer != null) {
            importer.sRGBTexture = false; // Importante para máscaras lineales
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }

    string ObtenerNombreTextura(Texture2D textura) {
        string path = AssetDatabase.GetAssetPath(textura);
        return Path.GetFileNameWithoutExtension(path);
    }

    string ObtenerRutaTextura(Texture2D textura) {
        string path = AssetDatabase.GetAssetPath(textura);
        return Path.GetDirectoryName(path).Replace("\\", "/") + "/";
    }

    void generar_salidas_mascara_terreno() {
        salidas.Clear();
        string[] nombres = { "metal", "oclusion", "altura", "suavisado" };
        Canal[] canales = { Canal.R, Canal.G, Canal.B, Canal.A };

        for (int i = 0; i < 4; i++) {
            SalidaCanal s = new SalidaCanal();
            s.nombre = nombres[i];
            // Configuramos para que cada una extraiga su canal correspondiente
            if (i == 0) s.R.usar = true; s.R.canal = canales[i];
            if (i == 1) s.G.usar = true; s.G.canal = canales[i];
            if (i == 2) s.B.usar = true; s.B.canal = canales[i];
            if (i == 3) s.A.usar = true; s.A.canal = canales[i];
            salidas.Add(s);
        }
    }

    string LimpiarNombreTextura(string nombreOriginal) {
        // 1. Definimos los sufijos comunes (puedes agregar más a esta lista)
        // Agregamos términos en inglés y español
        string[] sufijos = {
        "maskmap", "ao", "oclusion", "normal", "smoothness", "suavisado",
        "specular", "metal", "height", "altura", "roughness", "albedo", "diffuse", "basecolor"
    };

        string resultado = nombreOriginal;

        // 2. Creamos un patrón de búsqueda:
        // El patrón busca un guion bajo (_) seguido de cualquiera de los sufijos,
        // permitiendo números extra o guiones bajos al final (como _02 o 2)
        foreach (string sufijo in sufijos) {
            // Explicación del Regex:
            // _           -> busca el guion bajo inicial
            // (?i){sufijo} -> busca el sufijo ignorando mayúsculas/minúsculas
            // [\d_]* -> busca cualquier número (\d) o guion bajo (_) que siga después
            // $           -> asegura que esté al final de la cadena (opcional, según prefieras)
            string patron = @"(?i)_" + sufijo + @"[\d_]*";

            resultado = Regex.Replace(resultado, patron, "");
        }

        return resultado;
    }
}
