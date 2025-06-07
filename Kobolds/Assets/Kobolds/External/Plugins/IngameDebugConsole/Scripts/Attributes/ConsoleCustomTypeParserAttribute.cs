using System;

namespace IngameDebugConsole
{
	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	public class ConsoleCustomTypeParserAttribute : ConsoleAttribute
	{
		public readonly string readableName;
		public readonly Type type;

		public ConsoleCustomTypeParserAttribute(Type type, string readableName = null)
		{
			this.type = type;
			this.readableName = readableName;
		}

		public override int Order => 0;

		public override void Load()
		{
			DebugLogConsole.AddCustomParameterType(
				type,
				(DebugLogConsole.ParseFunction) Delegate.CreateDelegate(typeof(DebugLogConsole.ParseFunction), Method),
				readableName);
		}
	}
}
