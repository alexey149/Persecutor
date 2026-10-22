using UnityEngine;

namespace Persecutor
{
    /// <summary>
    /// Процедурная анимация для моделей Floreswa (своих анимаций в них нет): стойка, ходьба и прыжок.
    /// Вешается на модель. Скорость берётся из перемещения модели, поэтому скрипт одинаково работает у игрока и бота.
    /// Кости ищутся по именам скелета: spine, thigh.L, shin.L, upper_arm.L, forearm.L и т. д.
    /// </summary>
    public class CharacterAnimator : MonoBehaviour
    {
        public float fullSpeed = 5f;      // скорость, при которой шаг максимальный, м/с
        public float stepLength = 1.1f;   // длина шага, м: чем больше, тем реже шаги
        public float legSwing = 25f;      // размах ног, град
        public float kneeBend = 55f;      // сгиб колена, град
        public float armSwing = 30f;      // размах рук, град
        public float armsDown = 25f;      // насколько опустить руки из A-позы модели, град

        // Кости скелета: таз, грудь, бёдра, голени, плечи, предплечья.
        Transform hips, chest, thighL, thighR, shinL, shinR, armL, armR, forearmL, forearmR;
        Transform[] bones;                 // все кости, которые анимируем
        Quaternion[] restPose;             // их исходные повороты (поза модели)
        Vector3 hipsRestPosition;          // исходное положение таза
        Vector3 lastPosition;              // положение модели в прошлом кадре — для расчёта скорости
        float legLength;                   // длина ноги: от бедра до земли
        float phase;                       // фаза шага: полный цикл (два шага) — 2π
        float walk;                        // насколько персонаж идёт: 0 — стоит, 1 — идёт в полную силу
        float air;                         // насколько персонаж в воздухе: 0 — на земле, 1 — в прыжке

        void Start()
        {
            // Ищем кости по именам.
            hips = FindBone("spine");
            chest = FindBone("spine.003");
            thighL = FindBone("thigh.L");
            thighR = FindBone("thigh.R");
            shinL = FindBone("shin.L");
            shinR = FindBone("shin.R");
            armL = FindBone("upper_arm.L");
            armR = FindBone("upper_arm.R");
            forearmL = FindBone("forearm.L");
            forearmR = FindBone("forearm.R");

            // Если какой-то кости нет (другая модель) — анимацию выключаем.
            bones = new[] { hips, chest, thighL, thighR, shinL, shinR, armL, armR, forearmL, forearmR };
            if (System.Array.IndexOf(bones, null) >= 0)
            {
                Debug.LogWarning($"[CharacterAnimator] У модели {name} нет нужных костей — анимация выключена.", this);
                enabled = false;
                return;
            }

            // Запоминаем исходную позу, чтобы каждый кадр начинать с неё.
            restPose = new Quaternion[bones.Length];
            for (int i = 0; i < bones.Length; i++)
                restPose[i] = bones[i].localRotation;
            hipsRestPosition = hips.localPosition;
            legLength = thighL.position.y - transform.position.y;
            lastPosition = transform.position;
        }

        // LateUpdate — после того как игрок и бот уже передвинулись в этом кадре.
        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f)
                return; // пауза

            // 1. Скорость по горизонтали и «на земле ли» (луч вниз от ног).
            Vector3 delta = transform.position - lastPosition;
            lastPosition = transform.position;
            float speed = new Vector2(delta.x, delta.z).magnitude / dt;
            bool grounded = Physics.Raycast(transform.position + Vector3.up * 0.3f, Vector3.down, 0.8f);

            // 2. Плавно переходим между стойкой, ходьбой и прыжком.
            walk = Mathf.MoveTowards(walk, Mathf.Clamp01(speed / fullSpeed), dt * 4f);
            air = Mathf.MoveTowards(air, grounded ? 0f : 1f, dt * 6f);
            phase += speed / stepLength * Mathf.PI * dt; // чем быстрее идём, тем чаще шаги

            // 3. Каждый кадр начинаем с исходной позы модели.
            for (int i = 0; i < bones.Length; i++)
                bones[i].localRotation = restPose[i];
            hips.localPosition = hipsRestPosition;

            float swing = Mathf.Sin(phase);   // качание ног и рук: от -1 до 1
            float lift = Mathf.Cos(phase);    // > 0 — левая нога идёт вперёд, < 0 — правая
            float walking = walk * (1f - air); // в воздухе ходьбу не показываем
            float idle = 1f - walk;            // насколько персонаж стоит на месте

            // 4. Таз опускается, когда ноги расставлены, — чтобы обе стопы оставались на земле.
            float legAngle = legSwing * Mathf.Abs(swing) * walking * Mathf.Deg2Rad;
            hips.position += Vector3.down * (legLength * (1f - Mathf.Cos(legAngle)));

            // 5. Корпус: наклон вперёд на ходу, «дыхание» стоя.
            Pitch(chest, 6f * walking + Mathf.Sin(Time.time * 2f) * 1.5f * idle);

            // 6. Ноги: бедро качается вперёд-назад, колено сгибается, когда нога идёт вперёд.
            //    В прыжке левая нога поджата сильнее правой.
            Pitch(thighL, -legSwing * swing * walking - 40f * air);
            Pitch(thighR, legSwing * swing * walking + 10f * air);
            Pitch(shinL, (kneeBend * Mathf.Max(0f, lift) + 5f) * walking + 70f * air);
            Pitch(shinR, (kneeBend * Mathf.Max(0f, -lift) + 5f) * walking + 30f * air);

            // 7. Руки: опущены вдоль тела и качаются против ног, локти слегка согнуты.
            //    В прыжке руки разведены в стороны.
            float armsRoll = armsDown - 70f * air;
            Roll(armL, armsRoll);
            Roll(armR, -armsRoll);
            Pitch(armL, armSwing * swing * walking - 20f * air);
            Pitch(armR, -armSwing * swing * walking - 20f * air);
            Pitch(forearmL, -20f * walking);
            Pitch(forearmR, -20f * walking);
        }

        /// <summary>
        /// Поворот кости вокруг оси «вправо» персонажа (наклон вперёд-назад).
        /// Для ног и рук минус — конец кости уходит вперёд, для корпуса плюс — наклон вперёд.
        /// </summary>
        void Pitch(Transform bone, float angle)
        {
            bone.rotation = Quaternion.AngleAxis(angle, transform.right) * bone.rotation;
        }

        /// <summary>Поворот кости вокруг оси «вперёд» персонажа: опустить или поднять руку в сторону.</summary>
        void Roll(Transform bone, float angle)
        {
            bone.rotation = Quaternion.AngleAxis(angle, transform.forward) * bone.rotation;
        }

        /// <summary>Ищет кость с нужным именем среди всех дочерних объектов модели.</summary>
        Transform FindBone(string boneName)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>())
                if (child.name == boneName)
                    return child;
            return null;
        }
    }
}
