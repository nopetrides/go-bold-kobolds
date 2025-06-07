using Kobolds.NetComponents;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

/// <summary>
///     Client only system for handling input for ghosted entities
/// </summary>
[UpdateInGroup(typeof(GhostInputSystemGroup))]
internal partial struct NetCodePlayerInputSystem : ISystem
{
	[BurstCompile]
	public void OnCreate(ref SystemState state)
	{
		state.RequireForUpdate<NetworkStreamInGame>();
		state.RequireForUpdate<NetCodePlayerInputComponent>();
	}

	[BurstCompile]
	public void OnUpdate(ref SystemState state)
	{
		foreach (var netCodePlayerInput in
				SystemAPI.Query<RefRW<NetCodePlayerInputComponent>>().WithAll<GhostOwnerIsLocal>())
		{
			var inputVector = new float2();
			if (Input.GetKey(KeyCode.W))
				inputVector.y += 1;
			if (Input.GetKey(KeyCode.S))
				inputVector.y -= 1;
			if (Input.GetKey(KeyCode.A))
				inputVector.x -= 1;
			if (Input.GetKey(KeyCode.D))
				inputVector.x += 1;
			netCodePlayerInput.ValueRW.InputVector = inputVector;

			netCodePlayerInput.ValueRW.InputJump = Input.GetKeyDown(KeyCode.Space);
		}
	}

	[BurstCompile]
	public void OnDestroy(ref SystemState state)
	{
	}
}
