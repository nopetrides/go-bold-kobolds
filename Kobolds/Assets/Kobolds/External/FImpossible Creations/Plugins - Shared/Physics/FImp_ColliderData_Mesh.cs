using UnityEngine;

namespace FIMSpace
{
	public class FImp_ColliderData_Mesh : FImp_ColliderData_Base
	{
		private readonly ContactFilter2D filter;

		private readonly RaycastHit2D[] r;

		public FImp_ColliderData_Mesh(MeshCollider collider)
		{
			Is2D = false;
			Transform = collider.transform;
			Collider = collider;
			Mesh = collider;
			ColliderType = EFColliderType.Mesh;
		}

		public FImp_ColliderData_Mesh(PolygonCollider2D collider)
		{
			Is2D = true;
			Transform = collider.transform;
			Poly2D = collider;
			Collider2D = collider;
			ColliderType = EFColliderType.Mesh;
			filter = new ContactFilter2D();
			filter.useTriggers = false;
			filter.useDepth = false;
			r = new RaycastHit2D[1];
		}

		public MeshCollider Mesh { get; }
		public PolygonCollider2D Poly2D { get; }

		public override bool PushIfInside(ref Vector3 segmentPosition, float segmentRadius, Vector3 segmentOffset)
		{
			if (Is2D == false)
			{
				if (Mesh.convex)
				{
					Vector3 closest;
					var positionOffsetted = segmentPosition + segmentOffset;
					var castMul = 1f;

					closest = Physics.ClosestPoint(
						positionOffsetted, Mesh, Mesh.transform.position, Mesh.transform.rotation);
					if (Vector3.Distance(closest, positionOffsetted) > segmentRadius * 1.01f) return false;

					var dir = closest - positionOffsetted;
					if (dir == Vector3.zero) return false;

					RaycastHit meshHit;
					Mesh.Raycast(new Ray(positionOffsetted, dir.normalized), out meshHit, segmentRadius * castMul);

					if (meshHit.transform)
					{
						segmentPosition = meshHit.point + meshHit.normal * segmentRadius;
						return true;
					}
				}
				else
				{
					Vector3 closest;
					var plus = 0f;

					var positionOffsetted = segmentPosition + segmentOffset;

					closest = Mesh.ClosestPointOnBounds(positionOffsetted);
					plus = (closest - Mesh.transform.position).magnitude;

					var inside = false;
					var insideMul = 1f;

					if (closest == positionOffsetted)
					{
						inside = true;
						insideMul = 7f;
						closest = Mesh.transform.position;
					}

					var targeting = closest - positionOffsetted;
					var rayDirection = targeting.normalized;
					var rayOrigin = positionOffsetted -
									rayDirection * (segmentRadius * 2f + Mesh.bounds.extents.magnitude);

					var rayDistance = targeting.magnitude + segmentRadius * 2f + plus + Mesh.bounds.extents.magnitude;

					if ((positionOffsetted - closest).magnitude < segmentRadius * insideMul)
					{
						var ray = new Ray(rayOrigin, rayDirection);

						RaycastHit hit;
						if (Mesh.Raycast(ray, out hit, rayDistance))
						{
							var hitToPointDist = (positionOffsetted - hit.point).magnitude;

							if (hitToPointDist < segmentRadius * insideMul)
							{
								var toNormal = hit.point - positionOffsetted;
								Vector3 pushNormal;

								if (inside) pushNormal = toNormal + toNormal.normalized * segmentRadius;
								else pushNormal = toNormal - toNormal.normalized * segmentRadius;

								var dot = Vector3.Dot((hit.point - positionOffsetted).normalized, rayDirection);
								if (inside && dot > 0f) pushNormal = toNormal - toNormal.normalized * segmentRadius;

								segmentPosition = segmentPosition + pushNormal;

								return true;
							}
						}
					}

					return false;
				}
			}
			else
			{
#if UNITY_2019_1_OR_NEWER
				Vector2 positionOffsetted = segmentPosition + segmentOffset;
				Vector2 closest;

				if (Poly2D.OverlapPoint(positionOffsetted))
				{
					// Collider inside polygon collider!
					var indir = Poly2D.bounds.center - (Vector3) positionOffsetted;
					indir.z = 0f;
					var r = new Ray(Poly2D.bounds.center - indir * Poly2D.bounds.max.magnitude, indir);
					var dist = 0f;
					Poly2D.bounds.IntersectRay(r, out dist); // We've got partially correct point
					if (dist > 0f)
						closest = Poly2D.ClosestPoint(r.GetPoint(dist));
					else
						closest = Poly2D.ClosestPoint(positionOffsetted);
				}
				else
				{
					closest = Poly2D.ClosestPoint(positionOffsetted);
				}

				var dir = (closest - positionOffsetted).normalized;
				var hits = Physics2D.Raycast(positionOffsetted, dir, filter, r, segmentRadius);

				if (hits > 0)
					if (r[0].transform == Transform)
					{
						segmentPosition = closest + r[0].normal * segmentRadius;
						return true;
					}
#else
                return false;
#endif
			}

			return false;
		}


