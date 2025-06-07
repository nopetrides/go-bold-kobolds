using UnityEngine;
using UnityEngine.UI;

namespace LeTai.TrueShadow.Demo
{
	[RequireComponent(typeof(Image))]
	public class GradientSlider : MonoBehaviour
	{
		public Gradient gradient;

		private Image image;

		private void Start()
		{
			image = GetComponent<Image>();
		}

		public void Set(float value)
		{
			if (!image) return;

			image.fillAmount = value;
			image.color = gradient.Evaluate(value);
		}
	}
}
