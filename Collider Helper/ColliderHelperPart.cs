using System.Linq;
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

    public class ColliderHelperPart : PartModule
    {
        private RendererState _state = RendererState.Off;

        [KSPField(isPersistant = false, advancedTweakable = true, guiActiveEditor = true, guiActive = false,
            guiName = "Part Opacity"), UI_FloatRange(minValue = 0.1f, maxValue = 1.0f, stepIncrement = 0.1f)]
        public float PartOpacity = 1.0f;

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
                        var onCount = this.part.symmetryCounterparts.Count(t => t.GetComponent<ColliderHelperPart>()._state == RendererState.Active);

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
                    var component = this.part.symmetryCounterparts[i].GetComponent<ColliderHelperPart>();

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
                    var helperComponent = this.part.symmetryCounterparts[i].GetComponent<ColliderHelperPart>();

                    helperComponent?.SetOff(false);
                }
            }

            var renderComponent = this.gameObject.GetComponent<WireframeComponent>();
            if (renderComponent != null)
                Destroy(renderComponent);

            _state = RendererState.Off;

            Events["ColliderHelperEvent"].guiName = "Show Collider: Off";
        }

        public void Update()
        {
            this.part.SetOpacity(PartOpacity);
        }

        public void OnDestroy()
        {
            this.part.SetOpacity(1.0f);
        }
    }
}