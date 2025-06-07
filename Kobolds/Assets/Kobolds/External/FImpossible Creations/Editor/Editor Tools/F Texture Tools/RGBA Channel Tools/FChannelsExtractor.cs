using System;
using System.Collections.Generic;
using FIMSpace.FTextureTools;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FIMSpace.FEditor
{
	public class FChannelsExtractor : EditorWindow
	{
		private bool extractA = true;
		private bool extractB = true;
		private bool extractG = true;

		private EExtractType ExtractionType = EExtractType.ExtractAsBlackAndWhite;

		private bool extractR = true;
		private List<Texture2D> textures;


		private void OnGUI()
		{
			if (textures == null) textures = new List<Texture2D>();
			textures.Clear();

			if (Selection.objects.Length > 0)
				for (var i = 0; i < Selection.objects.Length; i++)
				{
					var texture =
						AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GetAssetPath(Selection.objects[i]));
					if (texture != null) textures.Add(texture);
				}

			if (textures.Count == 0)
			{
				EditorGUILayout.HelpBox("You must select at least one texture file!", MessageType.Warning);
				return;
			}

			EditorGUILayout.LabelField("Textures to extract channels from: " + textures.Count);
			GUILayout.Space(4);

			if (textures.Count > 0 && textures[0] != null)
			{
				EditorGUILayout.BeginHorizontal();

				var texRect = GUILayoutUtility.GetRect(40, 40);
				var off = 0f;
				var toggleRect = texRect;
				toggleRect.x -= off;
				EditorGUI.DrawPreviewTexture(
					texRect, textures[0], null, ScaleMode.ScaleToFit, 1f, 0, ColorWriteMask.Red);
				extractR = EditorGUI.Toggle(toggleRect, extractR);
				texRect = GUILayoutUtility.GetRect(40, 40);
				toggleRect = texRect;
				toggleRect.x -= off;
				EditorGUI.DrawPreviewTexture(
					texRect, textures[0], null, ScaleMode.ScaleToFit, 1f, 0, ColorWriteMask.Green);
				extractG = EditorGUI.Toggle(toggleRect, extractG);
				texRect = GUILayoutUtility.GetRect(40, 40);
				toggleRect = texRect;
				toggleRect.x -= off;
				EditorGUI.DrawPreviewTexture(
					texRect, textures[0], null, ScaleMode.ScaleToFit, 1f, 0, ColorWriteMask.Blue);
				extractB = EditorGUI.Toggle(toggleRect, extractB);
				texRect = GUILayoutUtility.GetRect(40, 40);
				toggleRect = texRect;
				toggleRect.x -= off;
				EditorGUI.DrawPreviewTexture(
					texRect, textures[0], null, ScaleMode.ScaleToFit, 1f, 0, ColorWriteMask.Alpha);
				extractA = EditorGUI.Toggle(toggleRect, extractA);

				EditorGUILayout.EndHorizontal();
				GUILayout.Space(4);
			}

			EditorGUIUtility.labelWidth = 100;
			ExtractionType = (EExtractType) EditorGUILayout.EnumPopup("Extract Type:", ExtractionType);
			EditorGUIUtility.labelWidth = 0;

			FTextureToolsGUIUtilities.DrawUILine(Color.white * 0.35f, 2, 5);

			if (GUILayout.Button("Extract RGBA Channels (" + textures.Count + ")"))
				try
				{
					for (var i = 0; i < textures.Count; i++)
					{
						EditorUtility.DisplayProgressBar(
							"Extracting textures...", "Extracting texture " + textures[i].name,
							i / (float) textures.Count);
						var srcTexture = textures[i];

						var backup = FTextureEditorToolsMethods.StartEditingTextureAsset(textures[i]);

						var srcPixels = srcTexture.GetPixels();
						Color[] newPixels;

						if (extractR)
						{
							newPixels = new Color[srcPixels.Length];
							SetChannelColor(srcPixels, newPixels, EColorChannel.R);
							FTextureEditorToolsMethods.GenerateCopyWithNewPixels(srcTexture, newPixels, "-R", false);
						}

						if (extractG)
						{
							newPixels = new Color[srcPixels.Length];
							SetChannelColor(srcPixels, newPixels, EColorChannel.G);
							FTextureEditorToolsMethods.GenerateCopyWithNewPixels(srcTexture, newPixels, "-G", false);
						}

						if (extractB)
						{
							newPixels = new Color[srcPixels.Length];
							SetChannelColor(srcPixels, newPixels, EColorChannel.B);
							FTextureEditorToolsMethods.GenerateCopyWithNewPixels(srcTexture, newPixels, "-B", false);
						}

						if (extractA)
						{
							newPixels = new Color[srcPixels.Length];
							SetChannelColor(srcPixels, newPixels, EColorChannel.A);
							FTextureEditorToolsMethods.GenerateCopyWithNewPixels(srcTexture, newPixels, "-A");
						}

						FTextureEditorToolsMethods.EndEditingTextureAsset(textures[i], backup);
					}

					EditorUtility.ClearProgressBar();
				}
				catch (Exception exc)
				{
					EditorUtility.ClearProgressBar();
					Debug.LogError("[Fimpo Image Tools] Something went wrong when extracting textures! " + exc);
				}
		}

		public static void Init()
		{
			var window = (FChannelsExtractor) GetWindow(typeof(FChannelsExtractor));

			window.minSize = new Vector2(320f, 125f);
			window.maxSize = window.minSize;

			window.titleContent = new GUIContent(
				"Extract Channels", FTextureToolsGUIUtilities.FindIcon("SPR_Channels"));
			window.position = new Rect(200, 100, 320, 125);
			window.Show();
		}


		private void SetChannelColor(Color[] srcPix, Color[] newPix, EColorChannel channel)
		{
			if (ExtractionType == EExtractType.ExtractAsBlackAndWhite)
			{
				if (channel == EColorChannel.R)
					for (var i = 0; i < srcPix.Length; i++)
						newPix[i] = new Color(srcPix[i].r, srcPix[i].r, srcPix[i].r, 1f);
				else if (channel == EColorChannel.G)
					for (var i = 0; i < srcPix.Length; i++)
						newPix[i] = new Color(srcPix[i].g, srcPix[i].g, srcPix[i].g, 1f);
				else if (channel == EColorChannel.B)
					for (var i = 0; i < srcPix.Length; i++)
						newPix[i] = new Color(srcPix[i].b, srcPix[i].b, srcPix[i].b, 1f);
				else if (channel == EColorChannel.A)
					for (var i = 0; i < srcPix.Length; i++)
						newPix[i] = new Color(srcPix[i].a, srcPix[i].a, srcPix[i].a, 1f);
			}
			else if (ExtractionType == EExtractType.ExtractOnlyChannelColor)
			{
				if (channel == EColorChannel.R)
					for (var i = 0; i < srcPix.Length; i++)
						newPix[i] = new Color(srcPix[i].r, 0f, 0f, 1f);
				else if (channel == EColorChannel.G)
					for (var i = 0; i < srcPix.Length; i++)
						newPix[i] = new Color(0f, srcPix[i].g, 0f, 1f);
				else if (channel == EColorChannel.B)
					for (var i = 0; i < srcPix.Length; i++)
						newPix[i] = new Color(0f, 0f, srcPix[i].b, 1f);
				else if (channel == EColorChannel.A)
					for (var i = 0; i < srcPix.Length; i++)
						newPix[i] = new Color(1f, 1f, 1f, srcPix[i].a);
			}
			else if (ExtractionType == EExtractType.OnlyChannelColorWithTransparency)
			{
				if (channel == EColorChannel.R)
					for (var i = 0; i < srcPix.Length; i++)
						newPix[i] = new Color(srcPix[i].r, 0f, 0f, srcPix[i].r);
				else if (channel == EColorChannel.G)
					for (var i = 0; i < srcPix.Length; i++)
						newPix[i] = new Color(0f, srcPix[i].g, 0f, srcPix[i].g);
				else if (channel == EColorChannel.B)
					for (var i = 0; i < srcPix.Length; i++)
						newPix[i] = new Color(0f, 0f, srcPix[i].b, srcPix[i].b);
				else if (channel == EColorChannel.A)
					for (var i = 0; i < srcPix.Length; i++)
						newPix[i] = new Color(1f, 1f, 1f, srcPix[i].a);
			}
		}

		private enum EExtractType
		{
			ExtractAsBlackAndWhite,
			ExtractOnlyChannelColor,
			OnlyChannelColorWithTransparency
		}

		private enum EColorChannel
		{
			R,
			G,
			B,
			A
		}
	}
}
