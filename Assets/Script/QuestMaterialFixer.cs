using UnityEngine;

public static class QuestMaterialFixer
{
    public static void FixMaterialsForQuest(GameObject root)
    {
        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("[QuestFix] URP/Lit not found");
            return;
        }

        var rends = root.GetComponentsInChildren<Renderer>(true);

        foreach (var r in rends)
        {
            var mats = r.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                var src = mats[i];
                if (src == null) continue;

                // š Standard / glTF / ‰½‚Å—ˆ‚Ä‚àE‚¤
                Texture baseTex =
                       src.GetTexture("_BaseMap")
                    ?? src.GetTexture("_MainTex")
                    ?? src.GetTexture("baseColorTexture");

                var dst = new Material(urpLit);

                // •K‚¸Œ©‚¦‚éÝ’è
                dst.SetColor("_BaseColor", Color.white);
                dst.SetFloat("_Surface", 0f);   // Opaque
                dst.SetFloat("_Cull", 0f);      // —¼–Ê
                dst.SetFloat("_Metallic", 0f);
                dst.SetFloat("_Smoothness", 0.5f);

                if (baseTex != null)
                {
                    dst.SetTexture("_BaseMap", baseTex);
                }
                else
                {
                    Debug.LogWarning($"[QuestFix] No texture found on {src.shader.name}");
                }

                mats[i] = dst;
            }

            r.materials = mats;
        }

        Debug.Log("[QuestFix] Materials fixed for Quest");
    }
}
