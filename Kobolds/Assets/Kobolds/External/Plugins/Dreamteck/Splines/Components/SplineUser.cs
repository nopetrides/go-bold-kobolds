using System;
using UnityEditor;
using UnityEngine;

namespace Dreamteck.Splines
{
	[ExecuteInEditMode]
	public class SplineUser : MonoBehaviour, ISerializationCallbackReceiver, ISampleModifier
	{
		public enum UpdateMethod
		{
			Update,
			FixedUpdate,
			LateUpdate
		}

		[HideInInspector]
		public UpdateMethod updateMethod = UpdateMethod.Update;

		//Serialized values
		[SerializeField]
		[HideInInspector]
		private SplineComputer _spline;

		[SerializeField]
		[HideInInspector]
		private bool _autoUpdate = true;

		[SerializeField]
		[HideInInspector]
		protected RotationModifier _rotationModifier = new();

		[SerializeField]
		[HideInInspector]
		protected OffsetModifier _offsetModifier = new();

		[SerializeField]
		[HideInInspector]
		protected ColorModifier _colorModifier = new();

		[SerializeField]
		[HideInInspector]
		protected SizeModifier _sizeModifier = new();

		[SerializeField]
		[HideInInspector]
		private SplineSample _clipFromSample, _clipToSample;

		[SerializeField]
		[HideInInspector]
		private bool _loopSamples;

		[SerializeField]
		[HideInInspector]
		private double _clipFrom;

		[SerializeField]
		[HideInInspector]
		private double _clipTo = 1.0;

		//float values used for making animations
		[SerializeField]
		[HideInInspector]
		private float animClipFrom;

		[SerializeField]
		[HideInInspector]
		private float animClipTo = 1f;

		//Threading values
		[HideInInspector]
		public volatile bool multithreaded;

		[HideInInspector]
		public bool buildOnAwake = true;

		[HideInInspector]
		public bool buildOnEnable;

		private readonly SampleCollection _sampleCollection = new();

		private int _startSampleIndex;
		private SplineSample _workSample;

		/// <summary>
		///     Use this to work with the Evaluate and Project methods
		/// </summary>
		protected SplineSample evalResult = new();

		private bool rebuild, getSamples, postBuild;

		public SplineComputer spline
		{
			get => _spline;
			set
			{
				if (value != _spline)
				{
					if (_spline != null) _spline.Unsubscribe(this);
					_spline = value;
					if (_spline != null)
					{
						_spline.Subscribe(this);
						Rebuild();
					}

					OnSplineChanged();
				}
			}
		}

		public double clipFrom
		{
			get => _clipFrom;
			set
			{
				if (value != _clipFrom)
				{
					animClipFrom = (float) _clipFrom;
					_clipFrom = DMath.Clamp01(value);
					if (_clipFrom > _clipTo)
						if (!_spline.isClosed)
							_clipTo = _clipFrom;
					getSamples = true;
					Rebuild();
				}
			}
		}

		public double clipTo
		{
			get => _clipTo;
			set
			{
				if (value != _clipTo)
				{
					animClipTo = (float) _clipTo;
					_clipTo = DMath.Clamp01(value);
					if (_clipTo < _clipFrom)
						if (!_spline.isClosed)
							_clipFrom = _clipTo;
					getSamples = true;
					Rebuild();
				}
			}
		}

		public bool autoUpdate
		{
			get => _autoUpdate;
			set
			{
				if (value != _autoUpdate)
				{
					_autoUpdate = value;
					if (value) Rebuild();
				}
			}
		}

		public bool loopSamples
		{
			get => _loopSamples;
			set
			{
				if (value != _loopSamples)
				{
					_loopSamples = value;
					if (!_loopSamples && _clipTo < _clipFrom)
					{
						var temp = _clipTo;
						_clipTo = _clipFrom;
						_clipFrom = temp;
					}

					Rebuild();
				}
			}
		}

		//The percent of the spline that we're traversing
		public double span
		{
			get
			{
				if (samplesAreLooped) return 1.0 - _clipFrom + _clipTo;
				return _clipTo - _clipFrom;
			}
		}

		public bool samplesAreLooped => _loopSamples && _clipFrom >= _clipTo;

		public RotationModifier rotationModifier => _rotationModifier;

		public OffsetModifier offsetModifier => _offsetModifier;

		public ColorModifier colorModifier => _colorModifier;

