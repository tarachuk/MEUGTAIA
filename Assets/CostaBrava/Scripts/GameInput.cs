using UnityEngine;
using UnityEngine.InputSystem;

namespace CostaBrava
{
    public static class GameInput
    {
        public static bool Held(Key key) => Keyboard.current != null && Keyboard.current[key].isPressed;
        public static bool Pressed(Key key) => Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;
        public static Vector2 Move => new Vector2((Held(Key.D)?1:0)-(Held(Key.A)?1:0), (Held(Key.W)?1:0)-(Held(Key.S)?1:0)).normalized;
        public static Vector2 Look => Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
        public static bool Aim => Mouse.current != null && Mouse.current.rightButton.isPressed;
        public static bool Fire => Mouse.current != null && Mouse.current.leftButton.isPressed;
        public static bool FirePressed => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    }
}
