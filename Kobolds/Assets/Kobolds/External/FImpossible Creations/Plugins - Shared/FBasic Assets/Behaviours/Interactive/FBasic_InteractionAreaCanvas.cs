using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FIMSpace.Basics
{
	/// <summary>
	///     FM: Script for creating interaction area and canvas with text viewed on object and handling choosed event
	/// </summary>
	public class FBasic_InteractionAreaCanvas : FBasic_InteractionAreaBase
	{
		public static Transform InteractionCanvasesContainer;

		[Space(3f)]
		public KeyCode InteractionKey = KeyCode.E;

		[Space(10f)]
		public Vector3 canvasObjectOffset;

		[Space(10f)]
		public UnityEvent EventOnInteraction;

		public string textInCanvas = "Interact";
		protected CanvasGroup canvasGroup;
		protected RectTransform canvasRect;

		protected Canvas viewCanvas;

		protected Text viewText;

		protected override void Start()
		{
			base.Start();

			// Creating canvas to view text on it 
			var canvasObject = new GameObject("CanvasInteraction-" + name);
			canvasObject.transform.position = transform.position + transform.TransformVector(canvasObjectOffset);
			viewCanvas = canvasObject.AddComponent<Canvas>();
			canvasRect = canvasObject.GetComponent<RectTransform>();
			viewCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

			// When this game object will be destroyed, canvas will be also destroyed 
			// (if object is mobile text viewed on canvas will translate in ugly manner, so we let transforms be separated)
			gameObject.AddComponent<FBasic_DestroyOthersWithMe>().AddToDestroy(canvasObject);

			// Just canvas group to fade everything
			canvasGroup = canvasObject.AddComponent<CanvasGroup>();
			canvasGroup.alpha = 0f;

			// Creating Text object and assigning base variables
			var textObject = new GameObject("CanvasInteraction-Text-" + name);
			textObject.transform.SetParent(canvasObject.transform);
			textObject.transform.position = canvasObject.transform.position;
			viewText = textObject.AddComponent<Text>();
			viewText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
			viewText.alignment = TextAnchor.MiddleCenter;
			viewText.rectTransform.sizeDelta = new Vector2(500f, 300f);
			viewText.text = "[" + InteractionKey + "] " + textInCanvas;

			if (InteractionCanvasesContainer == null)
				InteractionCanvasesContainer = new GameObject("Interaction Canvases-Container").transform;

			canvasObject.transform.SetParent(InteractionCanvasesContainer, true);

			toLookPositionOffset = canvasObjectOffset;
		}

		private void OnDrawGizmos()
		{
			Gizmos.color = new Color(0.1f, 0.8f, 0.1f, 0.5f);
			Gizmos.DrawSphere(transform.position + transform.TransformVector(canvasObjectOffset), 0.25f);
		}

		protected override void UpdateIn()
		{
			if (!Focused)
			{
				canvasGroup.alpha = 0f;
				return;
			}

			// Waiting for input to invoke actions defined in event
			if (Input.GetKeyDown(InteractionKey))
				if (EventOnInteraction != null)
					EventOnInteraction.Invoke();


			// Setting position of text to be viewed on object in 3D space on 2D canvas
			var targetPos = FVectorMethods.GetUIPositionFromWorldPosition(
				transform.position + transform.TransformVector(canvasObjectOffset), Camera.main, canvasRect);

			if (targetPos.z > 0f)
			{
				// Fading in quickly canvas
				canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 1.05f, Time.deltaTime * 5f);

				targetPos.z = 0f;
				viewText.rectTransform.anchoredPosition = targetPos;
			}
			else
			{
				canvasGroup.alpha = 0f;
			}
		}

		protected override void OnEnter()
		{
			base.OnEnter();

			viewCanvas.gameObject.transform.position =
				transform.position + transform.TransformVector(canvasObjectOffset);

			if (InteractionKey != KeyCode.None) viewText.text = "[" + InteractionKey + "] " + textInCanvas;
			else viewText.text = textInCanvas;
		}

		protected override void OnExit()
		{
			canvasGroup.alpha = 0f;
			base.OnExit();
		}
	}
}
