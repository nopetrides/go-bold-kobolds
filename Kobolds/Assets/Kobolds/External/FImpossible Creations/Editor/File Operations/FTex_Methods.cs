using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.FTex
{
	public enum FETextureExtension
	{
		UNSUPPORTED,
		JPG,
		PNG,
		TGA,
		TIFF,
		EXR
	}

	public static class FTex_Methods
	{
		public static FETextureExtension GetFileExtension(string path)
		{
			var extension = Path.GetExtension(path);

			if (extension.ToLower().Contains("png")) return FETextureExtension.PNG;
			if (extension.ToLower().Contains("jpg") || extension.ToLower().Contains("jpeg"))
				return FETextureExtension.JPG;
			if (extension.ToLower().Contains("tga")) return FETextureExtension.TGA;
			if (extension.ToLower().Contains("tif")) return FETextureExtension.TIFF;
			if (extension.ToLower().Contains("exr")) return FETextureExtension.EXR;

			return FETextureExtension.UNSUPPORTED;
		}

		public static int FindNearestPowOf2(int val)
		{
			return Mathf.ClosestPowerOfTwo(val);
		}

		public static int FindHigherPowOf2(int val)
		{
			return Mathf.NextPowerOfTwo(val + 1);
		}

		public static int FindLowerPowOf2(int val)
		{
			return Mathf.NextPowerOfTwo((val - 1) / 2);
		}


		public static Color32[] GetPixelsFrom(Texture2D source)
		{
			Color32[] newPixels = null;

#if UNITY_EDITOR
			var sPath = AssetDatabase.GetAssetPath(source);
			var sourceTex = (TextureImporter) AssetImporter.GetAtPath(sPath);

			if (sourceTex != null)
			{
				var swasReadable = sourceTex.isReadable;
				sourceTex.isReadable = true;
				sourceTex.SaveAndReimport();

				newPixels = source.GetPixels32();

				sourceTex.isReadable = swasReadable;
				sourceTex.SaveAndReimport();
			}
#endif

			return newPixels;
		}
	}
}
