using Kobolds.NetComponents;
using Kobolds.Rpc;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Kobolds.NetSystems
{
	/// <summary>
	/// System that runs only on the server
	/// </summary>
	[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
	partial struct GoInGameServerSystem : ISystem
	{
		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<EntitiesReferencesComponent>();
			state.RequireForUpdate<NetworkId>();
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			CheckInGame(ref state);
		}

		[BurstCompile]
		private void CheckInGame(ref SystemState state)
		{
			// Buffer for the entity so we can execute commands on it when safe
			EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

			EntitiesReferencesComponent entitiesReferencesComponent =
				SystemAPI.GetSingleton<EntitiesReferencesComponent>();
			
			// Tuple from system api query
			foreach ((RefRO<ReceiveRpcCommandRequest> receiveRpcCommandRequest, Entity entity)
					in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>>().WithAll<GoInGameRequestRpc>().WithEntityAccess())
			{
				// Entity inside SourceConnection contains the NetworkId component, so we can know which client sent the rpc
				var connectedClientEntity = receiveRpcCommandRequest.ValueRO.SourceConnection;
				var networkId = state.EntityManager.GetComponentData<NetworkId>(connectedClientEntity);
				
				// Add the NetworkStreamInGame component to the entity that represents our connected client
				entityCommandBuffer.AddComponent<NetworkStreamInGame>(connectedClientEntity);
				
				Debug.Log($"<color=yellow>[GoInGameServerSystem] Received client InGame and set as InGame with NetworkId: {networkId.Value}");
				
				// Instantiate a new entity from the player prefab
				Entity netPlayerEntity = entityCommandBuffer.Instantiate(entitiesReferencesComponent.NetPlayerPrefabEntity);
				// Set its position to avoid stacking
				float angle = networkId.Value * 137.5f * Mathf.Deg2Rad;
				float radius = 1f * Mathf.Sqrt(networkId.Value);
				float3 position = new float3(radius * Mathf.Cos(angle), 0f, radius * Mathf.Sin(angle));
				entityCommandBuffer.SetComponent(netPlayerEntity, LocalTransform.FromPosition(position));
				// Set the NetworkId so we can track this ghost's owner
				entityCommandBuffer.AddComponent(netPlayerEntity, new GhostOwner
				{
					NetworkId = networkId.Value,
				});
				
				entityCommandBuffer.AppendToBuffer(receiveRpcCommandRequest.ValueRO.SourceConnection, new LinkedEntityGroup
				{
					Value = netPlayerEntity,
				});
				
				// Buffer the entity destruction now that we are done with it, but don't do it immediately
				entityCommandBuffer.DestroyEntity(entity);
			}
			
			// Tell the manager to execute the command buffer (destroy the entity)
			entityCommandBuffer.Playback(state.EntityManager);
		}

		[BurstCompile]
		public void OnDestroy(ref SystemState state)
		{

		}
	}
}