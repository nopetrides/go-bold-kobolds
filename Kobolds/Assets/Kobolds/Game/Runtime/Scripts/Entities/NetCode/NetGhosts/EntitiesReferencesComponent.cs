using Unity.Entities;

namespace Kobolds.NetComponents
{
	public struct EntitiesReferencesComponent : IComponentData
	{
		public Entity NetPlayerPrefabEntity;
	}
}