		public SizeModifier sizeModifier => _sizeModifier;

		protected Transform trs { get; private set; }

		protected bool hasTransform { get; private set; }

		public int sampleCount { get; private set; }

		protected virtual void Awake()
		{
#if UNITY_EDITOR
			isPlaying = Application.isPlaying;
			if (!isPlaying)
				if (spline != null)
					if (!_spline.IsSubscribed(this))
					{
						_spline.Subscribe(this);
						EditorUtility.SetDirty(spline);
					}
#endif

			CacheTransform();
			if (buildOnAwake && Application.isPlaying)
				RebuildImmediate();
			else
				GetSamples();

#if UNITY_EDITOR
			if (!Application.isPlaying) RebuildImmediate();
#endif
		}

		protected virtual void Reset()
		{
#if UNITY_EDITOR
			spline = GetComponent<SplineComputer>();
			Awake();
#endif
		}

		private void Update()
		{
			if (updateMethod == UpdateMethod.Update)
			{
				Run();
				RunUpdate();
				LateRun();
			}
		}

		private void FixedUpdate()
		{
			if (updateMethod == UpdateMethod.FixedUpdate)
			{
				Run();
				RunUpdate();
				LateRun();
			}
		}

		private void LateUpdate()
		{
			if (updateMethod == UpdateMethod.LateUpdate)
			{
				Run();
				RunUpdate();
				LateRun();
			}
#if UNITY_EDITOR
			if (!isPlaying && updateMethod == UpdateMethod.FixedUpdate)
			{
				Run();
				RunUpdate();
				LateRun();
			}
#endif
		}

		protected virtual void OnEnable()
		{
#if UNITY_EDITOR
			if (!isPlaying || buildOnEnable) RebuildImmediate();
#else
            if (buildOnEnable){
                RebuildImmediate();
            }
#endif
		}

		protected virtual void OnDisable()
		{
		}

		protected virtual void OnDestroy()
		{
#if UNITY_EDITOR
			if (!isPlaying && spline != null) _spline.Unsubscribe(this); //Unsubscribe if DestroyImmediate is called
#endif
		}

		protected virtual void OnDidApplyAnimationProperties()
		{
			var clip = false;
			if (_clipFrom != animClipFrom || _clipTo != animClipTo) clip = true;
			_clipFrom = animClipFrom;
			_clipTo = animClipTo;
			Rebuild();
			if (clip) GetSamples();
		}

		/// <summary>
		///     Applies the SplineUser modifiers to the provided sample
		/// </summary>
		/// <param name="sample">The sample to modify</param>
		public void ApplySampleModifiers(ref SplineSample sample)
		{
			ApplyModifier(_offsetModifier, ref sample);
			ApplyModifier(_rotationModifier, ref sample);
			ApplyModifier(_colorModifier, ref sample);
			ApplyModifier(_sizeModifier, ref sample);
		}

		public Vector3 GetModifiedSamplePosition(ref SplineSample sample)
		{
			if (_offsetModifier.hasKeys)
			{
				var offset = _offsetModifier.Evaluate(sample.percent);
				return sample.position + sample.right * offset.x + sample.up * offset.y;
				;
			}

			return sample.position;
		}

		public virtual void OnBeforeSerialize()
		{
		}

		public virtual void OnAfterDeserialize()
		{
		}

		public event EmptySplineHandler onPostBuild;

#if UNITY_EDITOR
		public virtual void EditorAwake()
		{
		}
#endif

		protected void CacheTransform()
		{
			trs = transform;
			hasTransform = true;
		}

		/// <summary>
		///     Gets the sample at the given index without modifications
		/// </summary>
		/// <param name="index">Sample index</param>
		/// <returns></returns>
		public void GetSampleRaw(int index, ref SplineSample sample)
		{
			if (index == 0)
			{
				sample.FastCopy(ref _clipFromSample);
				return;
			}

			if (index == sampleCount - 1)
			{
				sample.FastCopy(ref _clipToSample);
				return;
			}

			ClampLoopSampleIndex(ref index);
			sample.FastCopy(ref _sampleCollection.samples[index]);
		}

		public double GetSamplePercent(int index)
		{
			if (index == 0) return _clipFromSample.percent;
			if (index == sampleCount - 1) return _clipToSample.percent;

			ClampLoopSampleIndex(ref index);
			return _sampleCollection.samples[index].percent;
		}

