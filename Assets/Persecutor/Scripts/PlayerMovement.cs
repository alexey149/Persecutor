using UnityEngine;
using UnityEngine.InputSystem;

namespace Persecutor
{
    /// <summary>
    /// Передвижение игрока на CharacterController.
    /// WASD — ходьба относительно камеры, Space — прыжок.
    /// Ступеньки (stepOffset) и подъёмы (slopeLimit) CharacterController проходит сам.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        public float moveSpeed = 5f;      // скорость ходьбы, м/с (бот всегда медленнее)
        public float jumpHeight = 1.2f;   // высота прыжка, м
        public float gravity = -20f;      // гравитация, м/с²
        public float turnSpeed = 720f;    // скорость поворота в сторону движения, град/с

        CharacterController controller;   // компонент, который двигает игрока
        InputAction moveAction;           // действие «Move» (WASD / стик)
        InputAction jumpAction;           // действие «Jump» (Space / кнопка геймпада)
        float verticalSpeed;              // текущая вертикальная скорость (прыжок и падение)

        void Awake()
        {
            controller = GetComponent<CharacterController>();

            // Берём действия из настроек ввода проекта (Assets/InputSystem_Actions).
            moveAction = InputSystem.actions.FindAction("Move");
            jumpAction = InputSystem.actions.FindAction("Jump");
        }

        void Update()
        {
            // 1. Направление движения относительно камеры: «вперёд» — туда, куда смотрит камера.
            Transform cam = Camera.main.transform;
            Vector3 forward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized; // вперёд по земле
            Vector3 right = Vector3.Cross(Vector3.up, forward);                           // вправо по земле
            Vector2 input = moveAction.ReadValue<Vector2>();
            Vector3 move = Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f) * moveSpeed;

            // 2. Прыжок и гравитация.
            if (controller.isGrounded)
            {
                // Стоим на земле — прижимаем к ней со скоростью не меньше горизонтальной,
                // чтобы на спусках персонаж шёл по склону, а не «подпрыгивал».
                verticalSpeed = -moveSpeed;

                // Нажали прыжок — даём скорость вверх, которой хватит ровно на jumpHeight.
                if (jumpAction.WasPressedThisFrame())
                    verticalSpeed = Mathf.Sqrt(-2f * gravity * jumpHeight);
            }
            else
            {
                // В воздухе — падаем с ускорением.
                verticalSpeed += gravity * Time.deltaTime;
            }

            // 3. Двигаем персонажа. Ступеньки и склоны CharacterController обрабатывает сам.
            controller.Move((move + Vector3.up * verticalSpeed) * Time.deltaTime);

            // 4. Плавно поворачиваемся в сторону движения.
            if (move.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(move), turnSpeed * Time.deltaTime);
        }
    }
}
