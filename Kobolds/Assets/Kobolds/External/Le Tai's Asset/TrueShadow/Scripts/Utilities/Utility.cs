using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;


namespace LeTai
{
	public static class Utility
	{
		public static void LogList<T>(IEnumerable<T> list, Func<T, object> getData)
		{
			var sb = new StringBuilder();

			var i = 0;
			foreach (var el in list)
			{
				sb.Append(i + ":    ");
				sb.Append(getData(el).ToString());
				sb.Append("\n");
				i++;
			}

			Debug.Log(sb.ToString());
		}

		public static int SimplePingPong(int t, int max)
		{
			if (t > max)
				return 2 * max - t;
			return t;
		}

		public static void SafeDestroy(Object obj)
		{
			if (obj != null)
			{
				if (Application.isPlaying)
				{
					if (obj is GameObject go) go.transform.parent = null;

					Object.Destroy(obj);
				}
				else
				{
					Object.DestroyImmediate(obj);
				}
			}
		}
	}
}
