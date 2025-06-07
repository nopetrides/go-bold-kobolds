using System.Linq;
using UnityEngine;

namespace Dreamteck
{
	public class Singleton<T> : PrivateSingleton<T> where T : Component
	{
		public static T instance
		{
			get
			{
				if (_instance == null) _instance = FindObjectsOfType<T>().FirstOrDefault();

				return _instance;
			}
		}
	}
}
