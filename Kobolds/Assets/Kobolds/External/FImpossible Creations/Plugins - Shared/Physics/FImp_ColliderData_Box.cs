using UnityEngine;

namespace FIMSpace
{
	public class FImp_ColliderData_Box : FImp_ColliderData_Base
	{
		private Vector3 boxCenter;
		private Vector3 forward;
		private Vector3 forwardN;

		private Vector3 right;

		private Vector3 rightN;

		private Vector3 scales;
		private Vector3 up;
		private Vector3 upN;

		// For 3D
		public FImp_ColliderData_Box(BoxCollider collider)
		{
			Is2D = false;
			Collider = collider;
			Transform = collider.transform;
			Box = collider;
			ColliderType = EFColliderType.Box;
			RefreshColliderData();
			previousPosition = Transform.position + Vector3.forward * Mathf.Epsilon;
		}

		// For 2D
		public FImp_ColliderData_Box(BoxCollider2D collider2D)
		{
			Is2D = true;
			Collider2D = collider2D;
			Transform = collider2D.transform;
			Box2D = collider2D;
			ColliderType = EFColliderType.Box;
			RefreshColliderData();
			previousPosition = Transform.position + Vector3.forward * Mathf.Epsilon;
		}

		public BoxCollider Box { get; }
		public BoxCollider2D Box2D { get; }


#region Refreshing Data

		public override void RefreshColliderData()
		{
			if (IsStatic) return; // No need to refresh collider data if it is static

			if (Collider2D == null) // 3D Refresh
			{
				var diff = false;

				if (!Transform.position.VIsSame(previousPosition)) diff = true;
				else if (!Transform.rotation.QIsSame(previousRotation)) diff = true;

				if (diff)
				{
					right = Box.transform.TransformVector(Vector3.right / 2f * Box.size.x);
					up = Box.transform.TransformVector(Vector3.up / 2f * Box.size.y);
					forward = Box.transform.TransformVector(Vector3.forward / 2f * Box.size.z);

					rightN = right.normalized;
					upN = up.normalized;
					forwardN = forward.normalized;

					boxCenter = GetBoxCenter(Box);

					scales = Vector3.Scale(Box.size, Box.transform.lossyScale);
					scales.Normalize();
				}
			}
			else // 2D Refresh
			{
				var diff = false;

				if (Vector2.Distance(Transform.position, previousPosition) > Mathf.Epsilon)
					diff = true;
				else if (!Transform.rotation.QIsSame(previousRotation)) diff = true;

				if (diff)
				{
					right = Box2D.transform.TransformVector(Vector3.right / 2f * Box2D.size.x);
					up = Box2D.transform.TransformVector(Vector3.up / 2f * Box2D.size.y);

					rightN = right.normalized;
					upN = up.normalized;

					boxCenter = GetBoxCenter(Box2D);
					boxCenter.z = 0f;

					var scale = Transform.lossyScale;
					scale.z = 1f;
					scales = Vector3.Scale(Box2D.size, scale);
					scales.Normalize();
				}
			}

			base.RefreshColliderData();

			previousPosition = Transform.position;
			previousRotation = Transform.rotation;
		}

#endregion


