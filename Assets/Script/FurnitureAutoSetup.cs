using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FurnitureAutoSetup
{
    // 生成された家具(ルートGameObject)に「掴める」機能を自動付与
    public static void MakeGrabbable(GameObject root)
    {
        if (root == null) return;

        // 1) Colliderが無ければ追加（MeshColliderは重いのでまずBox推奨）
        var anyCol = root.GetComponentInChildren<Collider>();
        if (anyCol == null)
        {
            var box = root.AddComponent<BoxCollider>();
            FitBoxToRenderers(root, box);
        }

        // 2) Rigidbody（スクショ同様：isKinematic true）
        var rb = root.GetComponent<Rigidbody>();
        if (rb == null) rb = root.AddComponent<Rigidbody>();
        rb.useGravity = false;      // まずはOFF（床スナップ入れるなら後でONでも可）
        rb.isKinematic = true;      // 掴んでいる間だけ動かす運用に合う
        rb.interpolation = RigidbodyInterpolation.None;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

        // 3) Grabbable（Meta XR Interaction SDK）
        // 既に付いてたら二重追加しない
        var grabbable = root.GetComponent<Oculus.Interaction.Grabbable>();
        if (grabbable == null) grabbable = root.AddComponent<Oculus.Interaction.Grabbable>();

        // GrabbableのRigidbody参照を入れる（Inspectorの「リジッドボディ」相当）
        grabbable.InjectOptionalRigidbody(rb);

        // 4) GrabInteractable（手/コントローラ両対応にしたいなら）
        var grabInteractable = root.GetComponent<Oculus.Interaction.GrabInteractable>();
        if (grabInteractable == null) grabInteractable = root.AddComponent<Oculus.Interaction.GrabInteractable>();

        grabInteractable.InjectOptionalPointableElement(grabbable);
        grabInteractable.InjectRigidbody(rb);

        // 5) HandGrabInteractable（ハンド用）
        var handGrab = root.GetComponent<Oculus.Interaction.HandGrab.HandGrabInteractable>();
        if (handGrab == null) handGrab = root.AddComponent<Oculus.Interaction.HandGrab.HandGrabInteractable>();

        handGrab.InjectOptionalPointableElement(grabbable);
        handGrab.InjectRigidbody(rb);

        // ここまでで、スクショの「Cubeに付いてる主要コンポーネント」に近い状態になります

        var snap = root.GetComponent<SnapToSurface>();
        
        if (snap == null)
            snap = root.AddComponent<SnapToSurface>();

        snap.floorMask = LayerMask.GetMask("Floor");
        snap.wallMask = LayerMask.GetMask("Wall");


        // 追加：状態管理
        var state = root.GetComponent<FurnitureState>();
        if (state == null) state = root.AddComponent<FurnitureState>();

        state.rb = rb;
        state.grab = grabInteractable;
        state.handGrab = handGrab;


    }

    // BoxColliderを見た目のサイズに合わせる（Renderer boundsから算出）
    private static void FitBoxToRenderers(GameObject root, BoxCollider box)
    {
        var rends = root.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return;

        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);

        // boundsはワールド座標なので、ローカルに変換してColliderに入れる
        var t = root.transform;
        Vector3 centerLocal = t.InverseTransformPoint(b.center);

        // サイズはスケールを考慮
        Vector3 sizeWorld = b.size;
        Vector3 lossy = t.lossyScale;
        Vector3 sizeLocal = new Vector3(
            lossy.x != 0 ? sizeWorld.x / lossy.x : sizeWorld.x,
            lossy.y != 0 ? sizeWorld.y / lossy.y : sizeWorld.y,
            lossy.z != 0 ? sizeWorld.z / lossy.z : sizeWorld.z
        );

        box.center = centerLocal;
        box.size = sizeLocal;
    }
}
