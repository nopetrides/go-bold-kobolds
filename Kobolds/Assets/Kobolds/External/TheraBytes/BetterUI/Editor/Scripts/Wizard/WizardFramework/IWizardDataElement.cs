namespace TheraBytes.BetterUi.Editor
{
	public interface IWizardDataElement
	{
		string SerializationKey { get; }
		string GetValueAsString();
		bool TrySetValue(string input);
	}
}