		private void ClampLoopSampleIndex(ref int index)
		{
			if (index >= sampleCount) index = sampleCount - 1;

			if (samplesAreLooped)
			{
				int start;
				double lerp;
				_sampleCollection.GetSamplingValues(clipFrom, out start, out lerp);

				index = start + index;
				if (index >= _sampleCollection.length) index -= _sampleCollection.length;
			}
			else
			{
				index = _startSampleIndex + index;
			}
		}


		/// <summary>
		///     Returns the sample at the given index with modifiers applied
		/// </summary>
		/// <param name="index">Sample index</param>
		/// <param name="target">Sample to write to</param>
		public void GetSample(int index, ref SplineSample target)
		{
			GetSampleRaw(index, ref target);
			ApplySampleModifiers(ref target);
		}

		/// <summary>
		///     Returns the sample at the given index with modifiers applied and
		///     applies compensation to the size parameter based on the angle between the samples
		/// </summary>
		public void GetSampleWithAngleCompensation(int index, ref SplineSample target)
		{
			GetSampleRaw(index, ref target);
			ApplySampleModifiers(ref target);
			if (index > 0 && index < sampleCount - 1)
			{
				GetSampleRaw(index - 1, ref _workSample);
				ApplySampleModifiers(ref target);
				var prev = target.position - _workSample.position;
				GetSampleRaw(index + 1, ref _workSample);
				ApplySampleModifiers(ref target);
				var next = _workSample.position - target.position;
				target.size *= 1 / Mathf.Sqrt(Vector3.Dot(prev.normalized, next.normalized) * 0.5f + 0.5f);
			}
		}


		/// <summary>
		///     Rebuild the SplineUser. This will cause Build and Build_MT to be called.
		/// </summary>
		/// <param name="sampleComputer">Should the SplineUser sample the SplineComputer</param>
		public virtual void Rebuild()
		{
#if UNITY_EDITOR
			if (!hasTransform) CacheTransform();

			//If it's the editor and it's not playing, then rebuild immediate
			if (isPlaying)
			{
				if (!autoUpdate) return;
				rebuild = getSamples = true;
			}
			else
			{
				RebuildImmediate();
			}
#else
             if (!autoUpdate) return;
             rebuild = getSamples = true;
#endif
		}

		/// <summary>
		///     Rebuild the SplineUser immediate. This method will call sample samples and call Build as soon as it's called even
		///     if the component is disabled.
		/// </summary>
		/// <param name="sampleComputer">Should the SplineUser sample the SplineComputer</param>
		public virtual void RebuildImmediate()
		{
#if UNITY_EDITOR
			if (!hasTransform) CacheTransform();
#endif
			try
			{
				GetSamples();
				Build();
				PostBuild();
			}
			catch (Exception ex)
			{
				Debug.LogError(ex.Message);
			}

			rebuild = false;
			getSamples = false;
		}

		//Update logic for handling threads and rebuilding
		private void RunUpdate()
		{
#if UNITY_EDITOR
			if (!isPlaying) return;
#endif
			//Handle rebuilding
			if (rebuild)
			{
				if (multithreaded)
				{
					if (getSamples) SplineThreading.Run(ResampleAndBuildThreaded);
					else SplineThreading.Run(BuildThreaded);
				}
				else
				{
					if (getSamples || _spline.sampleMode == SplineComputer.SampleMode.Optimized) GetSamples();
					Build();
					postBuild = true;
				}

				rebuild = false;
			}

			if (postBuild)
			{
				PostBuild();
				var postBuildHandler = onPostBuild;
				if (postBuildHandler != null) postBuildHandler();
				postBuild = false;
			}
		}

		private void BuildThreaded()
		{
			while (postBuild)
			{
				//Wait if the main thread is still running post build operations
			}

			Build();
			postBuild = true;
		}

		private void ResampleAndBuildThreaded()
		{
			while (postBuild)
			{
				//Wait if the main thread is still running post build operations
			}

			GetSamples();
			Build();
			postBuild = true;
		}

		/// Code to run every Update/FixedUpdate/LateUpdate before any building has taken place
		protected virtual void Run()
		{
		}

		/// Code to run every Update/FixedUpdate/LateUpdate after any rabuilding has taken place
		protected virtual void LateRun()
		{
		}

