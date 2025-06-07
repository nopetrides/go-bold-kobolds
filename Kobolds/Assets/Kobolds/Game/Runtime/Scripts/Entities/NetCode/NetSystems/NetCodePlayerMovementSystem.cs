using Kobolds.NetComponents;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

/// <summary>
///     Synchronized movement system using prediction and physics.
/// </summary>
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
internal partial struct NetCodePlayerMovementSystem : ISystem
{
	[BurstCompile]
	public void OnCreate(ref SystemState state)
	{
		// Require PhysicsVelocity for physics-based movement
		state.RequireForUpdate<PhysicsVelocity>();
		// Require NetCodePlayerInputComponent for player input
		state.RequireForUpdate<NetCodePlayerInputComponent>();
		// Require LocalTransform for position and rotation (read-only here)
		state.RequireForUpdate<LocalTransform>();
	}

	[BurstCompile]
	public void OnUpdate(ref SystemState state)
	{
		// Define movement parameters
		var moveSpeed = 10f;
		var jumpForce = 5f; // Changed from jumpHeight to jumpForce for velocity

		// Move players based on client input and apply physics velocity
		foreach (var (netCodePlayerInput, physicsVelocity, localTransform)
				in // LocalTransform is now read-only for movement
				SystemAPI.Query<
					RefRO<NetCodePlayerInputComponent>,
					RefRW<PhysicsVelocity>,
					RefRO<LocalTransform>
				>().WithAll<Simulate>()) // Only simulate predicted entities
		{
			// Calculate desired horizontal velocity based on input
			var desiredHorizontalVelocity = new float3(
				netCodePlayerInput.ValueRO.InputVector.x,
				0,
				netCodePlayerInput.ValueRO.InputVector.y);

			// Normalize movement input and apply speed, considering delta time is handled by physics simulation
			if (math.lengthsq(desiredHorizontalVelocity) > 0.01f)
				desiredHorizontalVelocity = math.normalize(desiredHorizontalVelocity) * moveSpeed;

			// Set the linear velocity, preserving the existing vertical velocity (for gravity/jumps)
			physicsVelocity.ValueRW.Linear.x = desiredHorizontalVelocity.x;
			physicsVelocity.ValueRW.Linear.z = desiredHorizontalVelocity.z;

			// Handle jump input
			var jump = netCodePlayerInput.ValueRO.InputJump;
			// Check if the player is on the ground before allowing a jump (simplified check, requires more robust ground detection in a real game)
			// For now, we'll allow jumping in the air for testing.
			// TODO: Implement proper ground detection
			if (jump)
			{
				// Apply vertical impulse for jumping (sets vertical velocity)
				physicsVelocity.ValueRW.Linear.y = jumpForce;
				Debug.Log("Player jumped!"); // Debug log for jump action
			}

			// Note: Rotation is not handled in this system; LocalTransform is updated by the physics engine based on velocity.
		}
	}

	[BurstCompile]
	public void OnDestroy(ref SystemState state)
	{
		// Cleanup if needed
	}
}
