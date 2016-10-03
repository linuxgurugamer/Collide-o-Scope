using System;
using UnityEngine;

namespace ColliderHelper
{
    public class WireframeComponent : MonoBehaviour
    {
        private string _thrustVectorTransformName = "";

        public void OnRenderObject()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (MapView.MapIsEnabled || (CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA))
                    return;
            }

            //var camera = DrawTools.GetActiveCam();

            //if (camera.tag != "MainCamera")
            //    return;

            //if (!GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(camera),
            //        this.gameObject.GetRendererBounds()))
            //    return;

            //if (this.gameObject.GetRendererBounds().Contains(camera.transform.position))
            //    return;

            //var distance = this.gameObject.GetRendererBounds().SqrDistance(camera.transform.position);
            //Debug.Log("[CH] Distance: " + distance);

            //var Mat = this.gameObject.GetComponent<MeshRenderer>();
            //if (Mat != null)
            //{
            //    Mat.material.color.A(0.5f);
            //}

            DrawObjects(this.gameObject);
        }

        private void DrawObjects(GameObject go)
        {
            var comp = go.GetComponents<Collider>();

            var engineMod = go.GetComponent<ModuleEngines>();
            if (engineMod != null)
                _thrustVectorTransformName = engineMod.thrustVectorTransformName;

            if (go.name == _thrustVectorTransformName)
                DrawTools.DrawTransform(go.transform, 0.3f);

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

                    DrawTools.DrawCapsule(top, bottom, XKCDColors.Green, caps.radius);
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
                        //RaycastHit hit;
                        //Physics.Raycast(wheel.transform.position, -wheel.transform.up, out hit);
                        //var hitlag = (wheelHit.point - hit.point);
                        //Debug.Log("Lagging " + hitlag.magnitude + " Deltatime " + Time.deltaTime);

                        //wheelHit.point = wheelHit.point + FlightGlobals.ActiveVessel.srf_velocity * Time.deltaTime;

                        //hitlag = wheelHit.point - hit.point;
                        //Debug.Log("Lagging " + hitlag.magnitude + " velocity = " +
                        //          FlightGlobals.ActiveVessel.srf_velocity * Time.deltaTime);

                        pos = wheelHit.point + (wheel.transform.up * wheel.radius);
                    }
                    else
                    {
                        pos = wheel.transform.position - (wheel.transform.up * wheel.suspensionDistance);
                    }

                    DrawTools.DrawCircle(pos, wheel.transform.right, XKCDColors.Pink, wheel.radius);
                }
            }

            //MeshFilter[] meshes = go.GetComponents<MeshFilter>();
            //for (var i = 0; i < meshes.Length; i++)
            //{
            //    DrawTools.DrawLocalMesh(meshes[i].transform, meshes[i].sharedMesh, XKCDColors.Orange);
            //}

            for (var i = 0; i < go.transform.childCount; i++)
            {
                var child = go.transform.GetChild(i).gameObject;

                if (!child.GetComponent<Part>() && child.name != "main _camera pivot")
                    DrawObjects(child);
            }
        }

        //private GameObject FindParentWithComponent(Type component)
        //{
        //    var parent = this.gameObject;
        //    while (parent != null)
        //    {
        //        if (parent.GetComponentUpwards<ModuleWheelBase>())
        //        {
                    
        //        }
        //    }
        //}
    }
}
