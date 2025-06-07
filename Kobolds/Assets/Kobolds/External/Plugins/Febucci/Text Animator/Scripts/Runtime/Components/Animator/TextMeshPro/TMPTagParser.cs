using System.Globalization;
using System.Text;

namespace Febucci.UI.Core.Parsing
{
	public class TMPTagParser : TagParserBase
	{
		//PS no "noparse" since it's already checked
		private static readonly TMPTagInfo[] lookups =
		{
			new("<align="), new("</align>"),
			new("<allcaps>"), new("</allcaps>"),
			new("<alpha="), new("</alpha>"),
			new("<b>"), new("</b>"),
			new("<color="), new("</color>"),
			new("<cspace="), new("</cspace>"),
			new("<font="), new("</font>"),
			new("<font-weight="), new("</font-weight>"),
			new("<gradient="), new("</gradient>"),
			new("<i>"), new("</i>"),
			new("<indent="), new("</indent>"),
			new("<line-height="), new("</line-height>"),
			new("<line-indent="), new("</line-indent>"),
			new("<link="), new("</link>"),
			new("<link>"), new("</link>"),
			new("<lowercase>"), new("</lowercase>"),
			new("<margin="), new("</margin>"), new("<margin-left>"),
			new("<margin-right>"),
			new("<mark="), new("</mark>"),
			new("<mspace="), new("</mspace>"),
			new("<nobr>"), new("</nobr>"),
			new("<page>"),
			new("<pos="),
			new("<rotate="), new("</rotate>"),
			new("<s>"), new("</s>"),
			new("<size="), new("</size>"),
			new("<smallcaps>"), new("</smallcaps>"),
			new("<space="),
			new("<sprite", true), new("<sprite ", true),
			new("<style="), new("</style>"),
			new("<sub>"), new("</sub>"),
			new("<sup>"), new("</sup>"),
			new("<u>"), new("</u>"),
			new("<uppercase>"), new("</uppercase>"),
			new("<voffset="), new("</voffset>"),
			new("<width="), new("</width>"),
			new("<br>", true)
		};

		private readonly bool richTagsEnabled;

		public TMPTagParser(bool richTagsEnabled, char openingBracket, char closingBracket, char closingTagSymbol)
			: base(openingBracket, closingBracket, closingTagSymbol)
		{
			this.richTagsEnabled = richTagsEnabled;
		}

		public override bool TryProcessingTag(
			string textInsideBrackets, int tagLength, ref int realTextIndex, StringBuilder finalTextBuilder,
			int internalOrder)
		{
			if (!richTagsEnabled) return false;

			var fullTag = startSymbol + textInsideBrackets + endSymbol; //TODO improve

			foreach (var lookupTag in lookups)
				if (fullTag.StartsWith(lookupTag.tagOpening, true, CultureInfo.InvariantCulture))
				{
					finalTextBuilder.Append(fullTag);
					if (lookupTag.increasesTextLength) realTextIndex++;
					return true;
				}

			return false;
		}

		private struct TMPTagInfo
		{
			public readonly string tagOpening;
			public readonly bool increasesTextLength;

			public TMPTagInfo(string tagOpening, bool increasesTextLength = false)
			{
				this.tagOpening = tagOpening;
				this.increasesTextLength = increasesTextLength;
			}
		}
	}
}
