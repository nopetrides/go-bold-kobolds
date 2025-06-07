using System;
using UnityEngine;
#if UNITY_EDITOR_WIN
#if FICONS_ENCODERSIMPORTED
//using System.Windows.Media.Imaging;
//using System.Windows.Controls;
#endif

#endif

namespace FIMSpace.FTex
{
	public static class FTex_AdditionalEncoders
	{
		public enum FEColorChannel
		{
			R,
			G,
			B,
			A,
			White,
			Black,
			Gray
		}

		public static byte[] EncodeToTGA(Texture2D texture, FEColorChannel[] colorChannels = null)
		{
			return TGAEncoder.EncodeToTGA(texture, colorChannels);
		}

		public static byte[] EncodeToTIFF(Texture2D texture)
		{
			return TIFFEncoder.EncodeToTIFF(texture);
		}

#region Encoders

		// TIFF Encoder ------------------------------------------------------
		private static class TIFFEncoder
		{
			public static byte[] EncodeToTIFF(Texture2D texture)
			{
#if UNITY_EDITOR_WIN
#if FICONS_ENCODERSIMPORTED
                //string assetPath = AssetDatabase.GetAssetPath(texture);
                //string fullPath = Application.dataPath.Replace("Assets", "") + assetPath;
                
                //int width = texture.width;
                //int height = texture.height;
                //int stride = width / 8;
                //byte[] pixels = new byte[height * stride];
                
                //List<System.Windows.Media.Color> colors = new List<System.Windows.Media.Color>();
                //Color32[] texPixels = texture.GetPixels32();

                //for (int i = 0; i < texPixels.Length; i++)
                //{
                //    colors.Add(System.Windows.Media.Color.FromArgb(texPixels[i].a, texPixels[i].r, texPixels[i].g, texPixels[i].b));
                //}

                //BitmapPalette myPalette = BitmapPalettes.WebPaletteTransparent;

                //BitmapSource image = BitmapSource.Create(
                //    width,
                //    height,
                //    96,
                //    96,
                //    System.Windows.Media.PixelFormats.Indexed1,
                //    myPalette,
                //    pixels,
                //    stride);

                //var stream = new FileStream(fullPath, FileMode.Append);
                //var encoder = new TiffBitmapEncoder();
                //encoder.Compression = TiffCompressOption.None;
                //encoder.Frames.Add(BitmapFrame.Create(image));
                //encoder.Save(stream);

                Debug.LogError("[FIMPOSSIBLE TOOLS] TIFFs files encoding is not supported yet!");
                return null;

#endif
#endif

				Debug.LogError("[FIMPOSSIBLE TOOLS] TIFFs files encoding is not supported yet!");
				//Debug.LogError("[ICONS SCALER EDITOR] TIFFs files needs plugin to work, go to 'Icons Scaler - Readme.txt' you will have to move few .dll files from '...Program Files/Reference Assemblies...' to your project's plugins folder (only windows)");
				return null;
			}
		}


		// TGA Encoder -------------------------------------------------------
		private static class TGAEncoder
		{
			private static readonly byte[] TGA_FOOTER =
			{
				0, 0, 0, 0,
				0, 0, 0, 0,
				(byte) 'T', (byte) 'R', (byte) 'U', (byte) 'E',
				(byte) 'V', (byte) 'I', (byte) 'S', (byte) 'I', (byte) 'O', (byte) 'N',
				(byte) '-', (byte) 'X', (byte) 'F', (byte) 'I', (byte) 'L', (byte) 'E', (byte) '.', 0
			};

			public static byte[] EncodeToTGA(Texture2D texture, FEColorChannel[] colorChannels = null)
			{
				if (colorChannels == null)
					colorChannels = new[] {FEColorChannel.R, FEColorChannel.G, FEColorChannel.B, FEColorChannel.A};

				var channelsCount = colorChannels.Length;

				if (channelsCount != 3 && channelsCount != 4)
				{
					Debug.LogError("[FIMPOSSIBLE TOOLS] TGA can be saved only with 3 or 4 channels!");
					return null;
				}

				var header = CreateTGAHeader(texture.width, texture.height, channelsCount == 4);

				var pixels = texture.GetPixels32();
				var newBytes = new byte[header.Length + TGA_FOOTER.Length + pixels.Length * channelsCount];

				var b = header.Length;

				// Applying pixel format
				if (channelsCount == 4)
					for (var p = 0; p < pixels.Length; p++)
					{
						var pixel = pixels[p];

						newBytes[b + 0] = GetChannel(pixel, colorChannels[2]);
						newBytes[b + 1] = GetChannel(pixel, colorChannels[1]);
						newBytes[b + 2] = GetChannel(pixel, colorChannels[0]);
						newBytes[b + 3] = GetChannel(pixel, colorChannels[3]);
						b += channelsCount;
					}
				else
					for (var p = 0; p < pixels.Length; p++)
					{
						var pixel = pixels[p];

						newBytes[b + 0] = GetChannel(pixel, colorChannels[2]);
						newBytes[b + 1] = GetChannel(pixel, colorChannels[1]);
						newBytes[b + 2] = GetChannel(pixel, colorChannels[0]);
						b += channelsCount;
					}


				Array.ConstrainedCopy(header, 0, newBytes, 0, header.Length);
				Array.ConstrainedCopy(TGA_FOOTER, 0, newBytes, b, TGA_FOOTER.Length);

				return newBytes;
			}


			private static byte[] CreateTGAHeader(int width, int height, bool fourChannels = true)
			{
				return new byte[]
				{
					0, 0, 2,
					0, 0, 0, 0,
					0,
					0, 0, 0, 0,
					(byte) (width & 0x00FF),
					(byte) ((width & 0xFF00) >> 8),
					(byte) (height & 0x00FF),
					(byte) ((height & 0xFF00) >> 8),
					(byte) (fourChannels ? 32 : 24),
					0
				};
			}


			private static byte GetChannel(Color32 color, FEColorChannel colorChannel)
			{
				switch (colorChannel)
				{
					case FEColorChannel.R: return color.r;
					case FEColorChannel.G: return color.g;
					case FEColorChannel.B: return color.b;
					case FEColorChannel.A: return color.a;
					case FEColorChannel.Black: return 0;
					case FEColorChannel.Gray: return 127;
					default:
					case FEColorChannel.White: return 255;
				}
			}
		}

#endregion
	}
}
