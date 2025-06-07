using System;
using FIMSpace.FEditor;
using UnityEditor;
using UnityEngine;

namespace FIMSpace.FTextureTools
{
	public class FBlendToolWindow : FTextureProcessWindow
	{
		private float BlendValue = 0.5f;

		private Texture2D BlendWith;
		private Texture2D BlendWithMemo;

		private bool BlendWithReadable;
		private readonly float BlendWithTiling = 1f;

		private EBlendMode Mode = EBlendMode.AlphaBlend;

		private Func<Color32, Color32, Color32> pixelOperation;

		public static void Init()
		{
			var window = (FBlendToolWindow) GetWindow(typeof(FBlendToolWindow));
			window.titleContent = new GUIContent(
				"Blend Tool", FTextureToolsGUIUtilities.FindIcon("SPR_BlendGen"), "Blend two textures operation");
			window.previewScale = FEPreview.m_1x1;
			window.drawPreviewScale = true;

			window.previewSize = 100;
			window.position = new Rect(340, 50, 550, 580);
			window.Show();
			called = true;
		}


		protected override void OnGUICustom()
		{
			EditorGUI.BeginChangeCheck();

			Mode = (EBlendMode) EditorGUILayout.EnumPopup("Mode:", Mode);
			GUILayout.Space(8);
			BlendValue = EditorGUILayout.Slider("Blend:", BlendValue, 0f, 1f);

			GUILayout.Space(8);
			BlendWith = EditorGUILayout.ObjectField("Blend With:", BlendWith, typeof(Texture2D), false) as Texture2D;

#region Blend-with options

			if (BlendWith != null)
			{
				var path = AssetDatabase.GetAssetPath(BlendWith);
				var tImp = (TextureImporter) AssetImporter.GetAtPath(path);
				if (tImp is null == false)
				{
					BlendWithReadable = tImp.isReadable;

					GUILayout.Space(4);
					EditorGUILayout.BeginHorizontal();
					GUILayout.Space(32);
					if (tImp.isReadable == false) GUI.backgroundColor = Color.green;
					if (GUILayout.Button("Switch readonly for '" + BlendWith.name + "' to " + (!tImp.isReadable)))
					{
						tImp.isReadable = !tImp.isReadable;
						AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
						BlendWithReadable = tImp.isReadable;
					}

					GUI.backgroundColor = Color.white;
					GUILayout.Space(32);
					EditorGUILayout.EndHorizontal();

					//GUILayout.Space( 5 );

					//EditorGUILayout.BeginHorizontal();
					//float highRange = BlendWithTiling > 4f ? 16f : 4.001f;
					//BlendWithTiling = EditorGUILayout.Slider( "Blend-With Tiling:", BlendWithTiling, 0.1f, highRange );
					//EditorGUILayout.EndHorizontal();

					var srcTex = GetFirstTexture;
					if (srcTex)
						if (BlendWith.width != srcTex.width)
							EditorGUILayout.HelpBox(
								"Texture sizes differs, preview can look different than final file result!",
								MessageType.None);
				}

				if (BlendWithReadable == false)
					EditorGUILayout.HelpBox("Texture must have 'Read/Write' enabled", MessageType.Warning);
			}
			else
			{
				BlendWithReadable = false;
			}

#endregion

			if (EditorGUI.EndChangeCheck())
				somethingChanged = true;
			else
				somethingChanged = false;
		}

		protected override void ProcessTexture(Texture2D source, Texture2D target, bool preview = true)
		{
			if (BlendWith == null) return;
			if (BlendWithReadable == false) return;

			if (!preview) EditorUtility.DisplayProgressBar("Blending Texture...", "Preparing... ", 2f / 5f);

#region Prepare Blend-With reference

			var sourcePixels = source.GetPixels32();
			var newPixels = source.GetPixels32();

			var targetWidth = Mathf.RoundToInt(source.width / BlendWithTiling);
			var targetHeight = Mathf.RoundToInt(source.height / BlendWithTiling);
			if (targetWidth < 1) targetWidth = 1;
			if (targetHeight < 1) targetHeight = 1;

			if (BlendWithMemo == null || BlendWithMemo.width != targetWidth || BlendWithMemo.height != targetHeight ||
				BlendWithMemo.name != BlendWith.name)
			{
				BlendWithMemo = FTextureEditorToolsMethods.GenerateScaledTexture2DReference(
					BlendWith, new Vector2(targetWidth, targetHeight), preview ? 2 : 4);
				BlendWithMemo.name = BlendWith.name;
			}

			var blendWithSource = BlendWithMemo;

			if (blendWithSource.width < 4)
			{
				if (!preview) EditorUtility.ClearProgressBar();
				return;
			}

			var blendWithPixels = blendWithSource.GetPixels32();

#endregion

			if (!preview)
				EditorUtility.DisplayProgressBar("Blending Texture...", "Blending... ", 3f / 5f);

			var srcDimensions = GetDimensions(source);

			if (Mode == EBlendMode.AlphaBlend) pixelOperation = AlphaBlend;
			else if (Mode == EBlendMode.Additive) pixelOperation = AdditiveBlend;
			else if (Mode == EBlendMode.Multiply) pixelOperation = MultiplyBlend;
			else if (Mode == EBlendMode.NormalMapBlending) pixelOperation = NormalBlend;

			for (var x = 0; x < source.width; x++)
			{
				for (var y = 0; y < source.height; y++)
				{
					var pxIndex = GetPX(x, y, srcDimensions);

					Color srcPixel = sourcePixels[pxIndex];
					Color blendWithColor =
						blendWithPixels[
							GetPXLoopSkipEdges(x, y, new Vector2(blendWithSource.width, blendWithSource.height))];

					Color tgtColor = pixelOperation.Invoke(srcPixel, blendWithColor);

					newPixels[pxIndex] = tgtColor;
				}
			}

			// Finalizing changes
			if (!preview)
				EditorUtility.DisplayProgressBar(
					"Equalizing Texture...", "Applying Equalization to Texture... ", 3.85f / 5f);

			target.SetPixels32(newPixels);
			target.Apply(false, false);

			if (!preview)
				EditorUtility.ClearProgressBar();
		}

		private Color32 AlphaBlend(Color32 source, Color32 target)
		{
			return Color32.Lerp(source, target, BlendValue);
		}

		private Color32 AdditiveBlend(Color32 source, Color32 target)
		{
			var newColor = new Color32();
			newColor.r = (byte) Mathf.Min(255, source.r + target.r);
			newColor.g = (byte) Mathf.Min(255, source.g + target.g);
			newColor.b = (byte) Mathf.Min(255, source.b + target.b);
			newColor.a = target.a;

			return Color32.Lerp(source, newColor, BlendValue);
		}

		private Color32 MultiplyBlend(Color32 source, Color32 target)
		{
			var srcCol = (Color) source;
			var newColor = (Color) target;

			newColor.r = Mathf.Min(1f, srcCol.r * newColor.r);
			newColor.g = Mathf.Min(1f, srcCol.g * newColor.g);
			newColor.b = Mathf.Min(1f, srcCol.b * newColor.b);

			return Color32.Lerp(source, newColor, BlendValue);
		}

		private Color32 NormalBlend(Color32 source, Color32 target)
		{
			return FTextureEditorToolsMethods.BlendNormals(source, target, BlendValue);
		}

		private enum EBlendMode
		{
			AlphaBlend,
			Additive,
			Multiply,
			NormalMapBlending
		}
	}
}
