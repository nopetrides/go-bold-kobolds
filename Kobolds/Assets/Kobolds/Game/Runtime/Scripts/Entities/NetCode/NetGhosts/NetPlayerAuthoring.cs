using Unity.Entities;
using UnityEngine;
using Unity.Physics;
using Unity.Mathematics;

namespace Kobolds.NetComponents.Authoring
{
	public class NetPlayerAuthoring : MonoBehaviour
	{
		// Serialized fields for player physics properties
		[SerializeField] private float playerMass = 1f;
		[SerializeField] private float playerRadius = 0.5f;
		[SerializeField] private float playerHeight = 1.8f;

		public class SimpleBaker : Baker<NetPlayerAuthoring>
		{
			public override void Bake(NetPlayerAuthoring authoring)
			{
				Entity entity = GetEntity(TransformUsageFlags.Dynamic);

				// Add the NetPlayerComponent
				AddComponent(entity, new NetPlayerComponent());

				// Add physics components for movement and collision
				AddComponent(entity, new PhysicsVelocity()); // Component to control velocity
				AddComponent(entity, new PhysicsGravityFactor { Value = 1f }); // Component to apply gravity

				var collider = Unity.Physics.CapsuleCollider.Create(new CapsuleGeometry
				{
					Vertex0 = new float3(0, -authoring.playerHeight * 0.5f + authoring.playerRadius, 0), // Bottom sphere center
					Vertex1 = new float3(0, authoring.playerHeight * 0.5f - authoring.playerRadius, 0), // Top sphere center
					Radius = authoring.playerRadius
				});

				// Wrap the BlobAssetReference in a PhysicsCollider component
				var physicsCollider = new PhysicsCollider
				{
					Value = collider
				};

				// Add the PhysicsCollider component to the entity
				AddComponent(entity, physicsCollider);

				// Use the collider's MassProperties to create the PhysicsMass component
				var mass = PhysicsMass.CreateDynamic(collider.Value.MassProperties, authoring.playerMass);
				AddComponent(entity, mass);
			}
		}
	}
}