		public override bool PushIfInside(ref Vector3 segmentPosition, float segmentRadius, Vector3 segmentOffset)
		{
			var inOrInt = 0;
			var interPlane = Vector3.zero;
			var segmentOffsetted = segmentPosition + segmentOffset;
			var planeDistance = PlaneDistance(boxCenter + up, upN, segmentOffsetted);
			if (SphereInsidePlane(planeDistance, segmentRadius))
			{
				inOrInt++;
			}
			else if (SphereIntersectsPlane(planeDistance, segmentRadius))
			{
				inOrInt++;
				interPlane = up;
			}

			planeDistance = PlaneDistance(boxCenter - up, -upN, segmentOffsetted);
			if (SphereInsidePlane(planeDistance, segmentRadius))
			{
				inOrInt++;
			}
			else if (SphereIntersectsPlane(planeDistance, segmentRadius))
			{
				inOrInt++;
				interPlane = -up;
			}

			planeDistance = PlaneDistance(boxCenter - right, -rightN, segmentOffsetted);
			if (SphereInsidePlane(planeDistance, segmentRadius))
			{
				inOrInt++;
			}
			else if (SphereIntersectsPlane(planeDistance, segmentRadius))
			{
				inOrInt++;
				interPlane = -right;
			}

			planeDistance = PlaneDistance(boxCenter + right, rightN, segmentOffsetted);
			if (SphereInsidePlane(planeDistance, segmentRadius))
			{
				inOrInt++;
			}
			else if (SphereIntersectsPlane(planeDistance, segmentRadius))
			{
				inOrInt++;
				interPlane = right;
			}

			var insideOrIntersects = false;

			if (Collider2D == null)
			{
				planeDistance = PlaneDistance(boxCenter + forward, forwardN, segmentOffsetted);
				if (SphereInsidePlane(planeDistance, segmentRadius))
				{
					inOrInt++;
				}
				else if (SphereIntersectsPlane(planeDistance, segmentRadius))
				{
					inOrInt++;
					interPlane = forward;
				}

				planeDistance = PlaneDistance(boxCenter - forward, -forwardN, segmentOffsetted);
				if (SphereInsidePlane(planeDistance, segmentRadius))
				{
					inOrInt++;
				}
				else if (SphereIntersectsPlane(planeDistance, segmentRadius))
				{
					inOrInt++;
					interPlane = -forward;
				}

				if (inOrInt == 6) insideOrIntersects = true;
			}
			else if (inOrInt == 4)
			{
				insideOrIntersects = true;
			}

			if (insideOrIntersects)
			{
				var inside = false;
				//Vector3 rayDirection;

				if (interPlane.sqrMagnitude == 0f) // sphere is inside the box
				{
					//if ( Collider2D == null)
					//    interPlane = -GetTargetPlaneNormal(Box, segmentOffsetted, right, up, forward, scales);
					//else
					//    interPlane = -GetTargetPlaneNormal(Box2D, segmentOffsetted, right, up, scales);
					inside = true;
					//rayDirection = (interPlane).normalized; // poprawić przy przeskalowanych boxach
				}
				else // sphere is intersecting box
				{
					//rayDirection = (segmentOffsetted - boxCenter).normalized;
					if (Collider2D == null)
					{
						if (IsInsideBoxCollider(Box, segmentOffsetted)) inside = true;
					}
					else if (IsInsideBoxCollider(Box2D, segmentOffsetted))
					{
						inside = true;
					}
				}

				var pointOnPlane = GetNearestPoint(segmentOffsetted);
				var toNormal = pointOnPlane - segmentOffsetted;

				if (inside) toNormal += toNormal.normalized * segmentRadius;
				else toNormal -= toNormal.normalized * segmentRadius;
				//Debug.DrawRay(pointOnPlane, toNormal);

				if (inside)
					segmentPosition = segmentPosition + toNormal;
				else if (toNormal.sqrMagnitude > 0) segmentPosition = segmentPosition + toNormal;

				return true;
			}

			return false;
		}


		public static void PushOutFromBoxCollider(
			BoxCollider box, Collision collision, float segmentColliderRadius, ref Vector3 segmentPosition,
			bool is2D = false)
		{
			var right = box.transform.TransformVector(Vector3.right / 2f * box.size.x + box.center.x * Vector3.right);
			var up = box.transform.TransformVector(Vector3.up / 2f * box.size.y + box.center.y * Vector3.up);
			var forward =
				box.transform.TransformVector(Vector3.forward / 2f * box.size.z + box.center.z * Vector3.forward);

			var scales = Vector3.Scale(box.size, box.transform.lossyScale);
			scales.Normalize();

			PushOutFromBoxCollider(
				box, collision, segmentColliderRadius, ref segmentPosition, right, up, forward, scales, is2D);
		}

