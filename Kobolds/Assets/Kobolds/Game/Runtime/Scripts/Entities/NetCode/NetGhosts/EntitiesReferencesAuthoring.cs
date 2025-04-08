using Unity.Entities;
using UnityEngine;

namespace Kobolds.NetComponents.Authoring
{
	public class EntitiesReferencesAuthoring : MonoBehaviour
	{
		[SerializeField] private GameObject NetPlayerPrefabGameObject;
		
		public class SimpleBaker : Baker<EntitiesReferencesAuthoring>
		{
			public override void Bake(EntitiesReferencesAuthoring authoring)
			{
				Entity entity = GetEntity(TransformUsageFlags.Dynamic);
				AddComponent(entity, new EntitiesReferencesComponent
				{
					NetPlayerPrefabEntity = GetEntity(authoring.NetPlayerPrefabGameObject, TransformUsageFlags.Dynamic),
				});
			}
		}
	}
}