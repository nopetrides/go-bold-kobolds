using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Dreamteck
{
	public class AsyncJobSystem : MonoBehaviour
	{
		private IJobData _currentJob;

		private bool _isWorking;
		private readonly Queue<IJobData> _jobs = new();

		private void Update()
		{
			if (_jobs.Count > 0 && !_isWorking) StartCoroutine(JobCoroutine());
		}

		public AsyncJobOperation ScheduleJob<T>(JobData<T> data)
		{
			_jobs.Enqueue(data);
			return new AsyncJobOperation(data);
		}

		private IEnumerator JobCoroutine()
		{
			_isWorking = true;

			while (_jobs.Count > 0)
			{
				_currentJob = _jobs.Dequeue();
				_currentJob.Initialize();

				while (!_currentJob.done)
				{
					_currentJob.Next();
					yield return null;
				}

				_currentJob.Complete();
				_currentJob = null;

				yield return null;
			}

			_isWorking = false;
		}


		public class AsyncJobOperation : CustomYieldInstruction
		{
			private readonly IJobData _job;

			public AsyncJobOperation(IJobData job)
			{
				_job = job;
			}

			public override bool keepWaiting => !_job.done;
		}

		public interface IJobData
		{
			bool done { get; }

			void Initialize();

			void Next();

			void Complete();
		}

		public class JobData<T> : IJobData
		{
			private IEnumerator<T> _enumerator;

			private readonly int _iterations;

			private readonly Action<JobData<T>> _onComplete;

			private readonly Action<JobData<T>> _onIteration;

			public JobData(IEnumerable<T> collection, int iterations, Action<JobData<T>> onIteration)
			{
				this.collection = collection;
				_onIteration = onIteration;
				_iterations = iterations;
				done = false;
			}

			public JobData(
				IEnumerable<T> collection, int iterations, Action<JobData<T>> onIteration,
				Action<JobData<T>> onComplete) :
				this(collection, iterations, onIteration)
			{
				_onComplete = onComplete;
			}

			public T current => _enumerator.Current;

			public int index { get; private set; }

			public IEnumerable<T> collection { get; }

			public bool done { get; private set; }

			public void Initialize()
			{
				_enumerator = collection.GetEnumerator();
				index = -1;
				done = !_enumerator.MoveNext();
			}

			public void Complete()
			{
				_enumerator.Dispose();

				try
				{
					if (_onComplete != null) _onComplete(this);
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
			}

			public void Next()
			{
				var counter = _iterations;

				if (done) return;
				do
				{
					index++;

					try
					{
						if (_onIteration != null) _onIteration(this);
					}
					catch (Exception e)
					{
						Debug.LogException(e);
					}

					done = !_enumerator.MoveNext();
				} while (!done && --counter > 0);
			}
		}
	}
}