		public static void PushOutFromBoxCollider(
			BoxCollider box, float segmentColliderRadius, ref Vector3 segmentPosition, bool is2D = false)
		{
			var right = box.transform.TransformVector(Vector3.right / 2f * box.size.x + box.center.x * Vector3.right);
			var up = box.transform.TransformVector(Vector3.up / 2f * box.size.y + box.center.y * Vector3.up);
			var forward =
				box.transform.TransformVector(Vector3.forward / 2f * box.size.z + box.center.z * Vector3.forward);

			var scales = Vector3.Scale(box.size, box.transform.lossyScale);
			scales.Normalize();

			var boxCenter = GetBoxCenter(box);

			var pointRadius = segmentColliderRadius;
			var upN = up.normalized;
			var rightN = right.normalized;
			var forwardN = forward.normalized;

			var inOrInt = 0;
			var interPlane = Vector3.zero;
			var planeDistance = PlaneDistance(boxCenter + up, upN, segmentPosition);
			if (SphereInsidePlane(planeDistance, pointRadius))
			{
				inOrInt++;
			}
			else if (SphereIntersectsPlane(planeDistance, pointRadius))
			{
				inOrInt++;
				interPlane = up;
			}

			planeDistance = PlaneDistance(boxCenter - up, -upN, segmentPosition);
			if (SphereInsidePlane(planeDistance, pointRadius))
			{
				inOrInt++;
			}
			else if (SphereIntersectsPlane(planeDistance, pointRadius))
			{
				inOrInt++;
				interPlane = -up;
			}

			planeDistance = PlaneDistance(boxCenter - right, -rightN, segmentPosition);
			if (SphereInsidePlane(planeDistance, pointRadius))
			{
				inOrInt++;
			}
			else if (SphereIntersectsPlane(planeDistance, pointRadius))
			{
				inOrInt++;
				interPlane = -right;
			}

			planeDistance = PlaneDistance(boxCenter + right, rightN, segmentPosition);
			if (SphereInsidePlane(planeDistance, pointRadius))
			{
				inOrInt++;
			}
			else if (SphereIntersectsPlane(planeDistance, pointRadius))
			{
				inOrInt++;
				interPlane = right;
			}

			planeDistance = PlaneDistance(boxCenter + forward, forwardN, segmentPosition);
			if (SphereInsidePlane(planeDistance, pointRadius))
			{
				inOrInt++;
			}
			else if (SphereIntersectsPlane(planeDistance, pointRadius))
			{
				inOrInt++;
				interPlane = forward;
			}

			planeDistance = PlaneDistance(boxCenter - forward, -forwardN, segmentPosition);
			if (SphereInsidePlane(planeDistance, pointRadius))
			{
				inOrInt++;
			}
			else if (SphereIntersectsPlane(planeDistance, pointRadius))
			{
				inOrInt++;
				interPlane = -forward;
			}

			// Collision occured - sphere intersecting box shape volume or is inside of it
			if (inOrInt == 6)
			{
				var inside = false;
				//Vector3 rayDirection;

				if (interPlane.sqrMagnitude == 0f) // sphere is inside the box
				{
					//interPlane = -GetTargetPlaneNormal(box, segmentPosition, right, up, forward, scales, is2D);
					inside = true;
					//rayDirection = (interPlane).normalized; // poprawić przy przeskalowanych boxach
				}
				else // sphere is intersecting box
				{
					//rayDirection = (segmentPosition - boxCenter).normalized;
					if (IsInsideBoxCollider(box, segmentPosition)) inside = true;
				}

				var pointOnPlane = GetNearestPoint(segmentPosition, boxCenter, right, up, forward, is2D);

				var toNormal = pointOnPlane - segmentPosition;
				if (inside) toNormal += toNormal.normalized * pointRadius * 1.01f;
				else toNormal -= toNormal.normalized * pointRadius * 1.01f;

				if (inside)
					segmentPosition = segmentPosition + toNormal;
				else if (toNormal.sqrMagnitude > 0) segmentPosition = segmentPosition + toNormal;
			}
		}


