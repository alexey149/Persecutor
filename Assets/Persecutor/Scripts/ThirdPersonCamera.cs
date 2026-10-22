using UnityEngine;
using UnityEngine.InputSystem;

namespace Persecutor
{
    /// <summary>
    /// Камера от третьего лица: мышь вращает её вокруг цели.
    /// Esc — освободить курсор, клик в окне игры — снова захватить.
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        public Transform target;                                 // за кем следит камера
        public Vector3 pivotOffset = new Vector3(0f, 2f, 0f);    // точка вращения относительно цели (примерно голова)
        public float distance = 8f;                              // расстояние от точки вращения до камеры
        public float sensitivity = 0.15f;                        // чувствительность мыши, град на пиксель
        public float minPitch = -10f;                            // самый низкий наклон камеры, град
        public float maxPitch = 70f;                             // самый высокий наклон камеры, град

        InputAction lookAction;   // действие «Look» (мышь / правый стик)
        float yaw;                // поворот камеры по горизонтали
        float pitch = 20f;        // наклон камеры по вертикали

        void Start()
        {
            lookAction = InputSystem.actions.FindAction("Look");
            yaw = target.eulerAngles.y;               // в начале камера стоит за спиной игрока
            Cursor.lockState = CursorLockMode.Locked; // прячем и фиксируем курсор
        }

        void LateUpdate()
        {
            // Esc — отпустить курсор, клик — снова захватить.
            if (Keyboard.current.escapeKey.wasPressedThisFrame) Cursor.lockState = CursorLockMode.None;
            if (Mouse.current.leftButton.wasPressedThisFrame) Cursor.lockState = CursorLockMode.Locked;

            // Вращаем камеру мышью, только когда курсор захвачен.
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 look = lookAction.ReadValue<Vector2>() * sensitivity;
                yaw += look.x;
                pitch = Mathf.Clamp(pitch - look.y, minPitch, maxPitch);
            }

            // Ставим камеру на расстоянии distance позади точки вращения и смотрим на неё.
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.SetPositionAndRotation(target.position + pivotOffset - rotation * Vector3.forward * distance, rotation);
        }
    }
}
