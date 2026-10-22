using System.Collections.Generic;
using UnityEngine;

namespace Persecutor
{
    /// <summary>
    /// Бот-преследователь на Rigidbody.
    /// Идёт по следу игрока, держит дистанцию и всегда медленнее его.
    /// Высоту ступеньки, угол подъёма и высоту прыжка берёт у игрока, поэтому препятствия они проходят одинаково.
    ///
    /// Как бот проходит ступеньки: низ капсулы поднят на высоту ступеньки, поэтому невысокие препятствия
    /// её не задевают, а луч вниз находит землю и держит ноги на её уровне — на ступеньке, подъёме и спуске.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class BotChaser : MonoBehaviour
    {
        public PlayerMovement player;            // кого преследовать (если пусто — найдётся сам)
        [Range(0.1f, 0.95f)]
        public float speedRatio = 0.7f;          // скорость бота = скорость игрока × speedRatio
        public float stopDistance = 3f;          // ближе этого расстояния бот не подходит
        public float resumeDistance = 4f;        // снова идёт, когда игрок отошёл дальше
        public float turnSpeed = 540f;           // скорость поворота, град/с

        Rigidbody body;                          // физическое тело бота
        CapsuleCollider capsule;                 // его коллайдер
        readonly Queue<Vector3> trail = new Queue<Vector3>(); // след игрока: точки, по которым идёт бот
        Vector3 lastTrailPoint;                  // последняя записанная точка следа

        // Параметры, которые копируются у игрока при старте.
        float speed, stepHeight, slopeLimit, jumpHeight, gravity;

        bool chasing = true;   // идёт ли бот за игроком сейчас
        bool grounded;         // стоит ли на земле
        bool jumping;          // летит ли вверх после прыжка

        void Start()
        {
            if (player == null)
                player = FindAnyObjectByType<PlayerMovement>();

            // Всё как у игрока, кроме скорости — она меньше.
            CharacterController cc = player.GetComponent<CharacterController>();
            speed = player.moveSpeed * speedRatio;
            stepHeight = cc.stepOffset;
            slopeLimit = cc.slopeLimit;
            jumpHeight = player.jumpHeight;
            gravity = player.gravity;

            // Настраиваем Rigidbody.
            body = GetComponent<Rigidbody>();
            body.useGravity = false;   // гравитацию считаем сами — стоя на склоне, бот не сползает
            body.freezeRotation = true; // физика не опрокидывает бота, поворачиваем его только мы
            body.interpolation = RigidbodyInterpolation.Interpolate; // плавное движение между шагами физики

            // Поднимаем низ капсулы на высоту ступеньки (верх остаётся на месте)
            // и убираем трение, чтобы бот не цеплялся за стены.
            capsule = GetComponent<CapsuleCollider>();
            float top = capsule.center.y + capsule.height / 2f;
            capsule.height = top - stepHeight;
            capsule.center = new Vector3(0f, stepHeight + capsule.height / 2f, 0f);
            capsule.sharedMaterial = new PhysicsMaterial { dynamicFriction = 0f, staticFriction = 0f, frictionCombine = PhysicsMaterialCombine.Minimum };
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            Vector3 feet = body.position; // точка у ног бота

            // 1. Дистанция: ближе stopDistance — стоим, дальше resumeDistance — снова идём.
            float distance = Vector3.Distance(feet, player.transform.position);
            if (distance <= stopDistance) chasing = false;
            else if (distance >= resumeDistance) chasing = true;

            // 2. Идём по следу игрока — там же, где прошёл он: по подъёму, через ступеньку, через барьер.
            RecordTrail();
            Vector3 move = chasing ? DirectionAlongTrail(feet) * speed : Vector3.zero;

            // 3. Ищем землю. Если стояли на ней — ищем глубже, на высоту ступеньки:
            //    так бот не отрывается от земли на спусках и при шаге вниз.
            Vector3 velocity = body.linearVelocity;
            if (jumping && velocity.y <= 0f) jumping = false; // верхняя точка прыжка пройдена
            bool groundBelow = FindGround(feet, grounded ? stepHeight : 0.05f, out RaycastHit ground);
            grounded = groundBelow && !jumping;

            if (grounded)
            {
                // На земле: идём вдоль склона и подтягиваем ноги к её уровню.
                Vector3 n = ground.normal;
                float alongSlope = -(n.x * move.x + n.z * move.z) / n.y;   // вверх/вниз вдоль подъёма или спуска
                float toGround = (ground.point.y - feet.y) * 0.5f / dt;    // плавно поднимаемся на ступеньку или опускаемся с неё
                velocity = new Vector3(move.x, alongSlope + toGround, move.z);

                // Впереди препятствие, которое можно перепрыгнуть, — прыгаем.
                if (move != Vector3.zero && ShouldJump(feet, move.normalized))
                {
                    velocity.y = Mathf.Sqrt(-2f * gravity * jumpHeight);
                    jumping = true;
                }
            }
            else
            {
                // В воздухе: продолжаем идти по горизонтали и падаем с ускорением.
                velocity = new Vector3(move.x, velocity.y + gravity * dt, move.z);
            }

            body.linearVelocity = velocity;

            // 4. Поворот: на ходу — по направлению движения, стоя — лицом к игроку.
            Vector3 look = move != Vector3.zero ? move : Flat(player.transform.position - feet);
            if (look.sqrMagnitude > 0.001f)
                body.MoveRotation(Quaternion.RotateTowards(body.rotation, Quaternion.LookRotation(look), turnSpeed * dt));
        }

        /// <summary>
        /// Луч вниз изнутри капсулы. Земля считается найденной, если она не выше ног больше чем на высоту ступеньки,
        /// не ниже ног больше чем на depth и не круче slopeLimit.
        /// </summary>
        bool FindGround(Vector3 feet, float depth, out RaycastHit hit)
        {
            float start = stepHeight + 0.05f; // начинаем чуть выше максимальной ступеньки
            return Physics.Raycast(feet + Vector3.up * start, Vector3.down, out hit, start + depth, ~0, QueryTriggerInteraction.Ignore)
                && Vector3.Angle(hit.normal, Vector3.up) <= slopeLimit;
        }

        /// <summary>
        /// Нужно ли прыгать: впереди стенка выше ступеньки, но ниже высоты прыжка
        /// (то есть игрок её тоже перепрыгнул бы).
        /// </summary>
        bool ShouldJump(Vector3 feet, Vector3 direction)
        {
            float reach = capsule.radius + 0.5f; // как далеко вперёд смотрим

            // Луч чуть выше ступеньки: упёрся в стенку (не в склон и не в игрока)…
            return Physics.Raycast(feet + Vector3.up * (stepHeight + 0.05f), direction, out RaycastHit wall, reach, ~0, QueryTriggerInteraction.Ignore)
                && Vector3.Angle(wall.normal, Vector3.up) > slopeLimit
                && !wall.transform.IsChildOf(player.transform)
                // …а луч на высоте прыжка свободен — значит, стенку можно перепрыгнуть.
                && !Physics.Raycast(feet + Vector3.up * jumpHeight, direction, reach + 0.5f, ~0, QueryTriggerInteraction.Ignore);
        }

        /// <summary>Записываем путь игрока: новая точка каждые 0,5 м. Пока бот стоит, след не нужен.</summary>
        void RecordTrail()
        {
            if (!chasing)
            {
                trail.Clear();
                return;
            }

            Vector3 point = player.transform.position;
            if (trail.Count == 0 || (point - lastTrailPoint).sqrMagnitude > 0.25f) // 0,25 = 0,5²
            {
                trail.Enqueue(point);
                lastTrailPoint = point;
            }
        }

        /// <summary>Направление к ближайшей непройденной точке следа (или прямо к игроку, если след пуст).</summary>
        Vector3 DirectionAlongTrail(Vector3 feet)
        {
            // Выбрасываем точки, до которых уже дошли (ближе 0,5 м).
            while (trail.Count > 0 && Flat(trail.Peek() - feet).sqrMagnitude < 0.25f)
                trail.Dequeue();

            Vector3 goal = trail.Count > 0 ? trail.Peek() : player.transform.position;
            Vector3 direction = Flat(goal - feet);
            return direction.sqrMagnitude > 0.01f ? direction.normalized : Vector3.zero;
        }

        /// <summary>Тот же вектор, но без вертикальной составляющей.</summary>
        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
