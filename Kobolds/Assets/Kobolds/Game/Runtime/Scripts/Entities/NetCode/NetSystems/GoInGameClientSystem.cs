using Kobolds.NetComponents;
using Kobolds.Rpc;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Kobolds.NetSystems
{
	/// <summary>
	///     System that runs only on the client
	/// </summary>
	[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
	internal partial struct GoInGameClientSystem : ISystem
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
			var entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

			// Tuple from system api query
			foreach (var (networkId, entity)
					in SystemAPI.Query<RefRO<NetworkId>>().WithNone<NetworkStreamInGame>().WithEntityAccess())
			{
				// Buffer the add component to entity action
				entityCommandBuffer.AddComponent<NetworkStreamInGame>(entity);

				Debug.Log(
					$"<color=cyan>[GoInGameClientSystem] Sending client as InGame with NetworkId {networkId.ValueRO.Value}");

				// Create an entity for the rpc, send it to the server
				var rpcEntity = entityCommandBuffer.CreateEntity();
				entityCommandBuffer.AddComponent(rpcEntity, new GoInGameRequestRpc());
				entityCommandBuffer.AddComponent(rpcEntity, new SendRpcCommandRequest());
			}

			// Tell the manager to execute the command buffer
			entityCommandBuffer.Playback(state.EntityManager);
		}

		[BurstCompile]
		public void OnDestroy(ref SystemState state)
		{
		}
	}
}
