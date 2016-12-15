using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
// ReSharper disable ForCanBeConvertedToForeach

namespace ColliderHelper
{
    public enum RendererState
    {
        Active,
        Symmetry,
        Off
    }

    public class ModuleColliderHelper : PartModule
    {
        private RendererState _state = RendererState.Off;

        [SerializeField]
        private List<ThrustArrowComponent> _thrustArrows;

        private FlightMarkersComponent _flightMarkerComponent;
        private bool _flightMarkersEnabled;
        private bool _flightMarkersCombinedLift = true;

        [KSPEvent(guiActive = true, advancedTweakable = true, guiActiveUnfocused = true, guiActiveUncommand = true,
            externalToEVAOnly = false, guiActiveEditor = false, unfocusedRange = 100f, guiName = "Flight Markers: Off",
            active = true, isPersistent = false)]
        public void ToggleFlightMarkers()
        {
            var modules = vessel.FindPartModulesImplementing<ModuleColliderHelper>();

            if (_flightMarkersEnabled)
            {
                for (var i = 0; i < modules.Count; i++)
                {
                    modules[i].DisableFlightMarkers();
                }
            }
            else
            {
                _flightMarkerComponent = vessel.gameObject.AddComponent<FlightMarkersComponent>();

                for (var i = 0; i < modules.Count; i++)
                {
                    modules[i].EnableFlightMarkers();
                }
            }
        }

        [KSPEvent(guiActive = true, advancedTweakable = true, guiActiveUnfocused = true, guiActiveUncommand = true,
            externalToEVAOnly = false, guiActiveEditor = false, unfocusedRange = 100f, guiName = "Combine Lift: On",
            active = false, isPersistent = false)]
        public void ToggleCombinedLift()
        {
            var modules = vessel.FindPartModulesImplementing<ModuleColliderHelper>();
            for (var i = 0; i < modules.Count; i++)
            {
                modules[i].CycleCombinedLift();
            }
        }

        [KSPEvent(guiActive = true, guiActiveUnfocused = true, guiActiveUncommand = true, externalToEVAOnly = false,
            guiActiveEditor = true, unfocusedRange = 100f, guiName = "Show Collider: Off", active = true,
            advancedTweakable = true, isPersistent = false)]
        public void ColliderHelperEvent()
        {
            CycleState();
        }

#if DEBUG
        [KSPEvent(guiActive = true, guiActiveUnfocused = true, guiActiveUncommand = true, externalToEVAOnly = false,
            guiActiveEditor = true, unfocusedRange = 100f, guiName = "Do Things", active = true,
            advancedTweakable = true, isPersistent = false)]
        public void DoThings()
        {
            //ColliderHelper.DumpGameObjectChilds(this.gameObject, "");

            var controlSurfaces = part.vessel.FindPartModulesImplementing<ModuleControlSurface>();
            Debug.Log("Surfaces: " + controlSurfaces.Count);

            for (var i = 0; i < controlSurfaces.Count; i++)
            {
                var controlTransform = controlSurfaces[i].part.FindModelTransform(controlSurfaces[i].transformName);
                if(controlTransform)
                    Debug.Log("Ctrl: " + controlTransform.localRotation + ", Part: " + controlSurfaces[i].transform.localRotation);
            }
        }
#endif

        public void EnableFlightMarkers()
        {
            _flightMarkersEnabled = true;

            Events["ToggleCombinedLift"].active = true;
            Events["ToggleFlightMarkers"].guiName = "Flight Markers: On";
        }

        public void DisableFlightMarkers()
        {
            if (_flightMarkerComponent)
            {
                Destroy(_flightMarkerComponent);
                _flightMarkerComponent = null;
            }

            _flightMarkersEnabled = false;
            _flightMarkersCombinedLift = true;

            Events["ToggleCombinedLift"].active = false;
            Events["ToggleCombinedLift"].guiName = "Combine Lift: On";
            Events["ToggleFlightMarkers"].guiName = "Flight Markers: Off";
        }

