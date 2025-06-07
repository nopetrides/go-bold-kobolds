using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FIMSpace.FTextureTools
{
	public static class FTextureToolsGUIUtilities
	{
		private static GUIStyle __frameBoxStyle;
		private static GUIStyle __headerStyle;
		private static GUIStyle __headerStyleBig;
		private static GUIStyle __inBoxStyle;
		private static GUIStyle __inBoxBlankStyle;

		private static Dictionary<string, Texture2D> _Icons;

		public static GUIStyle FrameBoxStyle
		{
			get
			{
				if (__frameBoxStyle != null) return __frameBoxStyle;
				__frameBoxStyle = new GUIStyle(EditorStyles.helpBox);
				var bg = Resources.Load<Texture2D>("Fimp/Backgrounds/FFrameBox");
				__frameBoxStyle.normal.background = bg;
				__frameBoxStyle.border = new RectOffset(6, 6, 6, 6);
				__frameBoxStyle.padding = new RectOffset(1, 1, 1, 1);
				return __frameBoxStyle;
			}
		}

		public static GUIStyle HeaderStyle
		{
			get
			{
				if (__headerStyle != null) return __headerStyle;
				__headerStyle = new GUIStyle(EditorStyles.boldLabel);
				__headerStyle.richText = true;
				__headerStyle.padding = new RectOffset(0, 0, 0, 0);
				__headerStyle.margin = __headerStyle.padding;
				__headerStyle.alignment = TextAnchor.MiddleCenter;
				__headerStyle.active.textColor = Color.white;
				return __headerStyle;
			}
		}

		public static GUIStyle HeaderStyleBig
		{
			get
			{
				if (__headerStyleBig != null) return __headerStyleBig;
				__headerStyleBig = new GUIStyle(HeaderStyle);
				__headerStyleBig.fontSize = 17;
				__headerStyleBig.fontStyle = FontStyle.Normal;
				return __headerStyle;
			}
		}

		public static GUIStyle BGInBoxStyle
		{
			get
			{
				if (__inBoxStyle != null) return __inBoxStyle;
				__inBoxStyle = new GUIStyle(EditorStyles.helpBox);
				var bg = Resources.Load<Texture2D>("FInBoxSprite");
				__inBoxStyle.normal.background = bg;
				__inBoxStyle.border = new RectOffset(4, 4, 4, 4);
				__inBoxStyle.padding = new RectOffset(8, 6, 5, 5);
				__inBoxStyle.margin = new RectOffset(0, 0, 0, 0);
				return __inBoxStyle;
			}
		}

		public static GUIStyle BGInBoxBlankStyle
		{
			get
			{
				if (__inBoxBlankStyle != null) return __inBoxBlankStyle;
				__inBoxBlankStyle = new GUIStyle();
				__inBoxBlankStyle.padding = BGInBoxStyle.padding;
				__inBoxBlankStyle.margin = new RectOffset(10, 10, 4, 4);
				return __inBoxBlankStyle;
			}
		}

		public static void DrawUILineCommon(int padding = 6, int thickness = 1, float width = 0.975f)
		{
			DrawUILine(0.35f, 0.35f, thickness, padding, width);
		}

		public static void DrawUILine(Color color, int thickness = 2, int padding = 10, float width = 1f)
		{
			var rect = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
			var w = rect.width;
			var off = rect.width - rect.width * width;
			rect.height = thickness;
			rect.y += padding / 2;
			rect.x -= 2;
			rect.x += off / 2f;
			rect.width += 2;
			rect.width *= width;
			EditorGUI.DrawRect(rect, color);
		}

		public static void DrawUILine(
			float alpha, float brightness = 0.25f, int thickness = 2, int padding = 10, float width = 1f)
		{
			var rect = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
			var w = rect.width;
			var off = rect.width - rect.width * width;
			rect.height = thickness;
			rect.y += padding / 2;
			rect.x -= 2;
			rect.x += off / 2f;
			rect.width += 2;
			rect.width *= width;
			EditorGUI.DrawRect(rect, new Color(brightness, brightness, brightness, alpha));
		}

		/// <summary> Loading texture and remembering reference in the dictionary </summary>
		public static Texture FindIcon(string path)
		{
			if (_Icons == null) _Icons = new Dictionary<string, Texture2D>();

			Texture2D iconTex = null;

			if (_Icons.TryGetValue(path, out iconTex))
			{
				if (iconTex == null) _Icons.Remove(path);
				else return iconTex;
			}

			if (iconTex == null)
			{
				iconTex = Resources.Load<Texture2D>(path);
				_Icons.Add(path, iconTex);
			}

			return iconTex;
		}
	}
}
