using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cainos.PixelArtTopDown_Basic
{
    public class TopDownCharacterController : MonoBehaviour
    {
        [SerializeField]
        private float speed = 5f;

        private Animator animator;
        private Rigidbody2D body;
        private SpriteRenderer spriteRenderer;

        private static readonly int Direction = Animator.StringToHash("Direction");
        private static readonly int IsMoving = Animator.StringToHash("IsMoving");

        private void Awake()
        {
            animator = GetComponent<Animator>();
            body = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        }

        private void Update()
        {
            Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            bool moving = input.sqrMagnitude > 0.01f;

            animator.SetBool(IsMoving, moving);
            if (moving)
            {
                var directionIndex = GetDirectionIndex(input);
                animator.SetInteger(Direction, directionIndex);
                UpdateFacing(directionIndex);
            }

            Vector2 velocity = moving ? input.normalized * speed : Vector2.zero;
            body.linearVelocity = velocity;
        }

        private static int GetDirectionIndex(Vector2 input)
        {
            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            {
                return input.x > 0 ? 2 : 3; // 2 = East, 3 = West
            }

            return input.y > 0 ? 1 : 0; // 1 = North, 0 = South
        }

        private void UpdateFacing(int directionIndex)
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (directionIndex == 2)
            {
                spriteRenderer.flipX = false;
            }
            else if (directionIndex == 3)
            {
                spriteRenderer.flipX = true;
            }
        }
    }
}
