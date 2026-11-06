using Defibrillator.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Defibrillator.Mod;

internal static class DevicePhysics
{
    internal static void ConfigurePrefab(GameObject prefab)
    {
        // Donor transforms and collision geometry must not determine the size of our item.
        prefab.transform.localScale = Vector3.one;
        prefab.transform.localPosition = Vector3.zero;
        prefab.transform.localRotation = Quaternion.identity;
        var physical = prefab.GetComponent<PhysGrabObject>();
        foreach (var marker in prefab.GetComponentsInChildren<PhysGrabObjectCollider>(true)) Object.DestroyImmediate(marker);
        foreach (var marker in prefab.GetComponentsInChildren<PhysGrabObjectBoxCollider>(true)) Object.DestroyImmediate(marker);
        foreach (var collider in prefab.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
        physical.colliders.Clear();

        foreach (var box in DeviceGeometry.CollisionBoxes)
        {
            var child = new GameObject("Defibrillator collision - " + box.Name);
            child.layer = prefab.layer;
            child.transform.SetParent(prefab.transform, false);
            child.transform.localPosition = new Vector3(box.X, box.Y, box.Z) * DeviceGeometry.ModelScale;
            var collider = child.AddComponent<BoxCollider>();
            collider.size = new Vector3(box.Width, box.Height, box.Depth) * DeviceGeometry.ModelScale;
            child.AddComponent<PhysGrabObjectCollider>();
            child.AddComponent<PhysGrabObjectBoxCollider>().drawGizmos = false;
        }

        var body = prefab.GetComponent<Rigidbody>();
        body.mass = DeviceGeometry.MassKg;
        body.useGravity = true;
        body.linearDamping = 0.35f;
        body.angularDamping = 1f;
        body.centerOfMass = new Vector3(0, -0.015f, 0);
        physical.massOriginal = DeviceGeometry.MassKg;
        var nonValuable = prefab.GetComponent<NotValuableObject>();
        if (nonValuable)
        {
            // Native NotValuableObject.Start reapplies this preset. Never edit the donor's shared asset.
            var preset = nonValuable.physAttributePreset ? Object.Instantiate(nonValuable.physAttributePreset) :
                ScriptableObject.CreateInstance<PhysAttribute>();
            preset.name = "Industrial Defibrillator physics";
            preset.mass = DeviceGeometry.MassKg;
            nonValuable.physAttributePreset = preset;
        }
        var center = prefab.transform.Find("Center of Mass");
        if (center) center.position = prefab.transform.TransformPoint(body.centerOfMass);
        var grabPoint = prefab.transform.Find("Force Grab Point");
        if (grabPoint) grabPoint.position = prefab.transform.position;

        var roomCheck = prefab.GetComponent<RoomVolumeCheck>();
        if (roomCheck)
        {
            // Native extraction/truck checks use overlap geometry, not donor trigger colliders.
            roomCheck.CheckPosition = Vector3.zero;
            roomCheck.currentSize = new Vector3(0.35f, 0.24f, 0.13f);
        }
        foreach (var volume in prefab.GetComponentsInChildren<ItemVolume>(true))
            volume.transform.position = prefab.transform.TransformPoint(new Vector3(0, DeviceGeometry.FloorY, 0));
        prefab.GetComponent<ItemAttributes>().costOffset = new Vector3(0, 0.20f, 0);
    }
}

[HarmonyLib.HarmonyPatch(typeof(PhysGrabObject), "Awake")]
internal static class CompactScalePatch
{
    [HarmonyLib.HarmonyPrefix]
    private static void Prefix(PhysGrabObject __instance)
    {
        if (!__instance.GetComponent<Device>()) return;
        // Native Awake restores a saved network scale before calculating collider bounds.
        // Normalize only our item's scale payload so old saves cannot restore the donor size.
        var view = __instance.GetComponent<Photon.Pun.PhotonView>();
        var data = view ? view.InstantiationData : null;
        if (data != null && data.Length >= 3)
        {
            var normalized = (object[])data.Clone();
            normalized[0] = normalized[1] = normalized[2] = 1f;
            var setter = HarmonyLib.AccessTools.PropertySetter(typeof(Photon.Pun.PhotonView), "InstantiationData")
                ?? throw new System.MissingMethodException("PhotonView.InstantiationData setter");
            setter.Invoke(view, new object[] { normalized });
        }
        __instance.transform.localScale = Vector3.one;
    }
}
