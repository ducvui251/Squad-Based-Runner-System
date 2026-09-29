using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public static class Level5TankBossBuilder
{
    private const string TankModelPath = "Assets/Models/Objects/Tank.fbx";
    private const string BossPrefabPath = "Assets/Prefabs/Enemies/Level5TankBoss.prefab";
    private const string ShootClipPath = "Assets/Animations/Level5TankBossShoot.anim";
    private const string AnimatorControllerPath = "Assets/Animations/Level5TankBoss.controller";
    private const string MuzzleMaterialPath = "Assets/Materials/Level5TankBossMuzzle.mat";
    private const float FinalGapFarEdgeZ = 390f + BuildLevel5FractureRelay.PostFinalGapShiftZ;
    private const float BossTrackOffsetAfterFinalGapZ = 6.31f;
    private const float BossTrackZ = FinalGapFarEdgeZ + BossTrackOffsetAfterFinalGapZ;
    private const float TankModelScale = 70f;
    private const float TankModelGroundOffset = 0.02f;
    // Keep the near edge 6m beyond the gap; capsule contact then has about 5m of runway.
    private const float BossTriggerCenterY = 2.5f;
    private const float BossTriggerCenterZ = -7f;
    private const float BossTriggerWidth = 9f;
    private const float BossTriggerHeight = 6f;
    private const float BossTriggerDepth = 18f;

    [MenuItem("SpiralSquad/Ensure Level 5 Tank Boss")]
    public static void EnsureInActiveLevel5Scene()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != "Level5")
        {
            throw new System.InvalidOperationException("Open Level5.unity before creating the final tank boss.");
        }

        GameObject[] roots = scene.GetRootGameObjects();
        GameObject levelRoot = null;
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == "Level 5 - Fracture Relay")
            {
                levelRoot = roots[i];
                break;
            }
        }

        if (levelRoot == null)
        {
            throw new System.InvalidOperationException("Level 5 root 'Level 5 - Fracture Relay' was not found.");
        }

        Configure(scene, levelRoot);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    [MenuItem("SpiralSquad/Refresh Level 5 Tank Boss Prefab")]
    public static void RefreshBossPrefabAsset()
    {
        EnsureBossPrefab();
        AssetDatabase.SaveAssets();
    }

    public static void Configure(Scene scene, GameObject levelRoot)
    {
        if (levelRoot == null) throw new System.ArgumentNullException(nameof(levelRoot));

        Transform enemyGroup = levelRoot.transform.Find("EnemyGroups");
        if (enemyGroup == null)
        {
            GameObject group = new GameObject("EnemyGroups");
            group.transform.SetParent(levelRoot.transform, false);
            enemyGroup = group.transform;
        }

        Transform existing = enemyGroup.Find("Final Tank Boss");
        GameObject boss = existing != null ? existing.gameObject : null;
        if (boss == null)
        {
            GameObject prefab = EnsureBossPrefab();
            boss = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (boss == null) throw new System.InvalidOperationException("Could not instantiate the Level 5 tank boss prefab.");
            boss.name = "Final Tank Boss";
            boss.transform.SetParent(enemyGroup, false);
            boss.transform.localPosition = new Vector3(0f, 0f, BossTrackZ);
            boss.transform.localRotation = Quaternion.identity;
            boss.transform.localScale = Vector3.one;
        }

        BoxCollider trigger = boss.GetComponent<BoxCollider>();
        if (trigger == null) throw new System.InvalidOperationException("Final Tank Boss is missing its root BoxCollider trigger.");

        float requiredNearEdgeZ = GetFinalGapFarEdgeZ(levelRoot) + 6f;
        float triggerNearEdgeZ = trigger.bounds.min.z;
        if (triggerNearEdgeZ < requiredNearEdgeZ)
        {
            Undo.RecordObject(trigger, "Move Level 5 tank trigger past final gap");
            Vector3 worldShift = Vector3.forward * (requiredNearEdgeZ - triggerNearEdgeZ);
            trigger.center += trigger.transform.InverseTransformVector(worldShift);
            EditorUtility.SetDirty(trigger);
            if (PrefabUtility.IsPartOfPrefabInstance(trigger))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(trigger);
            }
        }

        EditorUtility.SetDirty(boss);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static float GetFinalGapFarEdgeZ(GameObject levelRoot)
    {
        Transform track = levelRoot.transform.Find("Track");
        if (track != null)
        {
            Transform road = track.Find("Road Segment 399.13-429.13");
            if (road == null) road = track.Find("Road Segment 390-420");
            Collider roadCollider = road != null ? road.GetComponent<Collider>() : null;
            if (roadCollider != null && roadCollider.enabled && roadCollider.gameObject.activeInHierarchy)
            {
                return roadCollider.bounds.min.z;
            }
        }

        return FinalGapFarEdgeZ;
    }

    private static GameObject EnsureBossPrefab()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
        if (existing != null)
        {
            NormalizeExistingPrefabSettings();
            return AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
        }

        GameObject tankModel = AssetDatabase.LoadAssetAtPath<GameObject>(TankModelPath);
        if (tankModel == null) throw new System.InvalidOperationException("Tank model not found at " + TankModelPath + ".");

        EnsureFolder("Assets/Prefabs/Enemies");
        EnsureFolder("Assets/Animations");
        GameObject root = new GameObject("Level5TankBoss");
        try
        {
            Rigidbody body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeAll;

            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, BossTriggerCenterY, BossTriggerCenterZ);
            trigger.size = new Vector3(BossTriggerWidth, BossTriggerHeight, BossTriggerDepth);

            GameObject model = PrefabUtility.InstantiatePrefab(tankModel) as GameObject;
            if (model == null) throw new System.InvalidOperationException("Could not instantiate the imported Tank FBX.");
            model.name = "TankModel";
            model.transform.SetParent(root.transform, false);
            model.transform.localPosition = new Vector3(0f, TankModelGroundOffset, 0f);
            model.transform.localRotation = Quaternion.identity;
            // The imported Tank FBX root is authored at 100x; 70x keeps it at 70% of its imported size.
            model.transform.localScale = Vector3.one * TankModelScale;

            Animator animator = root.AddComponent<Animator>();
            animator.applyRootMotion = false;
            animator.runtimeAnimatorController = EnsureAnimatorController();

            ParticleSystem muzzleFlash = CreateMuzzleFlash(model.transform);

            GameObject labelObject = new GameObject("Boss Requirement");
            labelObject.transform.SetParent(root.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 5f, 0f);
            labelObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            labelObject.transform.localScale = Vector3.one * 0.08f;
            TextMeshPro label = labelObject.AddComponent<TextMeshPro>();
            if (label.font == null && TMP_Settings.defaultFontAsset != null)
            {
                label.font = TMP_Settings.defaultFontAsset;
            }
            label.text = "FINAL BOSS\n60 RUNNERS REQUIRED";
            label.fontSize = 4f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(1f, 0.82f, 0.3f);
            label.outlineColor = Color.black;
            label.outlineWidth = 0.2f;
            label.enableWordWrapping = false;

            Image healthBarFill = CreateBossHealthBar(root.transform);

            TankBoss tankBoss = root.AddComponent<TankBoss>();
            SerializedObject serializedBoss = new SerializedObject(tankBoss);
            SetReference(serializedBoss, "firingAnimator", animator);
            SetReference(serializedBoss, "muzzleFlash", muzzleFlash);
            SetReference(serializedBoss, "requirementLabel", label);
            SetReference(serializedBoss, "healthBarFill", healthBarFill);
            serializedBoss.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, BossPrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        GameObject created = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
        if (created == null) throw new System.InvalidOperationException("Tank boss prefab was not created at " + BossPrefabPath + ".");
        return created;
    }

    private static void NormalizeExistingPrefabSettings()
    {
        GameObject contents = PrefabUtility.LoadPrefabContents(BossPrefabPath);
        if (contents == null) throw new System.InvalidOperationException("Could not load the existing tank boss prefab contents.");

        try
        {
            Transform model = contents.transform.Find("TankModel");
            if (model == null) throw new System.InvalidOperationException("Existing tank boss prefab is missing TankModel.");

            Vector3 groundPosition = new Vector3(0f, TankModelGroundOffset, 0f);
            BoxCollider trigger = contents.GetComponent<BoxCollider>();
            if (trigger == null) trigger = contents.AddComponent<BoxCollider>();
            Vector3 triggerCenter = new Vector3(0f, BossTriggerCenterY, BossTriggerCenterZ);
            Vector3 triggerSize = new Vector3(BossTriggerWidth, BossTriggerHeight, BossTriggerDepth);
            bool changed = false;
            if (!trigger.isTrigger ||
                (trigger.center - triggerCenter).sqrMagnitude > 0.0001f ||
                (trigger.size - triggerSize).sqrMagnitude > 0.0001f)
            {
                trigger.isTrigger = true;
                trigger.center = triggerCenter;
                trigger.size = triggerSize;
                changed = true;
            }
            if (!Mathf.Approximately(model.localScale.x, TankModelScale) ||
                !Mathf.Approximately(model.localScale.y, TankModelScale) ||
                !Mathf.Approximately(model.localScale.z, TankModelScale) ||
                (model.localPosition - groundPosition).sqrMagnitude > 0.0001f)
            {
                model.localPosition = groundPosition;
                model.localScale = Vector3.one * TankModelScale;
                changed = true;
            }

            Transform existingBar = contents.transform.Find("Boss Health Bar");
            Transform existingFill = existingBar != null ? existingBar.Find("Background/Fill") : null;
            Image healthBarFill = existingFill != null ? existingFill.GetComponent<Image>() : null;
            if (healthBarFill == null)
            {
                if (existingBar != null) Object.DestroyImmediate(existingBar.gameObject);
                healthBarFill = CreateBossHealthBar(contents.transform);
                changed = true;
            }
            if (healthBarFill.color != Color.red)
            {
                healthBarFill.color = Color.red;
                changed = true;
            }

            TankBoss tankBoss = contents.GetComponent<TankBoss>();
            if (tankBoss == null) throw new System.InvalidOperationException("Existing tank boss prefab is missing TankBoss.");
            SerializedObject serializedBoss = new SerializedObject(tankBoss);
            SerializedProperty healthBarProperty = serializedBoss.FindProperty("healthBarFill");
            if (healthBarProperty == null) throw new System.InvalidOperationException("TankBoss is missing the serialized healthBarFill field.");
            if (healthBarProperty.objectReferenceValue != healthBarFill)
            {
                healthBarProperty.objectReferenceValue = healthBarFill;
                changed = true;
            }
            if (serializedBoss.ApplyModifiedPropertiesWithoutUndo()) changed = true;

            ParticleSystem particles = contents.GetComponentInChildren<ParticleSystem>(true);
            if (particles != null)
            {
                ParticleSystem.MainModule main = particles.main;
                if (main.scalingMode != ParticleSystemScalingMode.Local)
                {
                    main.scalingMode = ParticleSystemScalingMode.Local;
                    changed = true;
                }
            }

            if (changed) PrefabUtility.SaveAsPrefabAsset(contents, BossPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    private static Image CreateBossHealthBar(Transform parent)
    {
        Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        if (sprite == null) throw new System.InvalidOperationException("Built-in UI sprite UISprite.psd was not found.");

        GameObject canvasObject = new GameObject("Boss Health Bar", typeof(RectTransform), typeof(Canvas));
        canvasObject.transform.SetParent(parent, false);
        canvasObject.transform.localPosition = new Vector3(0f, 5.65f, 0f);
        canvasObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        canvasObject.transform.localScale = Vector3.one * 0.01f;

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 20;
        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(220f, 18f);

        Image background = CreateHealthBarImage(canvasObject.transform, "Background", sprite, new Color(0.08f, 0.07f, 0.07f, 0.96f));
        RectTransform backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        Image fill = CreateHealthBarImage(background.transform, "Fill", sprite, Color.red);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 1f;
        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2f, 2f);
        fillRect.offsetMax = new Vector2(-2f, -2f);
        return fill;
    }

    private static Image CreateHealthBarImage(Transform parent, string objectName, Sprite sprite, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static ParticleSystem CreateMuzzleFlash(Transform modelRoot)
    {
        Transform cannon = modelRoot.Find("head/cannon");
        if (cannon == null) throw new System.InvalidOperationException("Tank FBX is missing the expected head/cannon transform.");

        GameObject effectObject = new GameObject("Muzzle Flash");
        effectObject.transform.SetParent(cannon, false);
        effectObject.transform.localPosition = new Vector3(0f, 0.018f, -0.0035f);
        effectObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        ParticleSystem particles = effectObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.18f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.22f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.4f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.42f, 0.06f, 1f), new Color(1f, 0.92f, 0.34f, 1f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.maxParticles = 20;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 10) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = 0.04f;

        ParticleSystemRenderer particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.sharedMaterial = EnsureMuzzleMaterial();
        return particles;
    }

    private static Material EnsureMuzzleMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MuzzleMaterialPath);
        if (material != null) return material;

        EnsureFolder("Assets/Materials");
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) throw new System.InvalidOperationException("No supported particle unlit shader was found for the tank muzzle flash.");

        material = new Material(shader);
        material.name = "Level5TankBossMuzzle";
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", new Color(1f, 0.58f, 0.12f, 1f));
        if (material.HasProperty("_Color")) material.SetColor("_Color", new Color(1f, 0.58f, 0.12f, 1f));
        if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", new Color(1f, 0.42f, 0.05f, 1f));
        AssetDatabase.CreateAsset(material, MuzzleMaterialPath);
        return material;
    }

    private static AnimatorController EnsureAnimatorController()
    {
        AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
        AnimationClip clip = EnsureShootClip();
        AnimatorController controller = existing != null
            ? existing
            : AnimatorController.CreateAnimatorControllerAtPath(AnimatorControllerPath);

        bool hasFireParameter = false;
        AnimatorControllerParameter[] parameters = controller.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == "Fire" && parameters[i].type == AnimatorControllerParameterType.Trigger)
            {
                hasFireParameter = true;
                break;
            }
        }
        if (!hasFireParameter) controller.AddParameter("Fire", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState idle = stateMachine.defaultState;
        if (idle == null)
        {
            idle = FindState(stateMachine, "Idle");
            if (idle == null) idle = stateMachine.AddState("Idle");
            stateMachine.defaultState = idle;
        }

        AnimatorState shoot = FindState(stateMachine, "Shoot");
        if (shoot == null) shoot = stateMachine.AddState("Shoot");
        shoot.motion = clip;

        AnimatorStateTransition fire = FindTransition(idle, shoot);
        if (fire == null) fire = idle.AddTransition(shoot);
        fire.hasExitTime = false;
        fire.duration = 0f;
        bool hasFireCondition = false;
        AnimatorCondition[] conditions = fire.conditions;
        for (int i = 0; i < conditions.Length; i++)
        {
            if (conditions[i].mode == AnimatorConditionMode.If && conditions[i].parameter == "Fire")
            {
                hasFireCondition = true;
                break;
            }
        }
        if (!hasFireCondition) fire.AddCondition(AnimatorConditionMode.If, 0f, "Fire");

        AnimatorStateTransition finish = FindTransition(shoot, idle);
        if (finish == null) finish = shoot.AddTransition(idle);
        finish.hasExitTime = true;
        finish.exitTime = 1f;
        finish.duration = 0f;

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static AnimatorState FindState(AnimatorStateMachine stateMachine, string stateName)
    {
        ChildAnimatorState[] states = stateMachine.states;
        for (int i = 0; i < states.Length; i++)
        {
            if (states[i].state != null && states[i].state.name == stateName) return states[i].state;
        }

        return null;
    }

    private static AnimatorStateTransition FindTransition(AnimatorState source, AnimatorState destination)
    {
        AnimatorStateTransition[] transitions = source.transitions;
        for (int i = 0; i < transitions.Length; i++)
        {
            if (transitions[i].destinationState == destination) return transitions[i];
        }

        return null;
    }

    private static AnimationClip EnsureShootClip()
    {
        AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(ShootClipPath);
        if (existing != null) return existing;

        AnimationClip clip = new AnimationClip();
        clip.name = "Level5TankBossShoot";
        clip.frameRate = 30f;
        clip.wrapMode = WrapMode.Once;

        AnimationCurve recoil = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.06f, 0.006f),
            new Keyframe(0.12f, 0.002f),
            new Keyframe(0.22f, 0f));
        EditorCurveBinding binding = EditorCurveBinding.FloatCurve(
            "TankModel/head/cannon",
            typeof(Transform),
            "m_LocalPosition.z");
        AnimationUtility.SetEditorCurve(clip, binding, recoil);

        AssetDatabase.CreateAsset(clip, ShootClipPath);
        return clip;
    }

    private static void SetReference(SerializedObject serializedObject, string propertyName, Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null) throw new System.InvalidOperationException("TankBoss is missing serialized field " + propertyName + ".");
        property.objectReferenceValue = value;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string name = Path.GetFileName(path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
