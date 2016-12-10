#if DEBUG
#define ENABLE_PROFILER
#define DEVELOPMENT
#endif
using System.Collections.Generic;
using UnityEngine;

namespace ColliderHelper
{
    public class FlightMarkersComponent : MonoBehaviour
    {
        private Vessel _craft;
        private bool _enabled;


        private static Vector3 FindCenterOfMass(Vessel vessel)
        {
            var centerOfMass = Vector3.zero;
            var mass = 0f;

            for (var i = 0; i < vessel.parts.Count; i++)
            {
                var part = vessel.parts[i];

                if (part.physicalSignificance != Part.PhysicalSignificance.FULL) continue;

                centerOfMass += (part.transform.position + part.transform.rotation*part.CoMOffset)*
                                (part.mass + part.GetResourceMass());
                mass += part.mass + part.GetResourceMass();
            }

            return centerOfMass / mass;
        }

        // ReSharper disable once SuggestBaseTypeForParameter
        private static Ray FindCenterOfLift(Vessel vessel, List<ILiftProvider> providers)
        {
            var refVel = vessel.lastVel;
            var refAlt = vessel.altitude;
            var refStp = FlightGlobals.getStaticPressure(refAlt);
            var refTemp = FlightGlobals.getExternalTemperature(refAlt);
            var refDens = FlightGlobals.getAtmDensity(refStp, refTemp);

            var colQuery = new CenterOfLiftQuery();

            var centerOfLift = Vector3.zero;
            var directionOfLift = Vector3.zero;
            var lift = 0f;

            for (var i = 0; i < providers.Count; i++)
            {
                if (!providers[i].IsLifting) continue;

                colQuery.Reset();
                colQuery.refVector = refVel;
                colQuery.refAltitude = refAlt;
                colQuery.refStaticPressure = refStp;
                colQuery.refAirDensity = refDens;

                providers[i].OnCenterOfLiftQuery(colQuery);
                centerOfLift += colQuery.pos * colQuery.lift;
                directionOfLift += colQuery.dir * colQuery.lift;
                lift += colQuery.lift;
            }

            if (lift < float.Epsilon) return new Ray(Vector3.zero, Vector3.zero);

            var m = 1f / lift;
            centerOfLift *= m;
            directionOfLift *= m;

            return new Ray(centerOfLift, directionOfLift);
        }

        // ReSharper disable once SuggestBaseTypeForParameter
        private static Ray FindCenterOfThrust(List<IThrustProvider> providers)
        {
            var cotQuery = new CenterOfThrustQuery();

            var centerOfThrust = Vector3.zero;
            var directionOfThrust = Vector3.zero;
            var thrust = 0f;

            for (var i = 0; i < providers.Count; i++)
            {
                if (!((ModuleEngines)providers[i]).isOperational) continue;

                cotQuery.Reset();
                providers[i].OnCenterOfThrustQuery(cotQuery);
                centerOfThrust += cotQuery.pos * cotQuery.thrust;
                directionOfThrust += cotQuery.dir * cotQuery.thrust;
                thrust += cotQuery.thrust;
            }

            if (thrust < float.Epsilon) return new Ray(Vector3.zero, Vector3.zero);

            var m = 1f / thrust;
            centerOfThrust *= m;
            directionOfThrust *= m;

            return new Ray(centerOfThrust, directionOfThrust);
        }

        private static Ray FindBodyLift(Vessel vessel)
        {
            var bodyLiftPosition = Vector3.zero;
            var bodyLiftDirection = Vector3.zero;
            var lift = 0f;

            for (var i = 0; i < vessel.parts.Count; i++)
            {
                var part = vessel.parts[i];
                bodyLiftPosition += (part.transform.position + part.transform.rotation*part.bodyLiftLocalPosition)*
                                    part.bodyLiftLocalVector.magnitude;
                bodyLiftDirection += (part.transform.localRotation*part.bodyLiftLocalVector)*
                                     part.bodyLiftLocalVector.magnitude;
                lift += part.bodyLiftLocalVector.magnitude;
            }

            if (lift < float.Epsilon) return new Ray(Vector3.zero, Vector3.zero);

            var m = 1f / lift;
            bodyLiftPosition *= m;
            bodyLiftDirection *= m;

            return new Ray(bodyLiftPosition, bodyLiftDirection);
        }


        public void Start()
        {
            _craft = GetComponent<Vessel>();

            if (_craft == null) return;

            _enabled = true;
        }

        public void OnRenderObject()
        {
            if (!_enabled) return;

            if (MapView.MapIsEnabled || (CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA))
            {
                return;
            }

            Profiler.BeginSample("FlightMarkersRender");

            var centerOfMass = FindCenterOfMass(_craft);
            DrawTools.DrawSphere(centerOfMass, XKCDColors.Yellow);

            DrawTools.DrawSphere(_craft.rootPart.transform.position, XKCDColors.Red, 0.25f);

            var thrustProviders = _craft.FindPartModulesImplementing<IThrustProvider>();
            if (thrustProviders.Count > 0)
            {
                var centerOfThrust = FindCenterOfThrust(thrustProviders);
                if (centerOfThrust.direction != Vector3.zero)
                {
                    DrawTools.DrawSphere(centerOfThrust.origin, XKCDColors.Magenta, 0.95f);
                    DrawTools.DrawArrow(centerOfThrust.origin, centerOfThrust.direction*4f, XKCDColors.Magenta);
                }
            }

            if (_craft.rootPart.staticPressureAtm > 0f)
            {
                var liftProviders = _craft.FindPartModulesImplementing<ILiftProvider>();
                if (liftProviders.Count > 0)
                {
                    var centerOfLift = FindCenterOfLift(_craft, liftProviders);
                    if (centerOfLift.direction != Vector3.zero)
                    {
                        DrawTools.DrawSphere(centerOfLift.origin, XKCDColors.Blue, 0.9f);
                        DrawTools.DrawArrow(centerOfLift.origin, centerOfLift.direction*4f, XKCDColors.Blue);
                    }
                }

                var bodyLift = FindBodyLift(_craft);
                // ReSharper disable once RedundantArgumentDefaultValue
                if (!bodyLift.direction.IsSmallerThan(0.1f))
                {
                    DrawTools.DrawSphere(bodyLift.origin, XKCDColors.Cyan, 0.85f);
                    DrawTools.DrawArrow(bodyLift.origin, bodyLift.direction.normalized * 4f, XKCDColors.Cyan);
                }
            }

            Profiler.EndSample();
        }

        public void OnDestroy()
        {
            _enabled = false;
        }
    }
}
