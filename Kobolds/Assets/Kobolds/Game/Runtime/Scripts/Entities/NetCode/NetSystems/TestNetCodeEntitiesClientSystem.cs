using Kobolds.Rpc;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Kobolds.NetSystems
{
	/// <summary>
	/// System that runs only on the client
	/// </summary>
	[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
	partial struct TestNetCodeEntitiesClientSystem : ISystem
	{
		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{

		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			TestRpcOnKeyDown(ref state);
		}

		[BurstCompile]
		private void TestRpcOnKeyDown(ref SystemState state)
		{
			if (Input.GetKeyDown(KeyCode.T))
			{
				// Send Rpc
				var rpcEntity = state.EntityManager.CreateEntity();
				state.EntityManager.AddComponentData(rpcEntity, 
					new SimpleRpc 
					{
						Value = 69
					});
				
				state.EntityManager.AddComponentData(rpcEntity, 
					new SendRpcCommandRequest()); // Clients can only send RPCs to server
				Debug.Log("<color=cyan>[TestNetCodeEntitiesClientSystem] Sending Rpc");

				// For server, use TargetConnection field to send message to specific client / entity
				// state.EntityManager.AddComponentData(rpcEntity, 
				// 	new SendRpcCommandRequest 
				// 	{
				// 		TargetConnection = 
				// 	});
			}
		}

		[BurstCompile]
		public void OnDestroy(ref SystemState state)
		{

		}
	}
}