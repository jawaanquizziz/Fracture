using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Valmon.Core
{
    /// <summary>
    /// Unified input handler supporting both the new Unity Input System (com.unity.inputsystem)
    /// and the legacy Input Manager without throwing InvalidOperationException under any player settings.
    /// </summary>
    public static class ValmonInput
    {
        public static float GetHorizontal()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                float val = 0f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) val += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) val -= 1f;
                if (val != 0f) return val;
            }
#endif
            try
            {
                return Input.GetAxisRaw("Horizontal");
            }
            catch
            {
                return 0f;
            }
        }

        public static float GetVertical()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                float val = 0f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) val += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) val -= 1f;
                if (val != 0f) return val;
            }
#endif
            try
            {
                return Input.GetAxisRaw("Vertical");
            }
            catch
            {
                return 0f;
            }
        }

        public static bool IsSprinting()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed) return true;
            }
#endif
            try
            {
                return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            }
            catch
            {
                return false;
            }
        }

        public static bool GetJumpDown()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.spaceKey.wasPressedThisFrame) return true;
            }
#endif
            try
            {
                return Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space);
            }
            catch
            {
                return false;
            }
        }

        public static float GetMouseX()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse != null)
            {
                float deltaX = mouse.delta.x.ReadValue();
                if (Mathf.Abs(deltaX) > 0.001f) return deltaX * 0.1f;
            }
#endif
            try
            {
                return Input.GetAxis("Mouse X");
            }
            catch
            {
                return 0f;
            }
        }

        public static float GetMouseY()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse != null)
            {
                float deltaY = mouse.delta.y.ReadValue();
                if (Mathf.Abs(deltaY) > 0.001f) return deltaY * 0.1f;
            }
#endif
            try
            {
                return Input.GetAxis("Mouse Y");
            }
            catch
            {
                return 0f;
            }
        }

        public static float GetMouseScroll()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse != null)
            {
                float scroll = mouse.scroll.y.ReadValue();
                if (Mathf.Abs(scroll) > 0.01f) return Mathf.Clamp(scroll * 0.05f, -1f, 1f);
            }
#endif
            try
            {
                return Input.GetAxis("Mouse ScrollWheel");
            }
            catch
            {
                return 0f;
            }
        }

        public static Vector3 GetMousePosition()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 pos = mouse.position.ReadValue();
                return new Vector3(pos.x, pos.y, 0f);
            }
#endif
            try
            {
                return Input.mousePosition;
            }
            catch
            {
                return new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
            }
        }

        public static bool GetLeftClickDown()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.leftButton.wasPressedThisFrame) return true;
            }
#endif
            try
            {
                return Input.GetMouseButtonDown(0);
            }
            catch
            {
                return false;
            }
        }

        public static bool GetLeftClickHeld()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.leftButton.isPressed) return true;
            }
#endif
            try
            {
                return Input.GetMouseButton(0);
            }
            catch
            {
                return false;
            }
        }

        public static bool GetRightClickDown()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.rightButton.wasPressedThisFrame) return true;
            }
#endif
            try
            {
                return Input.GetMouseButtonDown(1);
            }
            catch
            {
                return false;
            }
        }

        public static bool GetThrowPokeBallDown()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb != null && (kb.eKey.wasPressedThisFrame || kb.fKey.wasPressedThisFrame)) return true;
            if (mouse != null && mouse.rightButton.wasPressedThisFrame) return true;
#endif
            try
            {
                return Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(1);
            }
            catch
            {
                return false;
            }
        }

        public static bool GetTabDown()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.tabKey.wasPressedThisFrame) return true;
#endif
            try
            {
                return Input.GetKeyDown(KeyCode.Tab);
            }
            catch
            {
                return false;
            }
        }

        public static bool GetEscapeDown()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) return true;
#endif
            try
            {
                return Input.GetKeyDown(KeyCode.Escape);
            }
            catch
            {
                return false;
            }
        }

        public static bool GetSpaceDown()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.spaceKey.wasPressedThisFrame) return true;
#endif
            try
            {
                return Input.GetKeyDown(KeyCode.Space);
            }
            catch
            {
                return false;
            }
        }

        public static bool GetKey1Down()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)) return true;
#endif
            try
            {
                return Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1);
            }
            catch
            {
                return false;
            }
        }

        public static bool GetKey2Down()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)) return true;
#endif
            try
            {
                return Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2);
            }
            catch
            {
                return false;
            }
        }

        public static bool GetKey3Down()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame)) return true;
#endif
            try
            {
                return Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3);
            }
            catch
            {
                return false;
            }
        }

        public static bool GetF1Down()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.f1Key.wasPressedThisFrame) return true;
#endif
            try
            {
                return Input.GetKeyDown(KeyCode.F1);
            }
            catch
            {
                return false;
            }
        }

        public static bool GetF2Down()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.f2Key.wasPressedThisFrame) return true;
#endif
            try
            {
                return Input.GetKeyDown(KeyCode.F2);
            }
            catch
            {
                return false;
            }
        }

        public static bool GetF3Down()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.f3Key.wasPressedThisFrame) return true;
#endif
            try
            {
                return Input.GetKeyDown(KeyCode.F3);
            }
            catch
            {
                return false;
            }
        }
    }
}
