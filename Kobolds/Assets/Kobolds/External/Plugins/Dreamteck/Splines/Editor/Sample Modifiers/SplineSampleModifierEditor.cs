using UnityEditor;
using UnityEngine;

namespace Dreamteck.Splines.Editor
{
	public class SplineSampleModifierEditor : SplineUserSubEditor
	{
		protected SerializedProperty _blend;
		private int _deleteElement = -1;
		protected SerializedProperty _keys;
		protected SerializedProperty _modifier;

		protected SerializedObject _serializedObject;
		protected SerializedProperty _useClipped;
		protected bool drawAllKeys;
		protected int selected = -1;

		public SplineSampleModifierEditor(
			SplineUser user, SplineUserEditor editor, string modifierPropertyPath = "") : base(user, editor)
		{
			title = "Sample Modifier";
			_serializedObject = new SerializedObject(user);
			var propertyPath = modifierPropertyPath.Split('/');
			var property = _serializedObject.FindProperty(propertyPath[0]);
			for (var i = 1; i < propertyPath.Length; i++)
			{
				if (propertyPath[i].StartsWith("[") && propertyPath[i].EndsWith("]"))
				{
					var num = propertyPath[i].Substring(1, propertyPath[i].Length - 2);
					property = property.GetArrayElementAtIndex(int.Parse(num));
					continue;
				}

				property = property.FindPropertyRelative(propertyPath[i]);
			}

			_modifier = property;
			_keys = _modifier.FindPropertyRelative(_keysPropertyName);
			_blend = _modifier.FindPropertyRelative("blend");
			_useClipped = _modifier.FindPropertyRelative("useClippedPercent");
		}

		protected virtual SerializedProperty _keysProperty => _keys;
		protected virtual string _keysPropertyName => "keys";

		public override void DrawInspector()
		{
			base.DrawInspector();
			if (!isOpen) return;
			if (_keysProperty.arraySize > 0) drawAllKeys = EditorGUILayout.Toggle("Draw all Modules", drawAllKeys);
			_serializedObject.Update();

			EditorGUI.BeginChangeCheck();
			for (var i = 0; i < _keysProperty.arraySize; i++)
			{
				EditorGUILayout.BeginVertical(EditorStyles.helpBox);
				var keyProperty = _keysProperty.GetArrayElementAtIndex(i);
				if (selected == i)
				{
					EditorGUI.BeginChangeCheck();
					KeyGUI(keyProperty);
					if (EditorGUI.EndChangeCheck()) user.Rebuild();
				}
				else
				{
					var start = keyProperty.FindPropertyRelative("_featherStart");
					var end = keyProperty.FindPropertyRelative("_featherEnd");
					EditorGUILayout.LabelField(
						i + " [" + Mathf.Round(start.floatValue * 10) / 10f + " - " +
						Mathf.Round(end.floatValue * 10) / 10f + "]");
				}

				EditorGUILayout.EndVertical();
				var lastRect = GUILayoutUtility.GetLastRect();
				if (lastRect.Contains(Event.current.mousePosition))
					if (Event.current.type == EventType.MouseDown)
					{
						if (Event.current.button == 0)
						{
							selected = i;
							editor.Repaint();
						}
						else if (Event.current.button == 1)
						{
							var index = i;
							var menu = new GenericMenu();
							menu.AddItem(new GUIContent("Delete"), false, delegate { _deleteElement = index; });
							menu.ShowAsContext();
							UpdateValues();
							_serializedObject.Update();
						}
					}
			}

			EditorGUILayout.Space();
			if (_keysProperty.arraySize > 0)
			{
				EditorGUILayout.PropertyField(_blend);
				EditorGUILayout.PropertyField(
					_useClipped,
					new GUIContent(
						"Use Clipped Percents",
						"Whether the percentages relate to the clip range of the user or not."));
			}

			if (_deleteElement >= 0)
			{
				_keysProperty.DeleteArrayElementAtIndex(_deleteElement);
				_deleteElement = -1;
				UpdateValues();
			}
			else
			{
				if (EditorGUI.EndChangeCheck()) UpdateValues();
			}
		}

