using UnityEngine;
// ReSharper disable ForCanBeConvertedToForeach

namespace ColliderHelper
{
	public class ThrustArrowComponent : MonoBehaviour
	{
		private const string _arrowShader = "Legacy Shaders/Particles/Alpha Blended";
		private const int _arrowLayer = 0;
		private const float _lineLength = 1.0f;

		private Color _color = XKCDColors.Yellow;
		private Material _material;

		private LineRenderer _lineStart;
		private LineRenderer _lineEnd;

		public void Awake()
		{
			_material = new Material(Shader.Find(_arrowShader));
		}

		public void Start()
		{
			_color.a = 0.75f;

			_lineStart = NewLine();
			_lineStart.positionCount = 2;
			_lineStart.startColor = _color;
			_lineStart.endColor = _color;
			_lineStart.startWidth = 0.1f;
			_lineStart.endWidth = 0.1f;
			_lineStart.SetPosition(0, Vector3.zero);
			_lineStart.SetPosition(1, gameObject.transform.parent.forward * (_lineLength - 0.2f));
			_lineStart.enabled = true;

			_lineEnd = NewLine();
			_lineEnd.positionCount = 2;
			_lineEnd.startColor = _color;
			_lineEnd.endColor = _color;
			_lineEnd.startWidth = 0.2f;
			_lineEnd.endWidth = 0f;
			_lineEnd.SetPosition(0, gameObject.transform.parent.forward * (_lineLength - 0.2f));
			_lineEnd.SetPosition(1, gameObject.transform.parent.forward * _lineLength);
			_lineEnd.enabled = true;
		}

		private LineRenderer NewLine()
		{
			var obj = new GameObject("ColliderHelper LineRenderer object");
			var lr = obj.AddComponent<LineRenderer>();
			obj.transform.parent = gameObject.transform;
			obj.transform.localPosition = Vector3.zero;
			obj.layer = _arrowLayer;
			lr.material = _material;
			lr.useWorldSpace = false;
			return lr;
		}
	}
}
