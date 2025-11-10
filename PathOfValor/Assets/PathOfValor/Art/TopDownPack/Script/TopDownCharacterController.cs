using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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
            Vector2 input = ReadMovementInput();
            bool moving = input.sqrMagnitude > 0.01f;

            animator.SetBool(IsMoving, moving);
            if (moving)
            {
                var directionIndex = GetDirectionIndex(input);
                animator.SetInteger(Direction, directionIndex);
                UpdateFacing(directionIndex);
            }

            if (body != null)
            {
                Vector2 velocity = moving ? input.normalized * speed : Vector2.zero;
#if UNITY_2022_2_OR_NEWER
                body.linearVelocity = velocity;
#else
                body.velocity = velocity;
#endif
            }
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

        private Vector2 ReadMovementInput()
        {
            Vector2 input = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            var enhancedInput = ReadInputSystemVector();
            if (enhancedInput.sqrMagnitude > 0.0001f)
            {
                input = enhancedInput;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (input.sqrMagnitude <= 0.0001f)
            {
                input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            }
#endif

            return input;
        }

#if ENABLE_INPUT_SYSTEM
        private static Vector2 ReadInputSystemVector()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                Vector2 input = Vector2.zero;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                {
                    input.y += 1f;
                }

                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                {
                    input.y -= 1f;
                }

                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                {
                    input.x += 1f;
                }

                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                {
                    input.x -= 1f;
                }

                if (input.sqrMagnitude > 0.0001f)
                {
                    return Vector2.ClampMagnitude(input, 1f);
                }
            }

            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                var stick = gamepad.leftStick.ReadValue();
                if (stick.sqrMagnitude > 0.0001f)
                {
                    return stick;
                }
            }

            return Vector2.zero;
        }
#endif
    }
}
