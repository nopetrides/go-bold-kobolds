using Unity.Entities;
using UnityEngine;

namespace Kobolds.NetComponents.Authoring
{
	public class NetCodePlayerInputAuthoring : MonoBehaviour
	{
		public class SimpleBaker : Baker<NetCodePlayerInputAuthoring>
		{
			public override void Bake(NetCodePlayerInputAuthoring authoring)
			{
				Entity entity = GetEntity(TransformUsageFlags.Dynamic);
				AddComponent(entity, new NetCodePlayerInputComponent());
			}
		}
	}
}