		public static void PushOutFromMeshCollider(
			MeshCollider mesh, Collision collision, float segmentColliderRadius, ref Vector3 pos)
		{
			var collisionPoint = collision.contacts[0].point;
			var pushNormal = collision.contacts[0].normal;

			RaycastHit info;
			// Doing cheap mesh raycast from outside to hit surface
			if (mesh.Raycast(
					new Ray(pos + pushNormal * segmentColliderRadius * 2f, -pushNormal), out info,
					segmentColliderRadius * 5))
			{
				pushNormal = info.point - pos;
				var pushMagn = pushNormal.sqrMagnitude;
				if (pushMagn > 0 && pushMagn < segmentColliderRadius * segmentColliderRadius)
					pos = info.point - pushNormal * (segmentColliderRadius / Mathf.Sqrt(pushMagn)) * 0.9f;
			}
			else
			{
				pushNormal = collisionPoint - pos;
				var pushMagn = pushNormal.sqrMagnitude;
				if (pushMagn > 0 && pushMagn < segmentColliderRadius * segmentColliderRadius)
					pos = collisionPoint - pushNormal * (segmentColliderRadius / Mathf.Sqrt(pushMagn)) * 0.9f;
			}
		}


		public static void PushOutFromMesh(MeshCollider mesh, Collision collision, float pointRadius, ref Vector3 point)
		{
			Vector3 closest;
			var plus = 0f;

			closest = mesh.ClosestPointOnBounds(point);
			plus = (closest - mesh.transform.position).magnitude;

			var inside = false;
			var insideMul = 1f;

			if (closest == point)
			{
				inside = true;
				insideMul = 7f;
				closest = mesh.transform.position;
			}

			var targeting = closest - point;
			var rayDirection = targeting.normalized;
			var rayOrigin = point - rayDirection * (pointRadius * 2f + mesh.bounds.extents.magnitude);

			var rayDistance = targeting.magnitude + pointRadius * 2f + plus + mesh.bounds.extents.magnitude;

			if ((point - closest).magnitude < pointRadius * insideMul)
			{
				Vector3 collisionPoint;

				if (!inside)
				{
					collisionPoint = collision.contacts[0].point;
				}
				else
				{
					var ray = new Ray(rayOrigin, rayDirection);
					RaycastHit hit;
					if (mesh.Raycast(ray, out hit, rayDistance)) collisionPoint = hit.point;
					else collisionPoint = collision.contacts[0].point;
				}

				var hitToPointDist = (point - collisionPoint).magnitude;

				if (hitToPointDist < pointRadius * insideMul)
				{
					var toNormal = collisionPoint - point;
					Vector3 pushNormal;

					if (inside) pushNormal = toNormal + toNormal.normalized * pointRadius;
					else pushNormal = toNormal - toNormal.normalized * pointRadius;

					var dot = Vector3.Dot((collisionPoint - point).normalized, rayDirection);
					if (inside && dot > 0f) pushNormal = toNormal - toNormal.normalized * pointRadius;

					point = point + pushNormal;
				}
			}
		}
	}
}
