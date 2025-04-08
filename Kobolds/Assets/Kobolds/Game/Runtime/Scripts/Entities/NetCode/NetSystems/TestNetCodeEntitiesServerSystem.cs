using Kobolds.Rpc;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Kobolds.NetSystems
{
	/// <summary>
	/// System that runs only on the server
	/// </summary>
	[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
	partial struct TestNetCodeEntitiesServerSystem : ISystem
	{
		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{

		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			// Buffer for the entity so we can execute commands on it when safe
			EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
			
			// Tuple from system api query
			foreach ((RefRO<SimpleRpc> simpleRpc, RefRO<ReceiveRpcCommandRequest> receiveRpcCommandRequest, Entity entity)
				in SystemAPI.Query<RefRO<SimpleRpc>, RefRO<ReceiveRpcCommandRequest>>().WithEntityAccess())
			{
				// Entity inside SourceConnection contains the NetworkId component, so we can know which client sent the rpc
				var connectedClientEntity = receiveRpcCommandRequest.ValueRO.SourceConnection;
				var networkId = state.EntityManager.GetComponentData<NetworkId>(connectedClientEntity);
				
				Debug.Log($"<color=yellow>[TestNetCodeEntitiesServerSystem] Received Rpc: {simpleRpc.ValueRO.Value} from client id: {networkId.Value}");
				
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