using Kobolds.NetComponents;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

/// <summary>
/// Synchronized movement system using prediction
/// </summary>
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
partial struct NetCodePlayerMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

	[BurstCompile]
	public void OnUpdate(ref SystemState state)
	{
		// Move based on client input
		foreach ((RefRO<NetCodePlayerInputComponent> netCodePlayerInput,
				RefRW<LocalTransform> localTransform) in
				SystemAPI.Query<
					RefRO<NetCodePlayerInputComponent>,
					RefRW<LocalTransform>
				>().WithAll<Simulate>())
		{
			float moveSpeed = 10f;
			float3 moveVector = new float3(
				netCodePlayerInput.ValueRO.InputVector.x, 
				0, 
				netCodePlayerInput.ValueRO.InputVector.y);
			localTransform.ValueRW.Position += moveVector * moveSpeed * SystemAPI.Time.DeltaTime;
		}
	}

	[BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