		public override void DrawScene()
		{
			base.DrawScene();
			_serializedObject.Update();
			var changed = false;
			for (var i = 0; i < _keysProperty.arraySize; i++)
				if (selected == i || drawAllKeys)
					if (KeyHandles(_keysProperty.GetArrayElementAtIndex(i), selected == i))
						changed = true;

			if (changed) UpdateValues();
		}

		protected void UpdateValues()
		{
			if (_serializedObject != null) _serializedObject.ApplyModifiedProperties();
			user.Rebuild();
			editor.Repaint();
		}

		protected virtual SerializedProperty AddKey(float f, float t)
		{
			_keys.InsertArrayElementAtIndex(_keys.arraySize);
			var key = _keys.GetArrayElementAtIndex(_keys.arraySize - 1);
			var start = key.FindPropertyRelative("_featherStart");
			var end = key.FindPropertyRelative("_featherEnd");
			var centerStart = key.FindPropertyRelative("_centerStart");
			var centerEnd = key.FindPropertyRelative("_centerEnd");
			var blend = key.FindPropertyRelative("blend");
			var interpolation = key.FindPropertyRelative("interpolation");

			start.floatValue = Mathf.Clamp01(f);
			end.floatValue = Mathf.Clamp01(t);
			blend.floatValue = 1f;
			interpolation.animationCurveValue = AnimationCurve.Linear(0f, 0f, 1f, 1f);
			centerStart.floatValue = 0.1f;
			centerEnd.floatValue = 0.9f;
			return key;
		}

		protected virtual void KeyGUI(SerializedProperty keyProperty)
		{
			var start = keyProperty.FindPropertyRelative("_featherStart");
			var end = keyProperty.FindPropertyRelative("_featherEnd");
			var centerStart = keyProperty.FindPropertyRelative("_centerStart");
			var centerEnd = keyProperty.FindPropertyRelative("_centerEnd");
			var interpolation = keyProperty.FindPropertyRelative("interpolation");
			var blend = keyProperty.FindPropertyRelative("blend");

			EditorGUILayout.BeginHorizontal();
			EditorGUIUtility.labelWidth = 50f;
			start.floatValue = EditorGUILayout.Slider("Start", start.floatValue, 0f, 1f);
			end.floatValue = EditorGUILayout.Slider("End", end.floatValue, 0f, 1f);
			EditorGUILayout.EndHorizontal();
			EditorGUIUtility.labelWidth = 0f;
			var cs = centerStart.floatValue;
			var ce = centerEnd.floatValue;
			EditorGUILayout.MinMaxSlider("Center", ref cs, ref ce, 0f, 1f);
			centerStart.floatValue = cs;
			centerEnd.floatValue = ce;
			EditorGUILayout.PropertyField(interpolation);
			EditorGUILayout.PropertyField(blend);
		}

		protected static float GlobalToLocalPercent(float start, float end, float t)
		{
			if (start > end)
			{
				if (t > start) return Mathf.InverseLerp(start, start + (1f - start) + end, t);
				if (t < end) return Mathf.InverseLerp(-(1f - start), end, t);
				return 0f;
			}

			return Mathf.InverseLerp(start, end, t);
		}

		protected static float LocalToGlobalPercent(float start, float end, float t)
		{
			if (start > end)
			{
				t = Mathf.Lerp(start, start + (1f - start) + end, t);
				if (t > 1f) t -= Mathf.Floor(t);
				return t;
			}

			return Mathf.Lerp(start, end, t);
		}

		protected static float GetPosition(float start, float end, float centerStart, float centerEnd)
		{
			var center = Mathf.Lerp(centerStart, centerEnd, 0.5f);
			if (start > end)
			{
				var fromToEndDistance = 1f - start;
				var centerDistance = center * (fromToEndDistance + end);
				var pos = start + centerDistance;
				if (pos > 1f) pos -= Mathf.Floor(pos);
				return pos;
			}

			return Mathf.Lerp(start, end, center);
		}

