using UnityEngine;

namespace MoreMountains.Tools
{
	/// <summary>
	///     Decisions are components that will be evaluated by transitions, every frame, and will return true or false.
	///     Examples include time spent in a state, distance to a target, or object detection within an area.
	/// </summary>
	public abstract class AIDecision : MonoBehaviour
	{
		/// a label you can set to organize your AI Decisions, not used by anything else
		[Tooltip("a label you can set to organize your AI Decisions, not used by anything else")]
		public string Label;

		protected AIBrain _brain;
		public virtual bool DecisionInProgress { get; set; }

		/// <summary>
		///     On Awake we grab our Brain
		/// </summary>
		protected virtual void Awake()
		{
			_brain = gameObject.GetComponentInParent<AIBrain>();
		}

		/// Decide will be performed every frame while the Brain is in a state this Decision is in. Should return true or false, which will then determine the transition's outcome.
		public abstract bool Decide();

		/// <summary>
		///     Meant to be overridden, called when the game starts
		/// </summary>
		public virtual void Initialization()
		{
		}

		/// <summary>
		///     Meant to be overridden, called when the Brain enters a State this Decision is in
		/// </summary>
		public virtual void OnEnterState()
		{
			DecisionInProgress = true;
		}

		/// <summary>
		///     Meant to be overridden, called when the Brain exits a State this Decision is in
		/// </summary>
		public virtual void OnExitState()
		{
			DecisionInProgress = false;
		}
	}
}