		//Used for calculations. Called on the main or the worker thread.
		protected virtual void Build()
		{
		}

		//Called on the Main thread only - used for applying the results from Build
		protected virtual void PostBuild()
		{
		}

		protected virtual void OnSplineChanged()
		{
		}

		private void ApplyModifier(SplineSampleModifier modifier, ref SplineSample sample)
		{
			if (!modifier.hasKeys) return;
			if (modifier.useClippedPercent) ClipPercent(ref sample.percent);

			modifier.Apply(ref sample);

			if (modifier.useClippedPercent) UnclipPercent(ref sample.percent);
		}

		/// <summary>
		///     Sets the clip range of the SplineUser. Same as setting clipFrom and clipTo
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		public void SetClipRange(double from, double to)
		{
			if (!_spline.isClosed && to < from) to = from;
			_clipFrom = DMath.Clamp01(from);
			_clipTo = DMath.Clamp01(to);
			GetSamples();
			Rebuild();
		}

		/// <summary>
		///     Gets the clipped samples defined by clipFrom and clipTo
		/// </summary>
		private void GetSamples()
		{
			getSamples = false;
			if (spline == null)
			{
				_sampleCollection.samples = new SplineSample[0];
				sampleCount = 0;
				return;
			}

			_spline.GetSamples(_sampleCollection);

			if (_sampleCollection.length == 0)
			{
				sampleCount = 0;
				return;
			}

			if (_clipFrom != 0.0)
				_sampleCollection.Evaluate(clipFrom, ref _clipFromSample);
			else
				_clipFromSample = _sampleCollection.samples[0];

			if (_clipTo != 1.0)
				_sampleCollection.Evaluate(_clipTo, ref _clipToSample);
			else
				_clipToSample = _sampleCollection.samples[_sampleCollection.length - 1];

			int start, end;
			sampleCount = _sampleCollection.GetClippedSampleCount(_clipFrom, _clipTo, out start, out end);
			double lerp;
			_sampleCollection.GetSamplingValues(_clipFrom, out _startSampleIndex, out lerp);
		}

		/// <summary>
		///     Takes a regular 0-1 percent mapped to the start and end of the spline and maps it to the clipFrom and clipTo
		///     valies. Useful for working with clipped samples
		/// </summary>
		/// <param name="percent"></param>
		/// <returns></returns>
		public double ClipPercent(double percent)
		{
			ClipPercent(ref percent);
			return percent;
		}

		/// <summary>
		///     Takes a regular 0-1 percent mapped to the start and end of the spline and maps it to the clipFrom and clipTo
		///     valies. Useful for working with clipped samples
		/// </summary>
		/// <param name="percent"></param>
		/// <returns></returns>
		public void ClipPercent(ref double percent)
		{
			if (_sampleCollection.length == 0)
			{
				percent = 0.0;
				return;
			}

			if (samplesAreLooped)
			{
				if (percent >= clipFrom && percent <= 1.0)
				{
					percent = DMath.InverseLerp(clipFrom, clipFrom + span, percent);
				} //If in the range clipFrom - 1.0
				else if (percent <= clipTo)
				{
					percent = DMath.InverseLerp(clipTo - span, clipTo, percent);
				} //if in the range 0.0 - clipTo
				else
				{
					//Find the nearest clip start
					if (DMath.InverseLerp(clipTo, clipFrom, percent) < 0.5) percent = 1.0;
					else percent = 0.0;
				}
			}
			else
			{
				percent = DMath.InverseLerp(clipFrom, clipTo, percent);
			}
		}

		public double UnclipPercent(double percent)
		{
			UnclipPercent(ref percent);
			return percent;
		}

		public void UnclipPercent(ref double percent)
		{
			if (samplesAreLooped)
			{
				if (span <= 0.00001)
				{
					percent = clipFrom;
					return;
				}

				var fromRatio = (1.0 - clipFrom) / span;
				if (percent < fromRatio)
				{
					percent = DMath.Lerp(clipFrom, 1.0, percent / fromRatio);
				}
				else if (clipTo == 0.0)
				{
					percent = 0.0;
					return;
				}
				else
				{
					percent = DMath.Lerp(0.0, clipTo, (percent - fromRatio) / (clipTo / span));
				}
			}
			else
			{
				if (percent == 0.0)
				{
					percent = clipFrom;
					return;
				}

				if (percent == 1.0)
				{
					percent = clipTo;
					return;
				}

				percent = DMath.Lerp(clipFrom, clipTo, percent);
			}

			percent = DMath.Clamp01(percent);
		}

