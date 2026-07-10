using UnityEngine;

public static class GltfUrpMaterialFixer
{
    public static void ReplaceToUrpLitKeepingTextures(GameObject root)
    {
        if (root == null) return;

        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("[URP] Shader not found: Universal Render Pipeline/Lit");
            return;
        }

        var rends = root.GetComponentsInChildren<Renderer>(true);
        int replaced = 0;

        foreach (var r in rends)
        {
            var mats = r.materials;

            for (int i = 0; i < mats.Length; i++)
            {
                var src = mats[i];
                if (src == null || src.shader == null) continue;

                // 元Materialから「テクスチャ」を汎用的に拾う（プロパティ名に依存しない）
                Texture baseTex = FindAnyTexture(src, preferBaseColor: true);

                // デバッグ：元にテクスチャが入ってるか確認したい場合は一時的にON
                // Debug.Log($"[URP] srcShader={src.shader.name} tex={(baseTex ? baseTex.name : "null")}");

                var dst = new Material(urpLit);

                // 1) ベース色を必ず白に（これで「黒くて見えない」を潰す）
                if (dst.HasProperty("_BaseColor"))
                    dst.SetColor("_BaseColor", Color.white);

                // 2) BaseMap をセット
                if (baseTex != null && dst.HasProperty("_BaseMap"))
                    dst.SetTexture("_BaseMap", baseTex);

                // 3) 透明系を強制OFF（透けて“何もない”を潰す）
                if (dst.HasProperty("_Surface"))
                    dst.SetFloat("_Surface", 0f); // 0=Opaque, 1=Transparent

                // 4) カリングを両面に（裏面で見えないを潰す）
                if (dst.HasProperty("_Cull"))
                    dst.SetFloat("_Cull", 0f); // 0=Off (両面)

                // 5) Metal/Smoothness を控えめに（真っ黒化/ギラつき回避）
                if (dst.HasProperty("_Metallic"))
                    dst.SetFloat("_Metallic", 0f);
                if (dst.HasProperty("_Smoothness"))
                    dst.SetFloat("_Smoothness", 0.5f);

                // 6) キーワード更新（重要：URPはこれがないと反映されないことがある）
                dst.EnableKeyword("_SURFACE_TYPE_OPAQUE");
                dst.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                dst.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;


                // 色（任意：見た目の白さが変になるのを抑える）
                if (dst.HasProperty("_BaseColor"))
                {
                    // src側の色プロパティ名は色々なので、取れれば取る
                    if (src.HasProperty("_BaseColor")) dst.SetColor("_BaseColor", src.GetColor("_BaseColor"));
                    else if (src.HasProperty("_Color")) dst.SetColor("_BaseColor", src.GetColor("_Color"));
                    else dst.SetColor("_BaseColor", Color.white);
                }

                // テクスチャをURP/LitのBaseMapへ
                if (baseTex != null)
                {
                    dst.SetTexture("_BaseMap", baseTex);

                    // タイリング/オフセットも移す（対応してる範囲で）
                    if (src.HasProperty("_MainTex"))
                    {
                        dst.SetTextureScale("_BaseMap", src.GetTextureScale("_MainTex"));
                        dst.SetTextureOffset("_BaseMap", src.GetTextureOffset("_MainTex"));
                    }
                }

                mats[i] = dst;
                replaced++;
            }

            r.materials = mats;
        }

        Debug.Log($"[URP] Replaced materials: {replaced}, renderers={rends.Length}");
    }

    private static Texture FindAnyTexture(Material mat, bool preferBaseColor)
    {
        // まず「ありがちな名前」を優先（glTFast系も混ざる）
        string[] candidates = preferBaseColor
            ? new[]
            {
                "_BaseMap", "_MainTex",
                "_BaseColorTexture", "_BaseColorTex", "_BaseColorMap",
                "_BaseColor", // shader graphによってはTextureと同名があることも
                "_ColorTexture", "_ColorTex",
                "_Albedo", "_AlbedoMap"
            }
            : new[] { "_BaseMap", "_MainTex" };

        foreach (var n in candidates)
        {
            if (mat.HasProperty(n))
            {
                var t = mat.GetTexture(n);
                if (t != null) return t;
            }
        }

        // 次に「元Shaderの全Textureプロパティ」を総当たりで探す（これが本命）
        var sh = mat.shader;
        int count = sh.GetPropertyCount();

        Texture firstAny = null;
        Texture firstLooksLikeBase = null;

        for (int p = 0; p < count; p++)
        {
            if (sh.GetPropertyType(p) != UnityEngine.Rendering.ShaderPropertyType.Texture)
                continue;

            string prop = sh.GetPropertyName(p);
            var t = mat.GetTexture(prop);
            if (t == null) continue;

            // base/color/albedoっぽいのを優先
            string low = prop.ToLowerInvariant();
            bool looksBase = low.Contains("base") || low.Contains("albedo") || low.Contains("color");

            if (firstAny == null) firstAny = t;
            if (looksBase && firstLooksLikeBase == null) firstLooksLikeBase = t;
        }

        return firstLooksLikeBase ?? firstAny;
    }
}
