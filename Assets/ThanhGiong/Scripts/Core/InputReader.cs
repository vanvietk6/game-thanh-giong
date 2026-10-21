using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace ThanhGiong
{
    /// <summary>
    /// Đọc điều khiển từ bàn phím, chuột và tay cầm bằng Input System mới của Unity.
    /// Gom về một chỗ để các lớp khác không phụ thuộc vào thiết bị cụ thể.
    /// </summary>
    public static class InputReader
    {
        static bool Key(Key k)
        {
            var kb = Keyboard.current;
            return kb != null && kb[k].wasPressedThisFrame;
        }

        static bool Pad(System.Func<Gamepad, ButtonControl> pick)
        {
            var gp = Gamepad.current;
            return gp != null && pick(gp).wasPressedThisFrame;
        }

        public static Vector2 Move
        {
            get
            {
                Vector2 v = Vector2.zero;
                var kb = Keyboard.current;
                if (kb != null)
                {
                    if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v.y += 1f;
                    if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v.y -= 1f;
                    if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) v.x += 1f;
                    if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) v.x -= 1f;
                }
                var gp = Gamepad.current;
                if (gp != null)
                {
                    Vector2 s = gp.leftStick.ReadValue();
                    if (s.sqrMagnitude > 0.04f) v += s;
                }
                return Vector2.ClampMagnitude(v, 1f);
            }
        }

        public static bool AttackMouse => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        public static bool AttackKey => Key(UnityEngine.InputSystem.Key.J) || Pad(g => g.buttonWest);
        public static bool FireMouse => Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
        public static bool FireKey => Key(UnityEngine.InputSystem.Key.K) || Pad(g => g.buttonNorth);
        public static bool Dash => Key(UnityEngine.InputSystem.Key.Space) || Key(UnityEngine.InputSystem.Key.LeftShift) || Pad(g => g.buttonSouth);
        public static bool Interact => Key(UnityEngine.InputSystem.Key.E) || Pad(g => g.buttonEast);

        public static bool Pause => Key(UnityEngine.InputSystem.Key.Escape) || Key(UnityEngine.InputSystem.Key.P) || Pad(g => g.startButton);
        public static bool Cancel => Key(UnityEngine.InputSystem.Key.Escape) || Pad(g => g.buttonEast);
        public static bool Confirm => Key(UnityEngine.InputSystem.Key.Enter) || Key(UnityEngine.InputSystem.Key.NumpadEnter)
                                      || Key(UnityEngine.InputSystem.Key.Space) || Pad(g => g.buttonSouth);
        public static bool Click => AttackMouse;
        public static bool Restart => Key(UnityEngine.InputSystem.Key.R);
        public static bool MenuKey => Key(UnityEngine.InputSystem.Key.M);
        public static bool Help => Key(UnityEngine.InputSystem.Key.H);

        public static bool Number(int n)
        {
            return n switch
            {
                1 => Key(UnityEngine.InputSystem.Key.Digit1) || Key(UnityEngine.InputSystem.Key.Numpad1) || Pad(g => g.buttonWest),
                2 => Key(UnityEngine.InputSystem.Key.Digit2) || Key(UnityEngine.InputSystem.Key.Numpad2) || Pad(g => g.buttonNorth),
                3 => Key(UnityEngine.InputSystem.Key.Digit3) || Key(UnityEngine.InputSystem.Key.Numpad3) || Pad(g => g.buttonEast),
                _ => false
            };
        }

        /// <summary>Giao điểm giữa tia từ con trỏ chuột và mặt đất (y = 0).</summary>
        public static bool TryGetMouseGround(Camera cam, out Vector3 point)
        {
            point = Vector3.zero;
            if (Mouse.current == null || cam == null) return false;
            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (!plane.Raycast(ray, out float dist)) return false;
            point = ray.GetPoint(dist);
            return true;
        }
    }
}
