using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ColliderHelper
{
    public class ModuleFlightMarkers : PartModule
    {
        private Vessel _craft;
        private bool _enabled = false;

        // MechJeb implimentation
        private static Vector3 CalcCenterOfMass(Vessel vessel)
        {
            var com = Vector3.zero;
            var mass = 0f;

            for (var i = 0; i < vessel.parts.Count; i++)
            {
                var p = vessel.parts[i];
                if (p.rb != null)
                {
                    mass += p.rb.mass;
                    com = com + (p.rb.worldCenterOfMass*p.rb.mass);
                }
            }
            return com / mass;
        }

        private static Vector3 FindCenterOfMass(Vessel v)
        {
            var com = Vector3.zero;
            var mass = 0f;

            for (var i = 0; i < v.parts.Count; i++)
            {
                var p = v.parts[i];

                if (p.physicalSignificance == Part.PhysicalSignificance.FULL)
                {
                    
                }
            }
        }

        // KSP implimentation
        private static Vector3 FindCenterOfMass(Part root)
        {
            var vector = Vector3.zero;
            var d = 0f;
            RecurseCenterOfMass(root, ref vector, ref d);
            vector = vector / d;
            return vector;
        }

        private static void RecurseCenterOfMass(Part part, ref Vector3 com, ref float m)
        {
            if (part.physicalSignificance == Part.PhysicalSignificance.FULL)
            {
                com += (part.transform.position + part.transform.rotation*part.CoMOffset)*
                       (part.mass + part.GetResourceMass());
            }
            else if (part.parent != null)
            {
                com += (part.parent.transform.position + part.parent.transform.rotation*part.parent.CoMOffset)*
                       (part.mass + part.GetResourceMass());
            }
            else if (part.potentialParent != null)
            {
                com += (part.potentialParent.transform.position +
                        part.potentialParent.transform.rotation*part.potentialParent.CoMOffset)*
                       (part.mass + part.GetResourceMass());
            }
            m += part.mass + part.GetResourceMass();

            for (var i = 0; i < part.children.Count; i++)
            {
                var p = part.children[i];
                RecurseCenterOfMass(p, ref com, ref m);
            }
        }

        // KSP implimentation (CoL)
        private Ray FindCenterOfLift(Vessel vessel)
        {
            var refVel = vessel.lastVel;
            var refAlt = vessel.altitude;
            var refStp = FlightGlobals.getStaticPressure(refAlt);
            var refTemp = FlightGlobals.getExternalTemperature(refAlt);
            var refDens = FlightGlobals.getAtmDensity(refStp, refTemp);

            return FindCenterOfLift(vessel.rootPart, refVel, refAlt, refStp, refDens);
        }

        private Ray FindCenterOfLift(Part root, Vector3 refvel, double refAlt, double refStp, double refDens)
        {
            var vector = Vector3.zero;
            var vector2 = Vector3.zero;
            var num = 0f;
            this.RecurseCenterOfLift(root, refvel, ref vector, ref vector2, ref num, refAlt, refStp, refDens);

            if (num != 0f)
            {
                float d = 1f / num;
                vector *= d;
                vector2 *= d;
                return new Ray(vector, vector2);
            }
            return new Ray(Vector3.zero, Vector3.zero);
        }

        private void RecurseCenterOfLift(Part part, Vector3 refVel, ref Vector3 CoL, ref Vector3 DoL, ref float t,
            double refAlt, double refStp, double refDens)
        {
            var mods = part.Modules.GetModules<ILiftProvider>();
            var colQuery = new CenterOfLiftQuery();

            for (var i = 0; i < mods.Count; i++)
            {
                colQuery.Reset();
                colQuery.refVector = refVel;
                colQuery.refAltitude = refAlt;
                colQuery.refStaticPressure = refStp;
                colQuery.refAirDensity = refDens;
                mods[i].OnCenterOfLiftQuery(colQuery);
                CoL += colQuery.pos * colQuery.lift;
                DoL += colQuery.dir * colQuery.lift;
                t += colQuery.lift;
            }

            for (var i = 0; i < part.children.Count; i++)
            {
                var p = part.children[i];
                this.RecurseCenterOfLift(p, refVel, ref CoL, ref DoL, ref t, refAlt, refStp, refDens);
            }
        }

        private GameObject _markerObject = new GameObject("Flight Markers");

        public void Start()
        {
            _craft = this.GetComponent<Vessel>();
            if (_craft != null)
            {
                _markerObject.transform.parent = _craft.rootPart.transform;
                ScreenMessages.PostScreenMessage("Flight markers _enabled.");
                _enabled = true;
            }
        }

        public void Update()
        {
            if (!this._enabled) return;


            _markerObject.transform.position = _craft.transform.position;

            //var com = _craft.ReferenceTransform.rotation * FindCenterOfMass(_craft.rootPart);
            //var colRay = FindCenterOfLift(_craft);
            //var col = _craft.ReferenceTransform.rotation * colRay.origin;
            //Debug.Log(string.Format("CoM: {0}, CoL.v: {1}, CoL.d: {2}", com, col, colRay.direction));
            //DrawTools.DrawSphere(com, XKCDColors.Red, 100f);
            //DrawTools.DrawSphere(col, XKCDColors.Blue, 100f);
        }

        public void OnRenderObject()
        {
            if (!this._enabled) return;

            if (HighLogic.LoadedSceneIsFlight)
            {
                if (MapView.MapIsEnabled || (CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA))
                {
                    return;
                }
            }

            DrawTools.DrawSphere(_markerObject.transform.TransformPoint(_markerObject.transform.position),
                XKCDColors.Yellow, 1f);
        }

        public void OnDestroy()
        {
            Destroy(_markerObject);
        }
    }
}