		public static void PushOutFromBoxCollider(
			BoxCollider box, Collision collision, float segmentColliderRadius, ref Vector3 pos, Vector3 right,
			Vector3 up, Vector3 forward, Vector3 scales, bool is2D = false)
		{
			var collisionPoint = collision.contacts[0].point;
			var pushNormal = pos - collisionPoint;
			var boxCenter = GetBoxCenter(box);
			if (pushNormal.sqrMagnitude == 0f) pushNormal = pos - boxCenter;

			var insideMul = 1f;
			if (IsInsideBoxCollider(box, pos))
			{
				// Finding intersection point on the box from the inside 
				var castFactor = GetBoxAverageScale(box);
				var fittingNormal = GetTargetPlaneNormal(box, pos, right, up, forward, scales);
				var fittingNormalNorm = fittingNormal.normalized;

				RaycastHit info;
				// Doing cheap boxCollider's raycast from outside to hit surface
				if (box.Raycast(
						new Ray(pos - fittingNormalNorm * castFactor * 3f, fittingNormalNorm), out info,
						castFactor * 4))
					collisionPoint = info.point;
				else
					collisionPoint = GetIntersectOnBoxFromInside(box, boxCenter, pos, fittingNormal);

				pushNormal = collisionPoint - pos;
				insideMul = 100f;
			}

			var toNormal = pos - (pushNormal / insideMul + pushNormal.normalized * 1.15f) / 2f * segmentColliderRadius;
			toNormal = collisionPoint - toNormal;

			var pushMagn = toNormal.sqrMagnitude;
			if (pushMagn > 0 && pushMagn < segmentColliderRadius * segmentColliderRadius * insideMul)
				pos = pos + toNormal;
		}

#region Push out from box 2D

		public static void PushOutFromBoxCollider(
			BoxCollider2D box2D, float segmentColliderRadius, ref Vector3 segmentPosition)
		{
			Vector2 right =
				box2D.transform.TransformVector(Vector3.right / 2f * box2D.size.x + box2D.offset.x * Vector3.right);
			Vector2 up = box2D.transform.TransformVector(Vector3.up / 2f * box2D.size.y + box2D.offset.y * Vector3.up);

			var scale2D = box2D.transform.lossyScale;
			scale2D.z = 1f;
			Vector2 scales = Vector3.Scale(box2D.size, scale2D);
			scales.Normalize();

			Vector2 boxCenter = GetBoxCenter(box2D);

			var pointRadius = segmentColliderRadius;
			var upN = up.normalized;
			var rightN = right.normalized;

			var inOrInt = 0;
			var interPlane = Vector3.zero;
			var planeDistance = PlaneDistance(boxCenter + up, upN, segmentPosition);
			if (SphereInsidePlane(planeDistance, pointRadius))
			{
				inOrInt++;
			}
			else if (SphereIntersectsPlane(planeDistance, pointRadius))
			{
				inOrInt++;
				interPlane = up;
			}

			planeDistance = PlaneDistance(boxCenter - up, -upN, segmentPosition);
			if (SphereInsidePlane(planeDistance, pointRadius))
			{
				inOrInt++;
			}
			else if (SphereIntersectsPlane(planeDistance, pointRadius))
			{
				inOrInt++;
				interPlane = -up;
			}

			planeDistance = PlaneDistance(boxCenter - right, -rightN, segmentPosition);
			if (SphereInsidePlane(planeDistance, pointRadius))
			{
				inOrInt++;
			}
			else if (SphereIntersectsPlane(planeDistance, pointRadius))
			{
				inOrInt++;
				interPlane = -right;
			}

			planeDistance = PlaneDistance(boxCenter + right, rightN, segmentPosition);
			if (SphereInsidePlane(planeDistance, pointRadius))
			{
				inOrInt++;
			}
			else if (SphereIntersectsPlane(planeDistance, pointRadius))
			{
				inOrInt++;
				interPlane = right;
			}

			// Collision occured - sphere intersecting box shape volume or is inside of it
			if (inOrInt == 4)
			{
				var inside = false;

				if (interPlane.sqrMagnitude == 0f) // sphere is inside the box
				{
					//interPlane = -GetTargetPlaneNormal(box2D, segmentPosition, right, up, scales);
					inside = true;
				}
				else // sphere is intersecting box
				{
					if (IsInsideBoxCollider(box2D, segmentPosition)) inside = true;
				}

				var pointOnPlane = GetNearestPoint2D(segmentPosition, boxCenter, right, up);

				var toNormal = pointOnPlane - segmentPosition;
				if (inside) toNormal += toNormal.normalized * pointRadius * 1.01f;
				else toNormal -= toNormal.normalized * pointRadius * 1.01f;

				if (inside)
					segmentPosition = segmentPosition + toNormal;
				else if (toNormal.sqrMagnitude > 0) segmentPosition = segmentPosition + toNormal;
			}
		}

#endregion


#region Box Calculations Helpers

