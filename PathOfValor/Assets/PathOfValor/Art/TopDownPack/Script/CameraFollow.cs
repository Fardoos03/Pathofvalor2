using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cainos.PixelArtTopDown_Basic
{
    //let camera follow target
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;

        private float zOffset;

        private void Start()
        {
            if (target == null) return;

            CacheOffset();
            SnapToTarget();
        }

        private void LateUpdate()
        {
            if (target == null) return;

            transform.position = GetTargetPosition();
        }

        public void SetTarget(Transform newTarget, bool snapImmediately = false)
        {
            target = newTarget;
            if (target == null) return;

            CacheOffset();
            if (snapImmediately)
            {
                SnapToTarget();
            }
        }

        private void CacheOffset()
        {
            zOffset = transform.position.z - target.position.z;
        }

        private Vector3 GetTargetPosition()
        {
            var targetPosition = target.position;
            targetPosition.z += zOffset;
            return targetPosition;
        }

        private void SnapToTarget()
        {
            transform.position = GetTargetPosition();
        }

    }
}
