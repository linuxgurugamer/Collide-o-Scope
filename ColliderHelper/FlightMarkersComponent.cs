using UnityEngine;

namespace ColliderHelper
{
    public class FlightMarkersComponent : MonoBehaviour
    {
        private Vessel _craft;
        private bool _enabled = false;


        private static Vector3 FindCenterOfMass(Vessel vessel)
        {
            var com = Vector3.zero;
            var mass = 0f;

            for (var i = 0; i < vessel.parts.Count; i++)
            {
                var part = vessel.parts[i];

                if (part.physicalSignificance != Part.PhysicalSignificance.FULL) continue;

                com += (part.transform.position + part.transform.rotation * part.CoMOffset) * (part.mass + part.GetResourceMass());
                mass += part.mass + part.GetResourceMass();
            }

            return com / mass;
        }

        private static Ray FindCenterOfLift(Vessel vessel)
        {
            var refVel = vessel.lastVel;
            var refAlt = vessel.altitude;
            var refStp = FlightGlobals.getStaticPressure(refAlt);
            var refTemp = FlightGlobals.getExternalTemperature(refAlt);
            var refDens = FlightGlobals.getAtmDensity(refStp, refTemp);

            var colQuery = new CenterOfLiftQuery();

            var col = Vector3.zero;
            var dir = Vector3.zero;
            var lift = 0f;

            for (var i = 0; i < vessel.parts.Count; i++)
            {
                var part = vessel.parts[i];
                var modules = part.Modules.GetModules<ILiftProvider>();

                colQuery.Reset();
                colQuery.refVector = refVel;
                colQuery.refAltitude = refAlt;
                colQuery.refStaticPressure = refStp;
                colQuery.refAirDensity = refDens;

                for (var j = 0; j < modules.Count; j++)
                {
                    modules[j].OnCenterOfLiftQuery(colQuery);
                    col += colQuery.pos * colQuery.lift;
                    dir += colQuery.dir * colQuery.lift;
                    lift += colQuery.lift;
                }
            }

            if (lift < float.Epsilon) return new Ray(Vector3.zero, Vector3.zero);

            var m = 1f / lift;
            col *= m;
            dir *= m;

            return new Ray(col, dir);
        }

        private static Ray FindCenterOfThrust(Vessel vessel)
        {
            var cotQuery = new CenterOfThrustQuery();

            var cot = Vector3.zero;
            var dir = Vector3.zero;
            var thrust = 0f;

            for (var i = 0; i < vessel.parts.Count; i++)
            {
                var part = vessel.parts[i];
                var modules = part.Modules.GetModules<IThrustProvider>();

                cotQuery.Reset();

                for (var j = 0; j < modules.Count; j++)
                {
                    modules[j].OnCenterOfThrustQuery(cotQuery);
                    cot += cotQuery.pos * cotQuery.thrust;
                    dir = cotQuery.dir * cotQuery.thrust;
                    thrust += cotQuery.thrust;
                }
            }

            if (thrust < float.Epsilon) return new Ray(Vector3.zero, Vector3.zero);

            var m = 1f / thrust;
            cot *= m;
            dir *= m;

            return new Ray(cot, dir);
        }


        public void Start()
        {
            _craft = this.GetComponent<Vessel>();

            if (_craft == null) return;

            _enabled = true;
        }

        public void OnRenderObject()
        {
            if (!this._enabled) return;

            if (MapView.MapIsEnabled || (CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA))
            {
                return;
            }

            var centerOfMass = FindCenterOfMass(_craft);
            DrawTools.DrawSphere(centerOfMass, new Color(1.0f, 0.818f, 0.0f, 0.498f));

            var centerOfLift = FindCenterOfLift(_craft);
            DrawTools.DrawSphere(centerOfLift.origin, new Color(0.0f, 0.916f, 1.0f, 0.498f), 0.9f);
            DrawTools.DrawArrow(centerOfLift.origin, centerOfLift.direction*4f, new Color(0.0f, 0.916f, 1.0f, 0.498f));

            var centerOfThrust = FindCenterOfThrust(_craft);
            DrawTools.DrawSphere(centerOfThrust.origin, new Color(1.0f, 0.0f, 0.986f, 0.498f), 0.95f);
            DrawTools.DrawArrow(centerOfThrust.origin, centerOfThrust.direction*4f, new Color(1.0f, 0.0f, 0.986f, 0.498f));
        }
    }
}