		/// <summary>
		///     Getting nearest plane normal fitting to given point position
		/// </summary>
		private Vector3 GetNearestPoint(Vector3 point)
		{
			var pointOnBox = point;

			var distancesPositive = Vector3.one;
			distancesPositive.x = PlaneDistance(boxCenter + right, rightN, point);
			distancesPositive.y = PlaneDistance(boxCenter + up, upN, point);
			if (Collider2D == null) distancesPositive.z = PlaneDistance(boxCenter + forward, forwardN, point);

			var distancesNegative = Vector3.one;
			distancesNegative.x = PlaneDistance(boxCenter - right, -rightN, point);
			distancesNegative.y = PlaneDistance(boxCenter - up, -upN, point);
			if (Collider2D == null) distancesNegative.z = PlaneDistance(boxCenter - forward, -forwardN, point);

			float nearestX, nearestY, nearestZ;
			float negX = 1f, negY = 1f, negZ = 1f;

			if (distancesPositive.x > distancesNegative.x)
			{
				nearestX = distancesPositive.x;
				negX = -1f;
			}
			else
			{
				nearestX = distancesNegative.x;
				negX = 1f;
			}

			if (distancesPositive.y > distancesNegative.y)
			{
				nearestY = distancesPositive.y;
				negY = -1f;
			}
			else
			{
				nearestY = distancesNegative.y;
				negY = 1f;
			}

			if (Collider2D == null)
			{
				if (distancesPositive.z > distancesNegative.z)
				{
					nearestZ = distancesPositive.z;
					negZ = -1f;
				}
				else
				{
					nearestZ = distancesNegative.z;
					negZ = 1f;
				}

				if (nearestX > nearestZ)
				{
					if (nearestX > nearestY)
						pointOnBox = ProjectPointOnPlane(right * negX, point, nearestX);
					else
						pointOnBox = ProjectPointOnPlane(up * negY, point, nearestY);
				}
				else
				{
					if (nearestZ > nearestY)
						pointOnBox = ProjectPointOnPlane(forward * negZ, point, nearestZ);
					else
						pointOnBox = ProjectPointOnPlane(up * negY, point, nearestY);
				}
			}
			else
			{
				if (nearestX > nearestY)
					pointOnBox = ProjectPointOnPlane(right * negX, point, nearestX);
				else
					pointOnBox = ProjectPointOnPlane(up * negY, point, nearestY);
			}


			return pointOnBox;
		}

