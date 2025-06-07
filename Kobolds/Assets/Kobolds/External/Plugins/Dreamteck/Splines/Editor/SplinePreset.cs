using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Dreamteck.Splines.Editor
{
	[Serializable]
	public struct S_Vector3
	{
		public float x, y, z;

		public Vector3 vector
		{
			get => new(x, y, z);
			set { }
		}


		public S_Vector3(Vector3 input)
		{
			x = input.x;
			y = input.y;
			z = input.z;
		}
	}

	[Serializable]
	public struct S_Color
	{
		public float r, g, b, a;

		public Color color
		{
			get => new(r, g, b, a);
			set { }
		}

		public S_Color(Color input)
		{
			r = input.r;
			g = input.g;
			b = input.b;
			a = input.a;
		}
	}

	[Serializable]
	public class SplinePreset
	{
		private static string path = "";

		[SerializeField]
		private S_Vector3[] points_position = new S_Vector3[0];

		[SerializeField]
		private S_Vector3[] points_tanget = new S_Vector3[0];

		[SerializeField]
		private S_Vector3[] points_tangent2 = new S_Vector3[0];

		[SerializeField]
		private S_Vector3[] points_normal = new S_Vector3[0];

		[SerializeField]
		private S_Color[] points_color = new S_Color[0];

		[SerializeField]
		private float[] points_size = new float[0];

		[SerializeField]
		private SplinePoint.Type[] points_type = new SplinePoint.Type[0];

		public bool isClosed;
		public string filename = "";
		public string name = "";
		public string description = "";
		public Spline.Type type = Spline.Type.Bezier;


		[NonSerialized]
		protected SplineComputer computer;

		[NonSerialized]
		public Vector3 origin = Vector3.zero;

		public SplinePreset(SerializedSplinePoint[] p, bool closed, Spline.Type t)
		{
			points_position = new S_Vector3[p.Length];
			points_tanget = new S_Vector3[p.Length];
			points_tangent2 = new S_Vector3[p.Length];
			points_normal = new S_Vector3[p.Length];
			points_color = new S_Color[p.Length];
			points_size = new float[p.Length];
			points_type = new SplinePoint.Type[p.Length];
			for (var i = 0; i < p.Length; i++)
			{
				points_position[i] = new S_Vector3(p[i].position);
				points_tanget[i] = new S_Vector3(p[i].tangent);
				points_tangent2[i] = new S_Vector3(p[i].tangent2);
				points_normal[i] = new S_Vector3(p[i].normal);
				points_color[i] = new S_Color(p[i].color);
				points_size[i] = p[i].size;
				points_type[i] = p[i].type;
			}

			isClosed = closed;
			type = t;
			path = ResourceUtility.FindFolder(Application.dataPath, "Dreamteck/Splines/Presets");
		}

		public SplinePoint[] points
		{
			get
			{
				var p = new SplinePoint[points_position.Length];
				for (var i = 0; i < p.Length; i++)
				{
					p[i].type = points_type[i];
					p[i].position = points_position[i].vector;
					p[i].tangent = points_tanget[i].vector;
					p[i].tangent2 = points_tangent2[i].vector;
					p[i].normal = points_normal[i].vector;
					p[i].color = points_color[i].color;
					p[i].size = points_size[i];
				}

				return p;
			}
		}

		public void Save(string name)
		{
			if (!Directory.Exists(path)) Directory.CreateDirectory(path);
			var file = File.Create(path + "/" + name + ".jsp");
			var bytes = ASCIIEncoding.ASCII.GetBytes(JsonUtility.ToJson(this));
			file.Write(bytes, 0, bytes.Length);
			file.Close();
		}

		public static void Delete(string filename)
		{
			path = ResourceUtility.FindFolder(Application.dataPath, "Dreamteck/Splines/Presets");
			if (!Directory.Exists(path))
			{
				Debug.LogError("Directory " + path + " does not exist");
				return;
			}

			File.Delete(path + "/" + filename);
		}

		public static SplinePreset[] LoadAll()
		{
			path = ResourceUtility.FindFolder(Application.dataPath, "Dreamteck/Splines/Presets");
			if (!Directory.Exists(path))
			{
				Debug.LogError("Directory " + path + " does not exist");
				return null;
			}

			var files = Directory.GetFiles(path, "*.jsp");
			var presets = new SplinePreset[files.Length];
			for (var i = 0; i < files.Length; i++)
			{
				var file = File.Open(files[i], FileMode.Open);
				var bytes = new byte[file.Length];
				file.Read(bytes, 0, bytes.Length);
				var json = ASCIIEncoding.ASCII.GetString(bytes);
				presets[i] = JsonUtility.FromJson<SplinePreset>(json);
				file.Close();
			}

			return presets;
		}
	}
}
