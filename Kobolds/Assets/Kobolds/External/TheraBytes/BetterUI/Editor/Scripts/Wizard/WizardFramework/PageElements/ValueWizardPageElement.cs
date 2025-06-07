using System;
using UnityEngine;

namespace TheraBytes.BetterUi.Editor
{
	public class ValueWizardPageElement<T> : WizardPageElementBase, IWizardDataElement
	{
		private readonly Func<ValueWizardPageElement<T>, T, T> drawGuiCallback;
		private T value;
		private readonly Action<ValueWizardPageElement<T>> valueChangedCallback;

		public ValueWizardPageElement(
			string serializationKey, Func<ValueWizardPageElement<T>, T, T> drawGuiCallback,
			Action<ValueWizardPageElement<T>> valueChangedCallback = null)
		{
			SerializationKey = serializationKey;
			this.drawGuiCallback = drawGuiCallback;
			this.valueChangedCallback = valueChangedCallback;
		}

		public T Value => value;
		public string SerializationKey { get; }

		public string GetValueAsString()
		{
			return ParseHelper.ToParsableString(Value);
		}

		public bool TrySetValue(string input)
		{
			return ParseHelper.TryParse(input, out value);
		}

		public override void DrawGui()
		{
			if (drawGuiCallback == null)
			{
				Debug.LogError("No gui callback assigned for wizard element: " + SerializationKey);
				State = WizardElementState.Complete;
				return;
			}

			var prev = value;
			value = drawGuiCallback(this, value);

			if (value != null && !value.Equals(prev) && valueChangedCallback != null) valueChangedCallback(this);
		}
	}
}