		/// <summary>
		///     Getting nearest plane normal fitting to given point position
		/// </summary>
		private static Vector3 GetNearestPoint(
			Vector3 point, Vector3 boxCenter, Vector3 right, Vector3 up, Vector3 forward, bool is2D = false)
		{
			var pointOnBox = point;

			var distancesPositive = Vector3.one;
			distancesPositive.x = PlaneDistance(boxCenter + right, right.normalized, point);
			distancesPositive.y = PlaneDistance(boxCenter + up, up.normalized, point);
			if (is2D == false) distancesPositive.z = PlaneDistance(boxCenter + forward, forward.normalized, point);

			var distancesNegative = Vector3.one;
			distancesNegative.x = PlaneDistance(boxCenter - right, -right.normalized, point);
			distancesNegative.y = PlaneDistance(boxCenter - up, -up.normalized, point);
			if (is2D == false) distancesNegative.z = PlaneDistance(boxCenter - forward, -forward.normalized, point);

			float nearestX, nearestY, nearestZ;
			float negX = 1f, negY = 1f, negZ = 1f;

			if (distancesPositive.x > distancesNegative.x)
			{
				nearestX = distancesPositive.x;
				negX = -1f;
			}
			else
			{
				nearestX = distancesNegative.x;
				negX = 1f;
			}

			if (distancesPositive.y > distancesNegative.y)
			{
				nearestY = distancesPositive.y;
				negY = -1f;
			}
			else
			{
				nearestY = distancesNegative.y;
				negY = 1f;
			}

			if (is2D == false)
			{
				if (distancesPositive.z > distancesNegative.z)
				{
					nearestZ = distancesPositive.z;
					negZ = -1f;
				}
				else
				{
					nearestZ = distancesNegative.z;
					negZ = 1f;
				}

				if (nearestX > nearestZ)
				{
					if (nearestX > nearestY)
						pointOnBox = ProjectPointOnPlane(right * negX, point, nearestX);
					else
						pointOnBox = ProjectPointOnPlane(up * negY, point, nearestY);
				}
				else
				{
					if (nearestZ > nearestY)
						pointOnBox = ProjectPointOnPlane(forward * negZ, point, nearestZ);
					else
						pointOnBox = ProjectPointOnPlane(up * negY, point, nearestY);
				}
			}
			else
			{
				if (nearestX > nearestY)
					pointOnBox = ProjectPointOnPlane(right * negX, point, nearestX);
				else
					pointOnBox = ProjectPointOnPlane(up * negY, point, nearestY);
			}

			return pointOnBox;
		}

		/// <summary>
		///     Getting nearest plane normal fitting to given point position
		/// </summary>
		private static Vector3 GetNearestPoint2D(Vector2 point, Vector2 boxCenter, Vector2 right, Vector2 up)
		{
			Vector3 pointOnBox = point;

			var distancesPositive = Vector3.one;
			distancesPositive.x = PlaneDistance(boxCenter + right, right.normalized, point);
			distancesPositive.y = PlaneDistance(boxCenter + up, up.normalized, point);

			var distancesNegative = Vector3.one;
			distancesNegative.x = PlaneDistance(boxCenter - right, -right.normalized, point);
			distancesNegative.y = PlaneDistance(boxCenter - up, -up.normalized, point);

			float nearestX, nearestY;
			float negX = 1f, negY = 1f;

			if (distancesPositive.x > distancesNegative.x)
			{
				nearestX = distancesPositive.x;
				negX = -1f;
			}
			else
			{
				nearestX = distancesNegative.x;
				negX = 1f;
			}

			if (distancesPositive.y > distancesNegative.y)
			{
				nearestY = distancesPositive.y;
				negY = -1f;
			}
			else
			{
				nearestY = distancesNegative.y;
				negY = 1f;
			}

			if (nearestX > nearestY)
				pointOnBox = ProjectPointOnPlane(right * negX, point, nearestX);
			else
				pointOnBox = ProjectPointOnPlane(up * negY, point, nearestY);

			return pointOnBox;
		}


