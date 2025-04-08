using Unity.Entities;
using UnityEngine;

namespace Kobolds.NetComponents.Authoring
{
	public class NetPlayerAuthoring : MonoBehaviour
	{
		public class SimpleBaker : Baker<NetPlayerAuthoring>
		{
			public override void Bake(NetPlayerAuthoring authoring)
			{
				Entity entity = GetEntity(TransformUsageFlags.Dynamic);
				AddComponent(entity, new NetPlayerComponent());
			}
		}
	}
}