		private int GetSampleIndex(double percent)
		{
			int index;
			double lerp;
			_sampleCollection.GetSamplingValues(UnclipPercent(percent), out index, out lerp);
			return index;
		}

		public Vector3 EvaluatePosition(double percent)
		{
			return _sampleCollection.EvaluatePosition(UnclipPercent(percent));
		}

		public void Evaluate(double percent, ref SplineSample result)
		{
			_sampleCollection.Evaluate(UnclipPercent(percent), ref result);
			result.percent = DMath.Clamp01(percent);
			ApplySampleModifiers(ref result);
		}

		public SplineSample Evaluate(double percent)
		{
			var result = new SplineSample();
			Evaluate(percent, ref result);
			result.percent = DMath.Clamp01(percent);
			ApplySampleModifiers(ref result);
			return result;
		}

		public void Evaluate(ref SplineSample[] results, double from = 0.0, double to = 1.0)
		{
			_sampleCollection.Evaluate(ref results, UnclipPercent(from), UnclipPercent(to));
			for (var i = 0; i < results.Length; i++)
			{
				ClipPercent(ref results[i].percent);
				ApplySampleModifiers(ref results[i]);
			}
		}

		public void EvaluatePositions(ref Vector3[] positions, double from = 0.0, double to = 1.0)
		{
			_sampleCollection.EvaluatePositions(ref positions, UnclipPercent(from), UnclipPercent(to));
		}

		public double Travel(double start, float distance, Spline.Direction direction, out float moved)
		{
			moved = 0f;
			if (direction == Spline.Direction.Forward && start >= 1.0) return 1.0;

			if (direction == Spline.Direction.Backward && start <= 0.0) return 0.0;
			if (distance == 0f) return DMath.Clamp01(start);

			var result = _sampleCollection.Travel(
				UnclipPercent(start), distance, direction, out moved, clipFrom, clipTo);
			var clippedResult = ClipPercent(result);

			if (result > clipTo)
				moved -= _sampleCollection.CalculateLength(clipTo, result);
			else if (result < clipFrom) moved -= _sampleCollection.CalculateLength(result, clipFrom);

			return clippedResult;
		}

		public double Travel(double start, float distance, Spline.Direction direction = Spline.Direction.Forward)
		{
			float moved;
			return Travel(start, distance, direction, out moved);
		}

		public double TravelWithOffset(
			double start, float distance, Spline.Direction direction, Vector3 offset, out float moved)
		{
			moved = 0f;
			if (direction == Spline.Direction.Forward && start >= 1.0) return 1.0;

			if (direction == Spline.Direction.Backward && start <= 0.0) return 0.0;
			if (distance == 0f) return DMath.Clamp01(start);
			var result = _sampleCollection.TravelWithOffset(
				UnclipPercent(start), distance, direction, offset, out moved, clipFrom, clipTo);
			return ClipPercent(result);
		}

		public virtual void Project(Vector3 position, ref SplineSample result, double from = 0.0, double to = 1.0)
		{
			if (_spline == null) return;
			_sampleCollection.Project(
				position, _spline.pointCount, ref result, UnclipPercent(from), UnclipPercent(to), this);
			ClipPercent(ref result.percent);
		}

		public float CalculateLength(double from = 0.0, double to = 1.0, bool preventInvert = true)
		{
			return _sampleCollection.CalculateLength(UnclipPercent(from), UnclipPercent(to), preventInvert);
		}

		public float CalculateLengthWithOffset(Vector3 offset, double from = 0.0, double to = 1.0)
		{
			return _sampleCollection.CalculateLengthWithOffset(offset, UnclipPercent(from), UnclipPercent(to));
		}

		/// <summary>
		///     Returns the offset transformed by the sample
		/// </summary>
		/// <param name="sample">Source sample</param>
		/// <param name="localOffset">Local offset to apply</param>
		/// <returns></returns>
		protected static Vector3 TransformOffset(SplineSample sample, Vector3 localOffset)
		{
			return (sample.right * localOffset.x + sample.up * localOffset.y + sample.forward * localOffset.z) *
					sample.size;
		}
#if UNITY_EDITOR
		protected bool isPlaying { get; private set; }
#endif
	}
}
