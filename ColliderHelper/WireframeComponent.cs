﻿using UnityEngine;
// ReSharper disable ArrangeThisQualifier
// ReSharper disable ForCanBeConvertedToForeach

namespace ColliderHelper
{
    public class WireframeComponent : MonoBehaviour
    {
        public void OnRenderObject()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (MapView.MapIsEnabled || (CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA))
                    return;
            }

            DrawObjects(this.gameObject);
        }

        private void DrawObjects(GameObject go)
        {
            var comp = go.GetComponents<Collider>();

            var engineMod = go.GetComponent<ModuleEngines>();
            if (engineMod != null)
            {
                for (var i = 0; i < engineMod.thrustTransforms.Count; i++)
                {
                    DrawTools.DrawTransform(engineMod.thrustTransforms[i], 0.3f);
                }
            }

            for (var i = 0; i < comp.Length; i++)
            {
                var baseCol = comp[i];

                if (baseCol is BoxCollider)
                {
                    var box = baseCol as BoxCollider;
                    DrawTools.DrawLocalCube(box.transform, box.size, XKCDColors.Yellow, box.center);
                }
                else if (baseCol is SphereCollider)
                {
                    var sphere = baseCol as SphereCollider;
                    DrawTools.DrawSphere(sphere.transform.TransformPoint(sphere.center), XKCDColors.Red, sphere.radius);
                }
                else if (baseCol is CapsuleCollider)
                {
                    var caps = baseCol as CapsuleCollider;

                    var dir = new Vector3(caps.direction == 0 ? 1 : 0, caps.direction == 1 ? 1 : 0,
                        caps.direction == 2 ? 1 : 0);
                    var top = caps.transform.TransformPoint(caps.center + caps.height * 0.5f * dir);
                    var bottom = caps.transform.TransformPoint(caps.center - caps.height * 0.5f * dir);

                    DrawTools.DrawCapsule(top, bottom, XKCDColors.Green, caps.radius * caps.transform.lossyScale.x);
                }
                else if (baseCol is MeshCollider)
                {
                    var mesh = baseCol as MeshCollider;
                    DrawTools.DrawLocalMesh(mesh.transform, mesh.sharedMesh, XKCDColors.ElectricBlue);
                }
                else if (baseCol is WheelCollider)
                {
                    var wheel = baseCol as WheelCollider;

                    Vector3 pos;
                    WheelHit wheelHit;
                    if (wheel.GetGroundHit(out wheelHit))
                    {
                        pos = wheelHit.point + (wheel.transform.up * wheel.radius);
                    }
                    else
                    {
                        pos = wheel.transform.position - (wheel.transform.up * wheel.suspensionDistance);
                    }

                    DrawTools.DrawCircle(pos, wheel.transform.right, XKCDColors.Pink, wheel.radius);
                }
            }

            for (var i = 0; i < go.transform.childCount; i++)
            {
                var child = go.transform.GetChild(i).gameObject;

                if (!child.GetComponent<Part>() && child.name != "main _camera pivot")
                    DrawObjects(child);
            }
        }
    }
}
