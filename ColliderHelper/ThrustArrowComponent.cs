using UnityEngine;
// ReSharper disable ArrangeThisQualifier
// ReSharper disable ForCanBeConvertedToForeach

namespace ColliderHelper
{
    public class ThrustArrowComponent : MonoBehaviour
    {
        private const string shader = "Particles/Alpha Blended";
        private const int layer = 0; //2
        private const float lineLength = 1.0f;

        private Color _color = XKCDColors.Yellow;
        private Material _material;

        private LineRenderer _lineStart;
        private LineRenderer _lineEnd;

        public void Awake()
        {
            _material = new Material(Shader.Find(shader));
        }

        public void Start()
        {
            _color.a = 0.75f;

            _lineStart = NewLine();
            _lineStart.SetVertexCount(2);
            _lineStart.SetColors(_color, _color);
            _lineStart.SetWidth(0.1f, 0.1f);
            _lineStart.SetPosition(0, Vector3.zero);
            _lineStart.SetPosition(1, this.gameObject.transform.parent.forward * (lineLength - 0.2f));
            _lineStart.enabled = true;

            _lineEnd = NewLine();
            _lineEnd.SetVertexCount(2);
            _lineEnd.SetColors(_color, _color);
            _lineEnd.SetWidth(0.2f, 0f);
            _lineEnd.SetPosition(0, this.gameObject.transform.parent.forward * (lineLength - 0.2f));
            _lineEnd.SetPosition(1, this.gameObject.transform.parent.forward * lineLength);
            _lineEnd.enabled = true;
        }

        private LineRenderer NewLine()
        {
            var obj = new GameObject("ColliderHelper LineRenderer object");
            var lr = obj.AddComponent<LineRenderer>();
            obj.transform.parent = this.gameObject.transform;
            obj.transform.localPosition = Vector3.zero;
            obj.layer = layer;
            lr.material = _material;
            lr.useWorldSpace = false;
            return lr;
        }
    }
}
