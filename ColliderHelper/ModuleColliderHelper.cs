using System.Collections.Generic;
using System.Linq;
using UnityEngine;
// ReSharper disable ArrangeThisQualifier
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

        [KSPEvent(guiActive = true, advancedTweakable = true, guiActiveUnfocused = true, guiActiveUncommand = true,
            externalToEVAOnly = false, guiActiveEditor = false, unfocusedRange = 100f, guiName = "Toggle CoL/M",
            active = true, isPersistent = false)]
        public void ToggleFlightMarkers()
        {
            ColliderHelper.ToggleFlightMarkers(this.vessel);
        }

        [KSPEvent(guiActive = true, guiActiveUnfocused = true, guiActiveUncommand = true, externalToEVAOnly = false,
            guiActiveEditor = true, unfocusedRange = 100f, guiName = "Show Collider: Off", active = true,
            advancedTweakable = true, isPersistent = false)]
        public void ColliderHelperEvent()
        {
            CycleState();
        }

        public void CycleState()
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch(_state)
            {
                case RendererState.Active:
                    // on->symmetry|off
                    if (this.part.symmetryCounterparts.Count > 0)
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
                    if (this.part.symmetryCounterparts.Count > 0)
                    {
                        var onCount = this.part.symmetryCounterparts.Count(t => t.GetComponent<ModuleColliderHelper>()._state == RendererState.Active);

                        if (onCount == this.part.symmetryCounterparts.Count)
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
            if (this.gameObject.GetComponent<WireframeComponent>() == null)
                this.gameObject.AddComponent<WireframeComponent>();

            if (_thrustArrows == null)
            {
                ModuleEngines engineMod;
                if (FindEngineModule(this.gameObject, out engineMod))
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
                for (var i = 0; i < this.part.symmetryCounterparts.Count; i++)
                {
                    var component = this.part.symmetryCounterparts[i].GetComponent<ModuleColliderHelper>();

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
                for (var i = 0; i < this.part.symmetryCounterparts.Count; i++)
                {
                    var helperComponent = this.part.symmetryCounterparts[i].GetComponent<ModuleColliderHelper>();

                    helperComponent?.SetOff(false);
                }
            }

            var renderComponent = this.gameObject.GetComponent<WireframeComponent>();
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

        private bool FindEngineModule(GameObject go, out ModuleEngines mod)
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
    }
}
