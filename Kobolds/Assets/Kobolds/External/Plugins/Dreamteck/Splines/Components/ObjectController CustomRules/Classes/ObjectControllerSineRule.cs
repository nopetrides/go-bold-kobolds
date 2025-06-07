using UnityEngine;

namespace Dreamteck.Splines
{
	//Use the CreateAssetMenu attribute to add the object to the Create Asset context menu
	//After that, go to Assets/Create/Dreamteck/Splines/... and create the scriptable object
	[CreateAssetMenu(menuName = "Dreamteck/Splines/Object Controller Rules/Sine Rule")]
	public class ObjectControllerSineRule : ObjectControllerCustomRuleBase
	{
		[SerializeField] private bool _useSplinePercent;
		[SerializeField] private float _frequency = 1f;
		[SerializeField] private float _amplitude = 1f;
		[SerializeField] private float _angle;
		[SerializeField] private float _minScale = 1f;
		[SerializeField] private float _maxScale = 1f;
		[SerializeField] [Range(0f, 1f)] private float _offset;

		public bool useSplinePercent
		{
			get => _useSplinePercent;
			set => _useSplinePercent = value;
		}

		public float frequency
		{
			get => _frequency;
			set => _frequency = value;
		}

		public float amplitude
		{
			get => _amplitude;
			set => _amplitude = value;
		}

		public float angle
		{
			get => _angle;
			set => _angle = value;
		}

		public float minScale
		{
			get => _minScale;
			set => _minScale = value;
		}

		public float maxScale
		{
			get => _maxScale;
			set => _maxScale = value;
		}

		public float offset
		{
			get => _offset;
			set
			{
				_offset = value;
				if (_offset > 1) _offset -= Mathf.FloorToInt(_offset);
				if (_offset < 0) _offset += Mathf.FloorToInt(-_offset);
			}
		}

		//Override GetOffset, GetRotation and GetScale to implement custom behaviors
		//Use the information from currentSample, currentObjectIndex, totalObjects and currentObjectPercent

		public override Vector3 GetOffset()
		{
			var sin = GetSine();
			return Quaternion.AngleAxis(_angle, Vector3.forward) * Vector3.up * sin * _amplitude;
		}

		public override Vector3 GetScale()
		{
			return Vector3.Lerp(Vector3.one * _minScale, Vector3.one * _maxScale, GetSine());
		}

		private float GetSine()
		{
			var objectPercent = _useSplinePercent ? (float) currentSample.percent : currentObjectPercent;
			return Mathf.Sin(Mathf.PI * _offset + objectPercent * Mathf.PI * _frequency);
		}
	}
}
