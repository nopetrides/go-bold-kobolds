using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LeTai.TrueShadow
{
	internal interface IChangeTracker
	{
		void Check();
	}

	internal class ChangeTracker<T> : IChangeTracker
	{
		private readonly Func<T, T, bool> compare;
		private readonly Func<T> getValue;
		private readonly Func<T, T> onChange;
		private T previousValue;

		public ChangeTracker(
			Func<T> getValue,
			Func<T, T> onChange,
			Func<T, T, bool> compare = null
		)
		{
			this.getValue = getValue;
			this.onChange = onChange;
			this.compare = compare ?? EqualityComparer<T>.Default.Equals;

			previousValue = this.getValue();
		}

		public void Check()
		{
			var newValue = getValue();
			if (!compare(previousValue, newValue)) previousValue = onChange(newValue);
		}

		public void Forget()
		{
			previousValue = getValue();
		}
	}

	public partial class TrueShadow
	{
		private Action checkHierarchyDirtiedDelegate;
		private ChangeTracker<int>[] hierarchyTrackers;
		private IChangeTracker[] transformTrackers;


		protected override void OnDidApplyAnimationProperties()
		{
			if (!isActiveAndEnabled) return;

			SetLayoutTextureDirty();
		}

		protected override void OnRectTransformDimensionsChange()
		{
			base.OnRectTransformDimensionsChange();

			if (!isActiveAndEnabled) return;

			SetLayoutTextureDirty();
		}

		protected override void OnTransformParentChanged()
		{
			base.OnTransformParentChanged();

			if (!isActiveAndEnabled) return;

			SetHierachyDirty();
			this.NextFrames(checkHierarchyDirtiedDelegate);
		}

		public void ModifyMesh(Mesh mesh)
		{
			if (!isActiveAndEnabled) return;

			if (SpriteMesh) Utility.SafeDestroy(SpriteMesh);
			SpriteMesh = Instantiate(mesh);

			SetLayoutTextureDirty();
		}

		public void ModifyMesh(VertexHelper verts)
		{
			if (!isActiveAndEnabled) return;

			// For when pressing play while in prefab mode
			if (!SpriteMesh) SpriteMesh = new Mesh();
			verts.FillMesh(SpriteMesh);

			SetLayoutTextureDirty();
		}

		private void InitInvalidator()
		{
			checkHierarchyDirtiedDelegate = CheckHierarchyDirtied;
			hierarchyTrackers = new[]
			{
				new ChangeTracker<int>(
					() => RectTransform.GetSiblingIndex(),
					newValue =>
					{
						SetHierachyDirty();
						return newValue; // + 1;
					}
				),
				new ChangeTracker<int>(
					() =>
					{
						if (shadowRenderer)
							return shadowRenderer.transform.GetSiblingIndex();
						return -1;
					},
					newValue =>
					{
						SetHierachyDirty();
						return newValue; // + 1;
					}
				)
			};

			transformTrackers = new IChangeTracker[]
			{
				new ChangeTracker<Vector3>(
					() => RectTransform.position,
					newValue =>
					{
						SetLayoutDirty();
						return newValue;
					},
					(prev, curr) => prev == curr
				),
				new ChangeTracker<Quaternion>(
					() => RectTransform.rotation,
					newValue =>
					{
						SetLayoutDirty();
						if (Cutout)
							SetTextureDirty();
						return newValue;
					},
					(prev, curr) => prev == curr
				),
				new ChangeTracker<Color>(
					() => CanvasRenderer.GetColor(),
					newValue =>
					{
						SetLayoutDirty();
						return newValue;
					},
					(prev, curr) => prev == curr
				)
			};

#if TMP_PRESENT
			if (Graphic is TextMeshProUGUI
				|| Graphic is TMP_SubMeshUI)
			{
				var old = transformTrackers;
				transformTrackers = new IChangeTracker[old.Length + 1];
				Array.Copy(old, transformTrackers, old.Length);

				transformTrackers[old.Length] = new ChangeTracker<Vector3>(
					() => RectTransform.lossyScale,
					newValue =>
					{
						SetLayoutTextureDirty();
						return newValue;
					},
					(prev, curr) =>
					{
						if (prev == curr) // Early exit for most common path
							return true;

						if (prev.x * prev.y * prev.z < 1e-9f
							&& curr.x * curr.y * curr.z > 1e-9f)
							return false;

						var diff = curr - prev;
						return Mathf.Abs(diff.x / prev.x) < .25f
								&& Mathf.Abs(diff.y / prev.y) < .25f
								&& Mathf.Abs(diff.z / prev.z) < .25f;
					}
				);
			}
#endif

			Graphic.RegisterDirtyLayoutCallback(SetLayoutTextureDirty);
			Graphic.RegisterDirtyVerticesCallback(SetLayoutTextureDirty);
			Graphic.RegisterDirtyMaterialCallback(OnGraphicMaterialDirty);

			CheckHierarchyDirtied();
			CheckTransformDirtied();
		}

		private void TerminateInvalidator()
		{
			if (Graphic)
			{
				Graphic.UnregisterDirtyLayoutCallback(SetLayoutTextureDirty);
				Graphic.UnregisterDirtyVerticesCallback(SetLayoutTextureDirty);
				Graphic.UnregisterDirtyMaterialCallback(OnGraphicMaterialDirty);
			}
		}

		private void OnGraphicMaterialDirty()
		{
			SetLayoutTextureDirty();
			shadowRenderer.UpdateMaterial();
		}

		internal void CheckTransformDirtied()
		{
			if (transformTrackers != null)
				for (var i = 0; i < transformTrackers.Length; i++)
					transformTrackers[i].Check();
		}

		internal void CheckHierarchyDirtied()
		{
			if (ShadowAsSibling && hierarchyTrackers != null)
				for (var i = 0; i < hierarchyTrackers.Length; i++)
					hierarchyTrackers[i].Check();
		}

		internal void ForgetSiblingIndexChanges()
		{
			for (var i = 0; i < hierarchyTrackers.Length; i++) hierarchyTrackers[i].Forget();
		}

		private void SetLayoutTextureDirty()
		{
#if TMP_PRESENT
			if (Graphic is TextMeshProUGUI tmp)
			{
				SpriteMesh = string.IsNullOrEmpty(tmp.text) ? null : tmp.mesh;
			}
			else if (Graphic is TMP_SubMeshUI stmp)
			{
				var isEmpty = string.IsNullOrEmpty(stmp.textComponent.text);
#if UNITY_2022_2 || UNITY_2023_2_OR_NEWER
				isEmpty |= !stmp.canvasRenderer.GetMesh(); // This is a different mesh than stmp.mesh
#endif
				SpriteMesh = isEmpty ? null : stmp.mesh;
			}
#endif
			SetLayoutDirty();
			SetTextureDirty();
		}

#if UNITY_EDITOR
		protected override void Reset()
		{
			base.Reset();
			ApplySerializedData();

			if (ProjectSettings.Instance.UseGlobalAngleByDefault) UseGlobalAngle = true;
		}

		protected override void OnValidate()
		{
			SetLayoutTextureDirty();
		}
#endif
	}
}
