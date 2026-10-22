using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Persecutor.EditorTools
{
    /// <summary>
    /// Собирает демо-сцену Assets/Scenes/PersecutorDemo.unity из примитивов Unity и моделей Floreswa.
    /// Меню: Tools ▸ Persecutor ▸ Собрать демо-сцену.
    /// Если увеличить Version, сцена один раз пересоберётся сама после компиляции.
    /// </summary>
    [InitializeOnLoad]
    public static class PersecutorSceneBuilder
    {
        const int Version = 2;                                      // версия сцены для автосборки
        const string ScenePath = "Assets/Scenes/PersecutorDemo.unity"; // куда сохранять сцену
        const string Models = "Assets/Floreswa/Prefabs/";            // папка с моделями персонажей
        const string Materials = "Assets/Floreswa/Materials/";       // папка с материалами

        // Статический конструктор вызывается при каждой загрузке скриптов в редакторе.
        static PersecutorSceneBuilder()
        {
            // Ждём, пока редактор закончит загрузку, и один раз собираем сцену новой версии.
            EditorApplication.delayCall += () =>
            {
                string key = "Persecutor.SceneVersion." + Application.dataPath;
                bool alreadyBuilt = EditorPrefs.GetInt(key, 0) >= Version;
                bool busy = EditorApplication.isPlayingOrWillChangePlaymode;      // идёт игра
                bool unsaved = SceneManager.GetActiveScene().isDirty;             // в открытой сцене есть несохранённые изменения
                if (alreadyBuilt || busy || unsaved)
                    return;

                EditorPrefs.SetInt(key, Version);
                Build();
            };
        }

        // Пункт меню для ручной пересборки сцены.
        [MenuItem("Tools/Persecutor/Собрать демо-сцену")]
        static void BuildFromMenu()
        {
            // Сначала предлагаем сохранить изменения в открытой сцене.
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                Build();
        }

        static void Build()
        {
            // Новая сцена с камерой и направленным светом.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // ---------- Уровень: ступенька → подъём 20° → платформа → спуск 30° → барьер для прыжка ----------
            Transform level = new GameObject("Level").transform;
            const float h = 1.6f;                                                  // высота платформы, м
            float upEnd = -1f + h / Mathf.Tan(20f * Mathf.Deg2Rad);                // где заканчивается подъём (по Z)
            float downStart = upEnd + 6f;                                          // платформа длиной 6 м
            float downEnd = downStart + h / Mathf.Tan(30f * Mathf.Deg2Rad);        // где заканчивается спуск

            Box("Ground", level, new Vector3(0f, -1f, 0f), new Vector3(60f, 2f, 60f), "Ground");          // пол, верх на y = 0
            Box("Step 0.3 m", level, new Vector3(0f, 0.15f, -6f), new Vector3(8f, 0.3f, 3f), "Tan");       // небольшая ступенька
            Ramp("Ramp Up 20°", level, new Vector3(0f, 0f, -1f), new Vector3(0f, h, upEnd));               // подъём
            Box("Platform", level, new Vector3(0f, h / 2f, upEnd + 3f), new Vector3(6f, h, 6f), "Gray");   // платформа
            Ramp("Ramp Down 30°", level, new Vector3(0f, h, downStart), new Vector3(0f, 0f, downEnd));     // спуск
            Box("Barrier 0.9 m (jump)", level, new Vector3(0f, 0.45f, 17f), new Vector3(8f, 0.9f, 0.5f), "Red"); // барьер — только перепрыгнуть

            // Ограждение по краю поля (выше прыжка, чтобы не выпрыгнуть).
            Box("Wall N", level, new Vector3(0f, 0.75f, 30f), new Vector3(60f, 1.5f, 0.5f), "Dark Gray");
            Box("Wall S", level, new Vector3(0f, 0.75f, -30f), new Vector3(60f, 1.5f, 0.5f), "Dark Gray");
            Box("Wall E", level, new Vector3(30f, 0.75f, 0f), new Vector3(0.5f, 1.5f, 60f), "Dark Gray");
            Box("Wall W", level, new Vector3(-30f, 0.75f, 0f), new Vector3(0.5f, 1.5f, 60f), "Dark Gray");

            // ---------- Игрок: CharacterController ----------
            GameObject player = Character("Player", "male01_1", new Vector3(0f, 0f, -12f), out float height);
            player.tag = "Player";
            CharacterController cc = player.AddComponent<CharacterController>();
            cc.height = height;                                         // рост капсулы = рост модели
            cc.radius = height * 0.17f;                                 // ширина по пропорциям модели
            cc.skinWidth = 0.08f;
            cc.center = new Vector3(0f, height / 2f + cc.skinWidth, 0f); // капсула приподнята на skinWidth, чтобы ноги стояли на земле
            cc.stepOffset = 0.45f;                                      // максимальная высота ступеньки (общая с ботом)
            cc.slopeLimit = 45f;                                        // максимальный угол подъёма (общий с ботом)
            PlayerMovement movement = player.AddComponent<PlayerMovement>();

            // ---------- Бот: Rigidbody + капсула. Ступеньку, склон и прыжок BotChaser возьмёт у игрока ----------
            GameObject bot = Character("Bot", "male03_2", new Vector3(-5f, 0f, -20f), out height);
            Rigidbody body = bot.AddComponent<Rigidbody>();
            body.mass = 70f;
            body.useGravity = false;                                    // гравитацию считает BotChaser
            body.constraints = RigidbodyConstraints.FreezeRotation;     // не опрокидывается
            body.interpolation = RigidbodyInterpolation.Interpolate;    // плавное движение
            CapsuleCollider capsule = bot.AddComponent<CapsuleCollider>();
            capsule.height = height;
            capsule.radius = height * 0.17f;
            capsule.center = new Vector3(0f, height / 2f, 0f);
            bot.AddComponent<BotChaser>().player = movement;

            // ---------- Камера от третьего лица ----------
            ThirdPersonCamera follow = Camera.main.gameObject.AddComponent<ThirdPersonCamera>();
            follow.target = player.transform;
            follow.pivotOffset = Vector3.up * (cc.height * 0.8f);       // вращаемся вокруг головы игрока
            follow.transform.position = player.transform.position + new Vector3(0f, 4.5f, -8f);
            follow.transform.LookAt(player.transform.position + follow.pivotOffset);

            // Сохраняем сцену и выделяем игрока.
            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeGameObject = player;
            Debug.Log($"[Persecutor] Сцена собрана: {ScenePath}. Play: WASD — ходьба, Space — прыжок, мышь — камера, Esc — курсор.");
        }

        /// <summary>
        /// Создаёт пустой объект-персонаж, кладёт внутрь модель с процедурной анимацией.
        /// В height возвращает рост модели.
        /// </summary>
        static GameObject Character(string name, string model, Vector3 position, out float height)
        {
            GameObject root = new GameObject(name);
            root.transform.position = position;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Models + model + ".prefab");
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
            visual.name = "Model";
            visual.AddComponent<CharacterAnimator>();

            // Рост — от ног до верхней границы модели.
            height = visual.GetComponentInChildren<Renderer>().bounds.max.y - position.y;
            return root;
        }

        /// <summary>Создаёт куб нужного размера с материалом из папки Floreswa.</summary>
        static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, string material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent);
            box.transform.position = position;
            box.transform.localScale = size;
            box.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Materials + material + ".mat");
            box.isStatic = true; // уровень неподвижен
            return box;
        }

        /// <summary>
        /// Наклонная плоскость из толстого куба: верхняя грань идёт от точки from до точки to,
        /// а остальная часть куба уходит в пол и платформу, поэтому сбоку рампа выглядит сплошным клином.
        /// </summary>
        static void Ramp(string name, Transform parent, Vector3 from, Vector3 to)
        {
            const float thickness = 2f;
            Quaternion rotation = Quaternion.LookRotation(to - from);                         // наклон вдоль рампы
            Vector3 center = (from + to) / 2f - rotation * Vector3.up * (thickness / 2f);      // сдвигаем вниз на половину толщины
            Box(name, parent, center, new Vector3(6f, thickness, (to - from).magnitude), "Orange").transform.rotation = rotation;
        }
    }
}
