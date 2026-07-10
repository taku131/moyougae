using UnityEngine;
using UnityEngine.Rendering;

public static class MaterialTextureDumper
{
    public static void Dump(GameObject root, string tag)
    {
        if (root == null) { Debug.Log($"[{tag}] root=null"); return; }

        var rends = root.GetComponentsInChildren<Renderer>(true);
        Debug.Log($"[{tag}] renderers={rends.Length}");

        foreach (var r in rends)
        {
            var mats = r.materials;
            for (int mi = 0; mi < mats.Length; mi++)
            {
                var m = mats[mi];
                if (m == null || m.shader == null) continue;

                var sh = m.shader;
                Debug.Log($"[{tag}] Renderer={r.name} Mat[{mi}] shader={sh.name}");

                int pc = sh.GetPropertyCount();
                for (int p = 0; p < pc; p++)
                {
                    if (sh.GetPropertyType(p) != ShaderPropertyType.Texture) continue;
                    string prop = sh.GetPropertyName(p);
                    var tex = m.GetTexture(prop);
                    if (tex != null)
                        Debug.Log($"[{tag}]   TEX prop={prop} -> {tex.name} ({tex.width}x{tex.height})");
                }
            }
        }
    }
}