        public void CycleCombinedLift()
        {
            if (_flightMarkerComponent)
            {
                _flightMarkerComponent.ToggleCombinedLift();
            }

            _flightMarkersCombinedLift = !_flightMarkersCombinedLift;

            Events["ToggleCombinedLift"].guiName = _flightMarkersCombinedLift ? "Combine Lift: On" : "Combine Lift: Off";
        }

        public void CycleState()
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch(_state)
            {
                case RendererState.Active:
                    // on->symmetry|off
                    if (part.symmetryCounterparts.Count > 0)
                    {
                        SetSymmetry(true);
                    }
                    else
                    {
                        SetOff(false);
                    }
                    break;
                case RendererState.Symmetry:
                    // symmetry->off
                    SetOff(true);
                    break;
                case RendererState.Off:
                    // off->on
                    if (part.symmetryCounterparts.Count > 0)
                    {
                        var onCount = part.symmetryCounterparts.Count(t => t.GetComponent<ModuleColliderHelper>()._state == RendererState.Active);

                        if (onCount == part.symmetryCounterparts.Count)
                            SetSymmetry(true);
                        else
                            SetOn(false);
                    }
                    else
                    {
                        SetOn(false);
                    }
                    break;
            }
        }

        public void SetOn(bool symmetry)
        {
            if (gameObject.GetComponent<WireframeComponent>() == null)
                gameObject.AddComponent<WireframeComponent>();

            if (_thrustArrows == null)
            {
                ModuleEngines engineMod;
                if (FindEngineModule(gameObject, out engineMod))
                {
                    _thrustArrows = new List<ThrustArrowComponent>();

                    for (var i = 0; i < engineMod.thrustTransforms.Count; i++)
                    {
                        var go = new GameObject("Thrust Transform Arrow Renderer");
                        go.transform.parent = engineMod.thrustTransforms[i].transform;
                        go.transform.localPosition = Vector3.zero;
                        _thrustArrows.Add(go.AddComponent<ThrustArrowComponent>());
                    }
                }
            }

            if (symmetry) return;

            _state = RendererState.Active;

            Events["ColliderHelperEvent"].guiName = "Show Collider: On";
        }

        public void SetSymmetry(bool recursive)
        {
            if (recursive)
            {
                for (var i = 0; i < part.symmetryCounterparts.Count; i++)
                {
                    var component = part.symmetryCounterparts[i].GetComponent<ModuleColliderHelper>();

                    component?.SetSymmetry(false);
                }
            }

            SetOn(true);

            _state = RendererState.Symmetry;

            Events["ColliderHelperEvent"].guiName = "Show Collider: Symmetry";
        }

        public void SetOff(bool recursive)
        {
            if (recursive)
            {
                for (var i = 0; i < part.symmetryCounterparts.Count; i++)
                {
                    var helperComponent = part.symmetryCounterparts[i].GetComponent<ModuleColliderHelper>();

                    helperComponent?.SetOff(false);
                }
            }

            var renderComponent = gameObject.GetComponent<WireframeComponent>();
            if (renderComponent != null)
                Destroy(renderComponent);

            if (_thrustArrows != null)
            {
                for (var i = 0; i < _thrustArrows.Count; i++)
                {
                    DestroyImmediate(_thrustArrows[i].gameObject);
                }
                _thrustArrows = null;
            }

            _state = RendererState.Off;

            Events["ColliderHelperEvent"].guiName = "Show Collider: Off";
        }

        private static bool FindEngineModule(GameObject go, out ModuleEngines mod)
        {
            var engineMod = go.GetComponent<ModuleEngines>();
            if (engineMod != null)
            {
                mod = engineMod;
                return true;
            }

            for (var i = 0; i < go.transform.childCount; i++)
            {
                var found = false;
                var child = go.transform.GetChild(i).gameObject;

                if (!child.GetComponent<Part>())
                    found = FindEngineModule(child, out engineMod);

                if (!found) continue;

                mod = engineMod;
                return true;
            }

            mod = null;
            return false;
        }

        public void OnDestroy()
        {
            if (_flightMarkerComponent)
            {
                Destroy(_flightMarkerComponent);
            }
        }
    }
}
