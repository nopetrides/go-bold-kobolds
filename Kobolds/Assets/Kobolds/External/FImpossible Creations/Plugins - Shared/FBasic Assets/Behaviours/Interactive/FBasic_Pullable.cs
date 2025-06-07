using UnityEngine;
using UnityEngine.Events;

namespace FIMSpace.Basics
{
	/// <summary>
	///     FM: Class to detect mouse dragging object to change value by moving mouse
	/// </summary>
	public class FBasic_Pullable : FBasic_InteractionAreaCanvas
	{
		[Header(" --- Pullable Parameters --- ")]
		[Space(5f)]
		public UnityEvent EventOnKeyUpInteraction;

		[Space(10f)]
		[Tooltip("If value for dragging should increase / decrease on moving with mouse on x axis")]
		public bool XAxis;

		[Tooltip("If value for dragging should increase / decrease on moving with mouse on y axis")]
		public bool YAxis = true;

		[Tooltip("If we stop dragging, value comes back to 0")]
		public bool ResetValue;

		[Tooltip("How much value should be changed by mouse movement")]
		public float Sensitivity = 0.25f;

		[Range(0f, 1f)]
		public float StartValueY;

		[Range(0f, 1f)]
		public float StartValueX;

		//v1.1
		public bool CanBePulledByMouse = true;

		[Tooltip(
			"If you want be able to pool it without need to be as player in object's range using mouse (pulling lever just using mouse from any distance)")]
		public bool OverrideCanBePulledByMouse;

		private Vector2 holdStartPosition;
		private float holdStartValueX;

		private float holdStartValueY;

		protected bool mouseEntered;

		private CursorLockMode previousCursorLockMode;
		private bool previousCursorVisibility;

		/// <summary> To hold space for clicking holding implementation instead of interaction key hold </summary>
		private bool startedByMouseClick;

		/// <summary> Dragged distance value in Y (most cases) </summary>
		public float YValue { get; protected set; }

		public float YValueUnclamped { get; private set; }

		/// <summary> Dragged mouse distance value in X </summary>
		public float XValue { get; protected set; }

		public float XValueUnclamped { get; private set; }

		//V1.1.1
		public bool Holding { get; protected set; }


		protected override void Start()
		{
			base.Start();
			Holding = false;
			YValue = StartValueY * 100f;
			YValueUnclamped = YValue;
			holdStartValueY = YValue;

			XValue = StartValueX * 100f;
			XValueUnclamped = XValue;
			holdStartValueX = XValue;
			mouseEntered = false;

			EventOnInteraction.AddListener(StartHolding);
		}

		protected virtual void OnMouseDown()
		{
			if (!CanBePulledByMouse) return;

			StartHolding();
			startedByMouseClick = true;
		}

		protected virtual void OnMouseEnter()
		{
			if (!CanBePulledByMouse) return;

			//v1.1
			if (OverrideCanBePulledByMouse)
				if (!Entered)
				{
					StartCoroutine(UpdateIfInRange());
					OnEnter();
					Entered = true;
				}

			if (Entered) MouseEnter();
		}

		/// <summary>
		///     Method to update pointers of pullable derives'
		/// </summary>
		protected virtual void UpdatePullableOrientation()
		{
		}

		/// <summary>
		///     When mouse enter on object's collider and when player is inside reach area
		/// </summary>
		protected virtual void MouseEnter()
		{
			if (!CanBePulledByMouse) return;

			mouseEntered = true;
		}

		protected virtual void StartHolding()
		{
			var c = Camera.main;

			if (c)
			{
				var tpp = c.GetComponent<FBasic_TPPCameraBehaviour>();
				if (tpp)
				{
					tpp.enabled = false;
				}
				else
				{
					var freeC = c.GetComponent<FBasic_FreeCameraBehaviour>();
					if (freeC) freeC.enabled = false;
				}
			}

			if (!Holding)
			{
				previousCursorLockMode = Cursor.lockState;
				previousCursorVisibility = Cursor.visible;

				Holding = true;
				holdStartPosition = Input.mousePosition;
				holdStartValueY = YValue;
				holdStartValueX = XValue;
				mouseEntered = true;

				Cursor.visible = false;
				Cursor.lockState = CursorLockMode.None;
			}
		}

		protected override void UpdateIn()
		{
			base.UpdateIn();

			if (!startedByMouseClick)
				if (Input.GetKeyUp(InteractionKey))
					Holding = false;

			if (Holding)
			{
				if (Input.GetMouseButtonUp(0)) Holding = false;

				var differenceValue = 0f;

				if (YAxis) differenceValue = holdStartPosition.y - Input.mousePosition.y;

				YValueUnclamped = holdStartValueY - differenceValue * Sensitivity;
				YValue = Mathf.Clamp(YValueUnclamped, 0f, 100f);

				differenceValue = 0f;
				if (XAxis) differenceValue = Input.mousePosition.x - holdStartPosition.x;
				XValueUnclamped = holdStartValueX - differenceValue * Sensitivity;
				XValue = Mathf.Clamp(XValueUnclamped, 0f, 100f);


				Cursor.visible = false;
				Cursor.lockState = CursorLockMode.None;
			}
			else
			{
				if (mouseEntered) StopHolding();
			}
		}

		protected override void OnExit()
		{
			StopHolding();
			base.OnExit();
		}


		/// <summary>
		///     Resetting values for holding object
		/// </summary>
		protected virtual void StopHolding()
		{
			EventOnKeyUpInteraction.Invoke();
			if (ResetValue) YValue = 0f;

			Cursor.lockState = previousCursorLockMode;
			Cursor.visible = previousCursorVisibility;

			startedByMouseClick = false;
			mouseEntered = false;
			Holding = false;

			var c = Camera.main;

			if (c)
			{
				var tpp = c.GetComponent<FBasic_TPPCameraBehaviour>();
				if (tpp)
				{
					tpp.enabled = true;
				}
				else
				{
					var freeC = c.GetComponent<FBasic_FreeCameraBehaviour>();
					if (freeC) freeC.enabled = true;
				}
			}

			//v1.1
			if (!Entered)
				if (CanBePulledByMouse)
					if (OverrideCanBePulledByMouse)
					{
						StopAllCoroutines();
						OnExit();
						Entered = false;
					}
		}
	}
}
