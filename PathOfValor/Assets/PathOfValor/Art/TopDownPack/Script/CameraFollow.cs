using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cainos.PixelArtTopDown_Basic
{
    //let camera follow target
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public float lerpSpeed = 1.0f;

        private Vector3 offset;
        private Vector3 targetPos;
        private bool hasTarget;

        private void Start()
        {
            TryResolveTarget();
        }

        private void Update()
        {
            if (!hasTarget || target == null)
            {
                TryResolveTarget();
                if (!hasTarget || target == null) return;
            }

            targetPos = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, targetPos, lerpSpeed * Time.deltaTime);
        }

        private void TryResolveTarget()
        {
            if (target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player == null) return;
                SetTarget(player.transform, true);
                return;
            }

            if (!hasTarget)
            {
                SetTarget(target, true);
            }
        }

        public void SetTarget(Transform newTarget, bool snapImmediately = false)
        {
            target = newTarget;
            if (target == null)
            {
                hasTarget = false;
                return;
            }

            offset = transform.position - target.position;
            hasTarget = true;

            if (snapImmediately)
            {
                transform.position = target.position + offset;
            }
        }

    }
}
