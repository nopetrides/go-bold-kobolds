using System.Collections.Generic;
using System.Linq;
using LeTai.TrueShadow.PluginInterfaces;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LeTai.TrueShadow
{
	public partial class TrueShadow
	{
		private readonly List<Color32> meshColors = new(4);
		private readonly List<Color32> meshColorsOpaque = new(4);
		private ITrueShadowCasterClearColorProvider casterClearColorProvider;
		private ITrueShadowCasterMaterialPropertiesModifier casterMaterialPropertiesModifier;
		private ITrueShadowCasterMaterialProvider casterMaterialProvider;
		private ITrueShadowCasterMeshModifier casterMeshModifier;
		private ITrueShadowRendererMaterialModifier rendererMaterialModifier;
		private ITrueShadowRendererMaterialProvider rendererMaterialProvider;
		private ITrueShadowRendererMeshModifier rendererMeshModifier;

		public bool UsingRendererMaterialProvider => rendererMaterialProvider != null;

		private void InitializePlugins()
		{
			casterMaterialProvider = GetComponent<ITrueShadowCasterMaterialProvider>();
			casterMaterialPropertiesModifier = GetComponent<ITrueShadowCasterMaterialPropertiesModifier>();
			casterMeshModifier = GetComponent<ITrueShadowCasterMeshModifier>();
			casterClearColorProvider = GetComponent<ITrueShadowCasterClearColorProvider>();

			rendererMaterialProvider = GetComponent<ITrueShadowRendererMaterialProvider>();
			rendererMaterialModifier = GetComponent<ITrueShadowRendererMaterialModifier>();
			rendererMeshModifier = GetComponent<ITrueShadowRendererMeshModifier>();

			if (casterMaterialProvider != null)
			{
				casterMaterialProvider.materialReplaced += HandleCasterMaterialReplaced;
				casterMaterialProvider.materialModified += HandleCasterMaterialModified;
			}

			if (rendererMaterialProvider != null)
			{
				rendererMaterialProvider.materialReplaced += HandleRendererMaterialReplaced;
				rendererMaterialProvider.materialModified += HandleRendererMaterialModified;
			}
		}

		private void TerminatePlugins()
		{
			if (casterMaterialProvider != null)
			{
				casterMaterialProvider.materialReplaced -= HandleCasterMaterialReplaced;
				casterMaterialProvider.materialModified -= HandleCasterMaterialModified;
			}

			if (rendererMaterialProvider != null)
			{
				rendererMaterialProvider.materialReplaced -= HandleRendererMaterialReplaced;
				rendererMaterialProvider.materialModified -= HandleRendererMaterialModified;
			}
		}

		public void RefreshPlugins()
		{
			TerminatePlugins();
			InitializePlugins();
		}

		private void HandleCasterMaterialReplaced()
		{
			SetTextureDirty();
		}

		private void HandleRendererMaterialReplaced()
		{
			if (shadowRenderer)
				shadowRenderer.UpdateMaterial();
		}

		private void HandleCasterMaterialModified()
		{
			SetTextureDirty();
		}

		private void HandleRendererMaterialModified()
		{
			if (shadowRenderer)
				shadowRenderer.SetMaterialDirty();
		}

		public virtual Material GetShadowCastingMaterial()
		{
			Material provided = null;

			if (casterMaterialProvider != null)
				provided = casterMaterialProvider.GetTrueShadowCasterMaterial();

#if TMP_PRESENT
			else if (Graphic is TextMeshProUGUI
					|| Graphic is TMP_SubMeshUI)
				provided = Graphic.materialForRendering;
#endif

			return provided != null ? provided : Graphic.material;
		}

		public virtual void ModifyShadowCastingMaterialProperties(MaterialPropertyBlock propertyBlock)
		{
			casterMaterialPropertiesModifier?.ModifyTrueShadowCasterMaterialProperties(propertyBlock);
		}

		public virtual void ModifyShadowCastingMesh(Mesh mesh)
		{
			casterMeshModifier?.ModifyTrueShadowCasterMesh(mesh);

			// Caster can be semi-transparent, but cutout requires mostly opaque stencil.
			// Setting alpha to 1 in fragment can't work because of antialiasing.
			MakeOpaque(mesh);
		}

		private void MakeOpaque(Mesh mesh)
		{
			if (shadowAsSibling)
				return;

			mesh.GetColors(meshColors);
			var meshColorCount = meshColors.Count;

			if (meshColorCount < 1) return;

			if (meshColorsOpaque.Count == meshColorCount)
			{
				// Assuming vertex colors are identical
				// TODO: This is the case for builtin graphics, but userscript may invalidate that.
				if (meshColors[0].a == meshColorsOpaque[0].a)
					return;
			}
			else
			{
				// TODO: This assumed vertex count change infrequently. Is not the case with Text
				meshColorsOpaque.Clear();
				meshColorsOpaque.AddRange(Enumerable.Repeat(new Color32(0, 0, 0, 0), meshColorCount));
			}

			for (var i = 0; i < meshColorCount; i++)
			{
				var c = meshColors[i];
				c.a = 255;

				meshColorsOpaque[i] = c;
			}

			mesh.SetColors(meshColorsOpaque);
		}

		public virtual Material GetShadowRenderingMaterial()
		{
			var provided = rendererMaterialProvider?.GetTrueShadowRendererMaterial();
			return provided != null ? provided : BlendMode.GetMaterial();
		}

		public virtual void ModifyShadowRendererMaterial(Material baseMaterial)
		{
			rendererMaterialModifier?.ModifyTrueShadowRendererMaterial(baseMaterial);
		}

		public virtual void ModifyShadowRendererMesh(VertexHelper vertexHelper)
		{
			rendererMeshModifier?.ModifyTrueShadowRendererMesh(vertexHelper);
		}
	}
}
