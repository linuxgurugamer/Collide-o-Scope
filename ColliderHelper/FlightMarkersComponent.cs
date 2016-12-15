#if DEBUG
#define ENABLE_PROFILER
#define DEVELOPMENT
#endif
using System.Collections.Generic;
using UnityEngine;
// ReSharper disable ForCanBeConvertedToForeach

namespace ColliderHelper
{
    public class FlightMarkersComponent : MonoBehaviour
    {
        private Vessel _craft;
        private bool _enabled;

        private Vector3 _centerOfMass = Vector3.zero;
        private Ray _centerOfThrust = new Ray(Vector3.zero, Vector3.zero);
        private Ray _centerOfLift = new Ray(Vector3.zero, Vector3.zero);
        private Ray _bodyLift = new Ray(Vector3.zero, Vector3.zero);
        private Ray _drag = new Ray(Vector3.zero, Vector3.zero);
        private Ray _combinedLift = new Ray(Vector3.zero, Vector3.zero);

        private static readonly Ray ZeroRay = new Ray(Vector3.zero, Vector3.zero);

        private const float CenterOfLiftCutoff = 0.2f;
        private const float BodyLiftCutoff = 0.2f;
        private const float DragCutoff = 0.3f;

        private bool _combineLift = true;


        public bool ToggleCombinedLift()
        {
            _combineLift = !_combineLift;

            return _combineLift;
        }

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

        private static Ray FindDrag(Vessel vessel)
        {
            var dragPosition = Vector3.zero;
            var dragDirection = Vector3.zero;
            var drag = 0f;

            for (var i = 0; i < vessel.parts.Count; i++)
            {
                var part = vessel.parts[i];
                var liftModule = part.Modules.GetModule<ModuleLiftingSurface>();

                if (liftModule)
                {
                    if (liftModule.useInternalDragModel)
                    {
                        dragPosition += (part.transform.position + part.transform.rotation * part.CoPOffset) * liftModule.dragScalar;
                        dragDirection += (part.transform.localRotation * liftModule.dragForce) * liftModule.dragScalar;
                        drag += liftModule.dragScalar;

                        continue;
                    }
                }

                dragPosition += (part.transform.position + part.transform.rotation * part.CoPOffset) * part.dragScalar;
                dragDirection += (part.transform.localRotation * part.dragVectorDirLocal) * part.dragScalar;
                drag += part.dragScalar;
            }

            if (drag < float.Epsilon) return ZeroRay;

            var m = 1f / drag;
            dragPosition *= m;
            dragDirection *= m;

            return new Ray(dragPosition, dragDirection);
        }


        public void Start()
        {
            _craft = GetComponent<Vessel>();

            if (_craft == null) return;

            _enabled = true;
        }

        public void FixedUpdate()
        {
            if (!_enabled) return;

            if (MapView.MapIsEnabled || (CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA))
            {
                return;
            }

            Profiler.BeginSample("FlightMarkersRenderMath");

            _centerOfMass = FindCenterOfMass(_craft);

            var thrustProviders = _craft.FindPartModulesImplementing<IThrustProvider>();
            _centerOfThrust = thrustProviders.Count > 0 ? FindCenterOfThrust(thrustProviders) : ZeroRay;

            if (_craft.rootPart.staticPressureAtm > 0.0f)
            {
                var liftProviders = _craft.FindPartModulesImplementing<ILiftProvider>();
                _centerOfLift = liftProviders.Count > 0 ? FindCenterOfLift(_craft, liftProviders) : ZeroRay;

                _bodyLift = FindBodyLift(_craft);

                _drag = FindDrag(_craft);
            }
            else
            {
                _centerOfLift = ZeroRay;
                _bodyLift = ZeroRay;
                _drag = ZeroRay;
            }

            Profiler.EndSample();
        }

        public void OnRenderObject()
        {
            if (!_enabled) return;

            if (MapView.MapIsEnabled || (CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA))
            {
                return;
            }

            Profiler.BeginSample("FlightMarkersRenderDraw");

            DrawTools.DrawSphere(_centerOfMass, XKCDColors.Yellow);

            DrawTools.DrawSphere(_craft.rootPart.transform.position, XKCDColors.Green, 0.25f);

            if (_centerOfThrust.direction != Vector3.zero)
            {
                DrawTools.DrawSphere(_centerOfThrust.origin, XKCDColors.Magenta, 0.95f);
                DrawTools.DrawArrow(_centerOfThrust.origin, _centerOfThrust.direction*4.0f, XKCDColors.Magenta);
            }

            if (_combineLift)
            {
                _combinedLift.origin = Vector3.zero;
                _combinedLift.direction = Vector3.zero;
                var count = 0;

                if (!_centerOfLift.direction.IsSmallerThan(CenterOfLiftCutoff))
                {
                    _combinedLift.origin += _centerOfLift.origin;
                    _combinedLift.direction += _centerOfLift.direction;
                    count++;
                }

                if (!_bodyLift.direction.IsSmallerThan(BodyLiftCutoff))
                {
                    _combinedLift.origin += _bodyLift.origin;
                    _combinedLift.direction += _bodyLift.direction;
                    count++;
                }

                _combinedLift.origin /= count;
                _combinedLift.direction /= count;

                DrawTools.DrawSphere(_combinedLift.origin, XKCDColors.Purple, 0.9f);
                DrawTools.DrawArrow(_combinedLift.origin, _combinedLift.direction*4.0f, XKCDColors.Purple);
            }
            else
            {
                if (!_centerOfLift.direction.IsSmallerThan(CenterOfLiftCutoff))
                {
                    DrawTools.DrawSphere(_centerOfLift.origin, XKCDColors.Blue, 0.9f);
                    DrawTools.DrawArrow(_centerOfLift.origin, _centerOfLift.direction*4.0f, XKCDColors.Blue);
                }

                if (!_bodyLift.direction.IsSmallerThan(BodyLiftCutoff))
                {
                    DrawTools.DrawSphere(_bodyLift.origin, XKCDColors.Cyan, 0.85f);
                    DrawTools.DrawArrow(_bodyLift.origin, _bodyLift.direction*4.0f, XKCDColors.Cyan);
                }
            }

            if (!_drag.direction.IsSmallerThan(DragCutoff))
            {
                DrawTools.DrawSphere(_drag.origin, XKCDColors.Red, 0.8f);
                DrawTools.DrawArrow(_drag.origin, _drag.direction*4.0f, XKCDColors.Red);
            }

            Profiler.EndSample();
        }

        public void OnDestroy()
        {
            _enabled = false;
        }
    }
}
