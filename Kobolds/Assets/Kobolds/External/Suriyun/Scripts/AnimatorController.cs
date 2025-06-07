using System;
using UnityEngine;

namespace Suriyun
{
	public class AnimatorController : MonoBehaviour
	{
		public Animator[] animators;

		public void SwapVisibility(GameObject obj)
		{
			obj.SetActive(!obj.activeSelf);
		}


		public void SetFloat(string parameter = "key,value")
		{
			char[] separator = {',', ';'};
			var param = parameter.Split(separator);

			var name = param[0];
			var value = (float) Convert.ToDouble(param[1]);

			Debug.Log(name + " " + value);

			foreach (var a in animators) a.SetFloat(name, value);
		}

		public void SetInt(string parameter = "key,value")
		{
			char[] separator = {',', ';'};
			var param = parameter.Split(separator);

			var name = param[0];
			var value = Convert.ToInt32(param[1]);

			Debug.Log(name + " " + value);

			foreach (var a in animators) a.SetInteger(name, value);
		}

		public void SetBool(string parameter = "key,value")
		{
			char[] separator = {',', ';'};
			var param = parameter.Split(separator);

			var name = param[0];
			var value = Convert.ToBoolean(param[1]);

			Debug.Log(name + " " + value);

			foreach (var a in animators) a.SetBool(name, value);
		}

		public void SetTrigger(string parameter = "key,value")
		{
			char[] separator = {',', ';'};
			var param = parameter.Split(separator);

			var name = param[0];

			Debug.Log(name);

			foreach (var a in animators) a.SetTrigger(name);
		}
	}
}
