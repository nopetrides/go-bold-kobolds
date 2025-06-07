using UnityEngine;

namespace Dreamteck.Splines
{
	[RequireComponent(typeof(MeshFilter))]
	[RequireComponent(typeof(MeshRenderer))]
	[AddComponentMenu("Dreamteck/Splines/Users/Spline Renderer")]
	[ExecuteInEditMode]
	public class SplineRenderer : MeshGenerator
	{
		[HideInInspector]
		public bool autoOrient = true;

		[HideInInspector]
		public int updateFrameInterval;

		[SerializeField]
		[HideInInspector]
		private int _slices = 1;

		private int _currentFrame;
		private bool _init;
		private bool _orthographic;
		private Vector3 _vertexDirection = Vector3.up;

		public int slices
		{
			get => _slices;
			set
			{
				if (value != _slices)
				{
					if (value < 1) value = 1;
					_slices = value;
					Rebuild();
				}
			}
		}

		private void Start()
		{
			if (Camera.current != null)
				_orthographic = Camera.current.orthographic;
			else if (Camera.main != null) _orthographic = Camera.main.orthographic;

			CreateMesh();
		}

		private void OnWillRenderObject()
		{
			if (!autoOrient) return;
			if (updateFrameInterval > 0)
				if (_currentFrame != 0)
					return;

			if (!Application.isPlaying)
				if (!_init)
				{
					Awake();
					_init = true;
				}

			if (Camera.current != null)
				RenderWithCamera(Camera.current);
			else if (Camera.main) RenderWithCamera(Camera.main);
		}

		protected override void LateRun()
		{
			if (updateFrameInterval > 0)
			{
				_currentFrame++;
				if (_currentFrame > updateFrameInterval) _currentFrame = 0;
			}
		}

		protected override void BuildMesh()
		{
			base.BuildMesh();
			GenerateVertices(_vertexDirection, _orthographic);
			MeshUtility.GeneratePlaneTriangles(ref _tsMesh.triangles, _slices, sampleCount, false);
		}

		public void RenderWithCamera(Camera cam)
		{
			_orthographic = cam.orthographic;
			if (_orthographic)
				_vertexDirection = -cam.transform.forward;
			else
				_vertexDirection = cam.transform.position;
			BuildMesh();
			WriteMesh();
		}

		public void GenerateVertices(Vector3 vertexDirection, bool orthoGraphic)
		{
			AllocateMesh((_slices + 1) * sampleCount, _slices * (sampleCount - 1) * 6);
			var vertexIndex = 0;
			ResetUVDistance();
			var hasOffset = offset != Vector3.zero;
			for (var i = 0; i < sampleCount; i++)
			{
				GetSample(i, ref evalResult);
				var center = evalResult.position;
				if (hasOffset)
					center += offset.x * -Vector3.Cross(evalResult.forward, evalResult.up) + offset.y * evalResult.up +
							offset.z * evalResult.forward;
				Vector3 vertexNormal;
				if (orthoGraphic) vertexNormal = vertexDirection;
				else vertexNormal = (vertexDirection - center).normalized;
				var vertexRight = Vector3.Cross(evalResult.forward, vertexNormal).normalized;
				if (uvMode == UVMode.UniformClamp || uvMode == UVMode.UniformClip) AddUVDistance(i);
				var vertexColor = evalResult.color * color;
				for (var n = 0; n < _slices + 1; n++)
				{
					var slicePercent = (float) n / _slices;
					_tsMesh.vertices[vertexIndex] = center - vertexRight * evalResult.size * 0.5f * size +
													vertexRight * evalResult.size * slicePercent * size;
					CalculateUVs(evalResult.percent, slicePercent);
					_tsMesh.uv[vertexIndex] = Vector2.one * 0.5f +
											(Vector2) (Quaternion.AngleAxis(uvRotation + 180f, Vector3.forward) *
														(Vector2.one * 0.5f - __uvs));
					_tsMesh.normals[vertexIndex] = vertexNormal;
					_tsMesh.colors[vertexIndex] = vertexColor;
					vertexIndex++;
				}
			}
		}
	}
}