		/// <summary>
		///     Getting nearest plane point on box collider
		/// </summary>
		public static Vector3 GetNearestPointOnBox(BoxCollider boxCollider, Vector3 point, bool is2D = false)
		{
			var right = boxCollider.transform.TransformVector(Vector3.right / 2f);
			var up = boxCollider.transform.TransformVector(Vector3.up / 2f);
			var forward = Vector3.forward;
			if (is2D == false) forward = boxCollider.transform.TransformVector(Vector3.forward / 2f);

			var pointOnBox = point;
			var center = GetBoxCenter(boxCollider);

			var rightN = right.normalized;
			var upN = up.normalized;
			var forwardN = forward.normalized;

			var distancesPositive = Vector3.one;
			distancesPositive.x = PlaneDistance(center + right, rightN, point);
			distancesPositive.y = PlaneDistance(center + up, upN, point);
			if (is2D == false) distancesPositive.z = PlaneDistance(center + forward, forwardN, point);

			var distancesNegative = Vector3.one;
			distancesNegative.x = PlaneDistance(center - right, -rightN, point);
			distancesNegative.y = PlaneDistance(center - up, -upN, point);
			if (is2D == false) distancesNegative.z = PlaneDistance(center - forward, -forwardN, point);

			float nearestX, nearestY, nearestZ;
			float negX = 1f, negY = 1f, negZ = 1f;

			if (distancesPositive.x > distancesNegative.x)
			{
				nearestX = distancesPositive.x;
				negX = -1f;
			}
			else
			{
				nearestX = distancesNegative.x;
				negX = 1f;
			}

			if (distancesPositive.y > distancesNegative.y)
			{
				nearestY = distancesPositive.y;
				negY = -1f;
			}
			else
			{
				nearestY = distancesNegative.y;
				negY = 1f;
			}

			if (is2D == false)
			{
				if (distancesPositive.z > distancesNegative.z)
				{
					nearestZ = distancesPositive.z;
					negZ = -1f;
				}
				else
				{
					nearestZ = distancesNegative.z;
					negZ = 1f;
				}

				if (nearestX > nearestZ)
				{
					if (nearestX > nearestY)
						pointOnBox = ProjectPointOnPlane(right * negX, point, nearestX);
					else
						pointOnBox = ProjectPointOnPlane(up * negY, point, nearestY);
				}
				else
				{
					if (nearestZ > nearestY)
						pointOnBox = ProjectPointOnPlane(forward * negZ, point, nearestZ);
					else
						pointOnBox = ProjectPointOnPlane(up * negY, point, nearestY);
				}
			}
			else
			{
				if (nearestX > nearestY)
					pointOnBox = ProjectPointOnPlane(right * negX, point, nearestX);
				else
					pointOnBox = ProjectPointOnPlane(up * negY, point, nearestY);
			}


			return pointOnBox;
		}


		private static float PlaneDistance(Vector3 planeCenter, Vector3 planeNormal, Vector3 point)
		{
			return Vector3.Dot(point - planeCenter, planeNormal);
		}

		private static Vector3 ProjectPointOnPlane(Vector3 planeNormal, Vector3 point, float distance)
		{
			var translationVector = planeNormal.normalized * distance;
			return point + translationVector;
		}

		private static bool SphereInsidePlane(float planeDistance, float pointRadius)
		{
			return -planeDistance > pointRadius;
		}

		private static bool SphereOutsidePlane(float planeDistance, float pointRadius)
		{
			return planeDistance > pointRadius;
		}

		private static bool SphereIntersectsPlane(float planeDistance, float pointRadius)
		{
			return Mathf.Abs(planeDistance) <= pointRadius;
		}


		public static bool IsInsideBoxCollider(BoxCollider collider, Vector3 point, bool is2D = false)
		{
			point = collider.transform.InverseTransformPoint(point) - collider.center;

			var xExtend = collider.size.x * 0.5f;
			var yExtend = collider.size.y * 0.5f;
			var zExtend = collider.size.z * 0.5f;
			return point.x < xExtend && point.x > -xExtend && point.y < yExtend && point.y > -yExtend &&
					point.z < zExtend && point.z > -zExtend;
		}

		// 2D Version
		public static bool IsInsideBoxCollider(BoxCollider2D collider, Vector3 point)
		{
			point = (Vector2) collider.transform.InverseTransformPoint(point) - collider.offset;

			var xExtend = collider.size.x * 0.5f;
			var yExtend = collider.size.y * 0.5f;

			return point.x < xExtend && point.x > -xExtend && point.y < yExtend && point.y > -yExtend;
		}


		/// <summary>
		///     Getting average scale of box's dimensions
		/// </summary>
		protected static float GetBoxAverageScale(BoxCollider box)
		{
			var scales = box.transform.lossyScale;
			scales = Vector3.Scale(scales, box.size);
			return (scales.x + scales.y + scales.z) / 3f;
		}

		protected static Vector3 GetBoxCenter(BoxCollider box)
		{
			return box.transform.position + box.transform.TransformVector(box.center);
		}

		protected static Vector3 GetBoxCenter(BoxCollider2D box)
		{
			return box.transform.position + box.transform.TransformVector(box.offset);
		}