		protected virtual bool KeyHandles(SerializedProperty key, bool edit)
		{
			if (!isOpen) return false;
			var useClip = _useClipped.boolValue;

			var start = key.FindPropertyRelative("_featherStart");
			var end = key.FindPropertyRelative("_featherEnd");
			var centerStart = key.FindPropertyRelative("_centerStart");
			var centerEnd = key.FindPropertyRelative("_centerEnd");

			var changed = false;
			double value = start.floatValue;

			if (useClip) user.UnclipPercent(ref value);
			SplineComputerEditorHandles.Slider(
				user.spline, ref value, user.spline.editorPathColor, "Start",
				SplineComputerEditorHandles.SplineSliderGizmo.ForwardTriangle, 0.8f);
			if (useClip) user.ClipPercent(ref value);

			if (start.floatValue != value)
			{
				MainPointModule.HoldInteraction();
				start.floatValue = (float) value;
				changed = true;
			}

			value = LocalToGlobalPercent(start.floatValue, end.floatValue, centerStart.floatValue);
			if (useClip) user.UnclipPercent(ref value);
			SplineComputerEditorHandles.Slider(
				user.spline, ref value, user.spline.editorPathColor, "",
				SplineComputerEditorHandles.SplineSliderGizmo.Rectangle, 0.6f);
			if (useClip) user.ClipPercent(ref value);

			if (LocalToGlobalPercent(start.floatValue, end.floatValue, centerStart.floatValue) != value)
			{
				MainPointModule.HoldInteraction();
				centerStart.floatValue = GlobalToLocalPercent(start.floatValue, end.floatValue, (float) value);
				changed = true;
			}

			value = LocalToGlobalPercent(start.floatValue, end.floatValue, centerEnd.floatValue);
			if (useClip) user.UnclipPercent(ref value);


			SplineComputerEditorHandles.Slider(
				user.spline, ref value, user.spline.editorPathColor, "",
				SplineComputerEditorHandles.SplineSliderGizmo.Rectangle, 0.6f);
			if (useClip) user.ClipPercent(ref value);
			if (LocalToGlobalPercent(start.floatValue, end.floatValue, centerEnd.floatValue) != value)
			{
				MainPointModule.HoldInteraction();
				centerEnd.floatValue = GlobalToLocalPercent(start.floatValue, end.floatValue, (float) value);
				changed = true;
			}


			value = end.floatValue;
			if (useClip) user.UnclipPercent(ref value);

			SplineComputerEditorHandles.Slider(
				user.spline, ref value, user.spline.editorPathColor, "End",
				SplineComputerEditorHandles.SplineSliderGizmo.BackwardTriangle, 0.8f);
			if (useClip) user.ClipPercent(ref value);
			if (end.floatValue != value)
			{
				MainPointModule.HoldInteraction();
				end.floatValue = (float) value;
				changed = true;
			}


			if (Event.current.control)
			{
				value = GetPosition(start.floatValue, end.floatValue, centerStart.floatValue, centerEnd.floatValue);
				var lastValue = value;
				if (useClip) user.UnclipPercent(ref value);
				SplineComputerEditorHandles.Slider(
					user.spline, ref value, user.spline.editorPathColor, "",
					SplineComputerEditorHandles.SplineSliderGizmo.Circle, 0.4f);

				if (useClip) user.ClipPercent(ref value);

				if (value != lastValue)
				{
					MainPointModule.HoldInteraction();
					var delta = value - lastValue;
					start.floatValue += (float) delta;
					end.floatValue += (float) delta;
					start.floatValue = Mathf.Clamp01(start.floatValue);
					end.floatValue = Mathf.Clamp(end.floatValue, start.floatValue, 1f);
					changed = true;
				}
			}

			return changed;
		}
	}
}