		protected static Vector3 GetTargetPlaneNormal(BoxCollider boxCollider, Vector3 point, bool is2D = false)
		{
			var right = boxCollider.transform.TransformVector(Vector3.right / 2f * boxCollider.size.x);
			var up = boxCollider.transform.TransformVector(Vector3.up / 2f * boxCollider.size.y);
			var forward = Vector3.forward;
			if (is2D == false)
				forward = boxCollider.transform.TransformVector(Vector3.forward / 2f * boxCollider.size.z);

			var scales = Vector3.Scale(boxCollider.size, boxCollider.transform.lossyScale);
			scales.Normalize();

			return GetTargetPlaneNormal(boxCollider, point, right, up, forward, scales, is2D);
		}

		/// <summary>
		///     Getting nearest plane normal fitting to given point position
		/// </summary>
		protected static Vector3 GetTargetPlaneNormal(
			BoxCollider boxCollider, Vector3 point, Vector3 right, Vector3 up, Vector3 forward, Vector3 scales,
			bool is2D = false)
		{
			var rayDirection = (GetBoxCenter(boxCollider) - point).normalized;

			// Finding proper box's plane
			Vector3 dots;
			dots.x = Vector3.Dot(rayDirection, right.normalized);
			dots.y = Vector3.Dot(rayDirection, up.normalized);
			dots.x = dots.x * scales.y * scales.z;
			dots.y = dots.y * scales.x * scales.z;

			if (is2D == false)
			{
				dots.z = Vector3.Dot(rayDirection, forward.normalized);
				dots.z = dots.z * scales.y * scales.x;
			}
			else
			{
				dots.z = 0;
			}

			dots.Normalize();

			var dotsAbs = dots;
			if (dots.x < 0) dotsAbs.x = -dots.x;
			if (dots.y < 0) dotsAbs.y = -dots.y;
			if (dots.z < 0) dotsAbs.z = -dots.z;

			Vector3 planeNormal;
			if (dotsAbs.x > dotsAbs.y)
			{
				if (dotsAbs.x > dotsAbs.z || is2D) planeNormal = right * Mathf.Sign(dots.x);
				else planeNormal = forward * Mathf.Sign(dots.z);
			}
			else
			{
				if (dotsAbs.y > dotsAbs.z || is2D) planeNormal = up * Mathf.Sign(dots.y);
				else planeNormal = forward * Mathf.Sign(dots.z);
			}

			return planeNormal;
		}


		// 2D Version
		protected static Vector3 GetTargetPlaneNormal(
			BoxCollider2D boxCollider, Vector2 point, Vector2 right, Vector2 up, Vector2 scales)
		{
			var rayDirection = ((Vector2) GetBoxCenter(boxCollider) - point).normalized;

			// Finding proper box's plane
			Vector2 dots;
			dots.x = Vector3.Dot(rayDirection, right.normalized);
			dots.y = Vector3.Dot(rayDirection, up.normalized);
			dots.x = dots.x * scales.y;
			dots.y = dots.y * scales.x;

			dots.Normalize();

			var dotsAbs = dots;
			if (dots.x < 0) dotsAbs.x = -dots.x;
			if (dots.y < 0) dotsAbs.y = -dots.y;

			Vector3 planeNormal;
			if (dotsAbs.x > dotsAbs.y) planeNormal = right * Mathf.Sign(dots.x);
			else
				planeNormal = up * Mathf.Sign(dots.y);

			return planeNormal;
		}


		/// <summary>
		///     Calculating cheap ray on box plane to detect position from inside
		/// </summary>
		protected static Vector3 GetIntersectOnBoxFromInside(
			BoxCollider boxCollider, Vector3 from, Vector3 to, Vector3 planeNormal)
		{
			var rayDirection = to - from;

			// Creating box's plane and casting cheap ray on it to detect intersection position
			var plane = new Plane(-planeNormal, GetBoxCenter(boxCollider) + planeNormal);
			var intersectionPoint = to;

			var enter = 0f;
			var ray = new Ray(from, rayDirection);
			if (plane.Raycast(ray, out enter)) intersectionPoint = ray.GetPoint(enter);

			return intersectionPoint;
		}

#endregion
	}
}
