using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using Unity.AI.Navigation;
using TMPro;
using UnityEngine.UI;

// Editor-only tool — builds the entire Hollow Vault scene from scratch with one click
// Menu: Tools > Setup Hollow Vault Scene
// Safe to run multiple times — it clears the scene first each time
public static class SceneSetup
{
    // --- ENTRY POINT ---

    [MenuItem("Tools/Setup Hollow Vault Scene")]
    public static void SetupScene()
    {
        ClearScene();
        BuildEnvironment(out NavMeshSurface navSurface);
        Transform[] waypoints = BuildPatrolWaypoints();
        PlayerController pc = BuildPlayer();
        CryptSentinel sentinel = BuildSentinel(pc.transform, waypoints);
        TheShade shade = BuildShade(pc.transform);
        GameManager gm = BuildUI(sentinel, shade, pc);
        BuildGoalZone();
        BuildLevelManager(sentinel, shade, pc, gm);
        BuildCamera(pc.transform);
        BuildLight();

        // Bake NavMesh last so all obstacles are in place
        navSurface.BuildNavMesh();

        // Mark scene dirty so Unity prompts to save
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[SceneSetup] Done — NavMesh baked. Press Play to test.");
    }

    // --- SCENE CLEAR ---

    static void ClearScene()
    {
        foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            Object.DestroyImmediate(go);
    }

    // --- ENVIRONMENT ---

    static void BuildEnvironment(out NavMeshSurface navSurface)
    {
        // Floor — 30×30 units; NavMeshSurface bakes the walkable area from here
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.localScale = new Vector3(3f, 1f, 3f);
        SetColor(floor, new Color(0.22f, 0.20f, 0.17f));

        navSurface = floor.AddComponent<NavMeshSurface>();
        navSurface.collectObjects = CollectObjects.All;
        navSurface.useGeometry    = NavMeshCollectGeometry.PhysicsColliders;

        // Outer perimeter walls — slightly oversized to close the corners
        MakeWall("Wall_North", new Vector3(0f,  1f,  15.25f), new Vector3(30.5f, 2f, 0.5f));
        MakeWall("Wall_South", new Vector3(0f,  1f, -15.25f), new Vector3(30.5f, 2f, 0.5f));
        MakeWall("Wall_East",  new Vector3( 15.25f, 1f, 0f),  new Vector3(0.5f, 2f, 30.5f));
        MakeWall("Wall_West",  new Vector3(-15.25f, 1f, 0f),  new Vector3(0.5f, 2f, 30.5f));

        // Interior pillars — break line of sight and add navigational interest
        MakePillar("Pillar_NE", new Vector3( 6f, 1f,  6f));
        MakePillar("Pillar_NW", new Vector3(-6f, 1f,  6f));
        MakePillar("Pillar_SE", new Vector3( 6f, 1f, -6f));
        MakePillar("Pillar_SW", new Vector3(-6f, 1f, -6f));

        // Central divider wall — creates a narrow corridor in the middle
        MakeWall("Divider", new Vector3(0f, 1f, 0f), new Vector3(10f, 2f, 0.5f));
    }

    // --- PATROL WAYPOINTS ---

    static Transform[] BuildPatrolWaypoints()
    {
        GameObject parent = new GameObject("PatrolWaypoints");

        Vector3[] positions =
        {
            new Vector3(-11f, 0f, -9f),
            new Vector3( 11f, 0f, -9f),
            new Vector3( 11f, 0f,  9f),
            new Vector3(-11f, 0f,  9f),
        };

        Transform[] waypoints = new Transform[positions.Length];
        for (int i = 0; i < positions.Length; i++)
        {
            GameObject wp = new GameObject($"Waypoint_{i}");
            wp.transform.SetParent(parent.transform);
            wp.transform.position = positions[i];
            waypoints[i] = wp.transform;
        }
        return waypoints;
    }

    // --- PLAYER ---

    static PlayerController BuildPlayer()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "Player";
        go.transform.position = new Vector3(0f, 1f, -10f);
        SetColor(go, new Color(0.25f, 0.48f, 0.92f));

        // CharacterController replaces the default CapsuleCollider
        Object.DestroyImmediate(go.GetComponent<CapsuleCollider>());
        CharacterController cc = go.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.4f;
        cc.center = Vector3.zero;

        PlayerController pc  = go.AddComponent<PlayerController>();
        PlayerAttack     atk = go.AddComponent<PlayerAttack>();
        atk.attackRange  = 2f;
        atk.attackDamage = 20f;

        return pc;
    }

    // --- CRYPT SENTINEL ---

    static CryptSentinel BuildSentinel(Transform player, Transform[] waypoints)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "CryptSentinel";
        go.transform.position = new Vector3(-11f, 1f, -9f); // starts at first waypoint
        SetColor(go, new Color(0.85f, 0.25f, 0.10f));

        // NavMeshAgent — pathing component
        NavMeshAgent agent = go.AddComponent<NavMeshAgent>();
        agent.height           = 2f;
        agent.radius           = 0.4f;
        agent.speed            = 1.8f;
        agent.angularSpeed     = 200f;
        agent.acceleration     = 8f;
        agent.stoppingDistance = 0.3f;

        // VisionCone — 60° FOV, 9m range
        VisionCone vision = go.AddComponent<VisionCone>();
        vision.viewRadius = 9f;
        vision.viewAngle  = 60f;

        // CryptSentinel FSM
        CryptSentinel s    = go.AddComponent<CryptSentinel>();
        s.playerTransform  = player;
        s.patrolWaypoints  = waypoints;
        s.patrolSpeed      = 1.8f;
        s.chargeSpeed      = 3.2f;
        s.attackRange      = 1.5f;
        s.staggerDuration  = 1.5f;
        s.maxHP            = 100f;
        s.rallyHPThreshold = 0.35f;
        s.memoryDuration   = 4f;
        s.hearingRange     = 6f;
        s.rallyRadius      = 10f;

        // Vision cone mesh — child object so it rotates with the Sentinel
        // At local Y = -0.7 the flat fan sits at world Y ≈ 0.3, safely above the floor
        GameObject coneGO = new GameObject("VisionConeMesh");
        coneGO.transform.SetParent(go.transform);
        coneGO.transform.localPosition = new Vector3(0f, -0.7f, 0f);
        coneGO.transform.localRotation = Quaternion.identity;
        VisionConeMesh vcm = coneGO.AddComponent<VisionConeMesh>();
        vcm.visionCone    = vision;
        vcm.segments      = 24;
        vcm.defaultColor  = new Color(0f, 1f, 1f, 0.35f);
        vcm.alertColor    = new Color(1f, 0f, 0f, 0.35f);

        return s;
    }

    // --- THE SHADE ---

    static TheShade BuildShade(Transform player)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "TheShade";
        go.transform.position = new Vector3(11f, 1f, 9f);
        SetColor(go, new Color(0.45f, 0.05f, 0.75f));

        // NavMeshAgent — faster turning and acceleration for a ghostly feel
        NavMeshAgent agent = go.AddComponent<NavMeshAgent>();
        agent.height           = 2f;
        agent.radius           = 0.4f;
        agent.speed            = 2.5f;
        agent.angularSpeed     = 360f;
        agent.acceleration     = 12f;
        agent.stoppingDistance = 0.3f;

        // TheShade FSM
        TheShade sh         = go.AddComponent<TheShade>();
        sh.playerTransform  = player;
        sh.driftSpeed       = 2.5f;
        sh.huntSpeed        = 4.5f;
        sh.soundRadius      = 8f;
        sh.lightRadius      = 5f;
        sh.huntRange        = 3f;
        sh.soundFadeTime    = 3f;
        sh.retreatRadius    = 10f;

        return sh;
    }

    // --- UI ---

    static GameManager BuildUI(CryptSentinel sentinel, TheShade shade, PlayerController pc)
    {
        // Screen-space canvas
        GameObject canvasGO = new GameObject("UICanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Agent state labels (top-left corner)
        sentinel.stateLabel = MakeTMPLabel("SentinelLabel", canvasGO.transform,
            new Vector2(10f, -10f), "Sentinel: Patrol");
        shade.stateLabel = MakeTMPLabel("ShadeLabel", canvasGO.transform,
            new Vector2(10f, -42f), "Shade: Drift");

        // Objective hint
        MakeTMPLabel("ObjectiveLabel", canvasGO.transform, new Vector2(10f, -74f),
            "OBJECTIVE: Reach the green exit");

        // Stamina bar (bottom-left corner)
        GameObject bgGO = new GameObject("StaminaBG");
        bgGO.transform.SetParent(canvasGO.transform, false);
        Image bg = bgGO.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.08f, 0.85f);
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin        = new Vector2(0f, 0f);
        bgRect.anchorMax        = new Vector2(0f, 0f);
        bgRect.pivot            = new Vector2(0f, 0f);
        bgRect.anchoredPosition = new Vector2(10f, 10f);
        bgRect.sizeDelta        = new Vector2(200f, 16f);

        GameObject fillGO = new GameObject("StaminaFill");
        fillGO.transform.SetParent(bgGO.transform, false);
        Image fill = fillGO.AddComponent<Image>();
        fill.color = new Color(0.20f, 0.82f, 0.30f, 1f);
        RectTransform fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fillRect.pivot     = new Vector2(0f, 0.5f);

        MakeTMPLabel("StaminaLabel", canvasGO.transform, new Vector2(10f, -100f), "STAMINA");

        StaminaBar sb = bgGO.AddComponent<StaminaBar>();
        sb.player    = pc;
        sb.fillImage = fill;

        // Controls reminder (bottom-right corner)
        GameObject ctrlGO = new GameObject("ControlsLabel");
        ctrlGO.transform.SetParent(canvasGO.transform, false);
        RectTransform ctrlRect = ctrlGO.AddComponent<RectTransform>();
        ctrlRect.anchorMin        = new Vector2(1f, 0f);
        ctrlRect.anchorMax        = new Vector2(1f, 0f);
        ctrlRect.pivot            = new Vector2(1f, 0f);
        ctrlRect.anchoredPosition = new Vector2(-10f, 10f);
        ctrlRect.sizeDelta        = new Vector2(400f, 80f);
        TextMeshProUGUI ctrlTmp = ctrlGO.AddComponent<TextMeshProUGUI>();
        ctrlTmp.text      = "WASD: Move   Shift: Sneak\nF: Toggle Torch   LMB: Attack Sentinel";
        ctrlTmp.fontSize  = 15;
        ctrlTmp.color     = new Color(1f, 1f, 1f, 0.7f);
        ctrlTmp.alignment = TMPro.TextAlignmentOptions.BottomRight;

        // End-game overlay (hidden until win/lose fires)
        GameObject overlayGO = new GameObject("EndOverlay");
        overlayGO.transform.SetParent(canvasGO.transform, false);
        RectTransform overlayRect = overlayGO.AddComponent<RectTransform>();
        overlayRect.anchorMin = new Vector2(0.5f, 0.5f);
        overlayRect.anchorMax = new Vector2(0.5f, 0.5f);
        overlayRect.pivot     = new Vector2(0.5f, 0.5f);
        overlayRect.sizeDelta = new Vector2(700f, 140f);
        overlayRect.anchoredPosition = Vector2.zero;
        TextMeshProUGUI overlayTmp = overlayGO.AddComponent<TextMeshProUGUI>();
        overlayTmp.text      = "";
        overlayTmp.fontSize  = 42;
        overlayTmp.alignment = TMPro.TextAlignmentOptions.Center;
        overlayGO.SetActive(false);

        // GameManager lives on the canvas so it has easy access to the overlay
        GameManager gm = canvasGO.AddComponent<GameManager>();
        gm.overlayText = overlayTmp;

        return gm;
    }

    // --- GOAL ZONE ---

    static void BuildGoalZone()
    {
        // Glowing green sphere on the far side of the map — player's objective
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "ExitZone";
        go.transform.position   = new Vector3(0f, 0.8f, 12f);
        go.transform.localScale = Vector3.one * 1.6f;

        // Emissive green material so it stands out in the dark vault
        Material m = new Material(Shader.Find("Standard"));
        m.color = new Color(0.1f, 0.9f, 0.2f, 1f);
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", new Color(0f, 1.2f, 0.1f));
        go.GetComponent<Renderer>().sharedMaterial = m;

        go.AddComponent<GoalZone>();
    }

    // --- LEVEL MANAGER ---

    static void BuildLevelManager(CryptSentinel sentinel, TheShade shade, PlayerController pc, GameManager gm)
    {
        GameObject go  = new GameObject("LevelManager");
        LevelManager lm = go.AddComponent<LevelManager>();
        lm.sentinel = sentinel;
        lm.shade    = shade;
        lm.player   = pc;
    }

    // --- CAMERA ---

    static void BuildCamera(Transform target)
    {
        GameObject go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        go.AddComponent<Camera>();
        go.AddComponent<AudioListener>();

        FollowCamera fc = go.AddComponent<FollowCamera>();
        fc.target      = target;
        fc.offset      = new Vector3(0f, 14f, -12f);
        fc.smoothSpeed = 8f;

        go.transform.position = target.position + new Vector3(0f, 14f, -12f);
        // Look slightly above floor level for a more natural angled view
        go.transform.LookAt(target.position + Vector3.up * 1.5f);
    }

    // --- LIGHT ---

    static void BuildLight()
    {
        GameObject go = new GameObject("Directional Light");
        Light dl = go.AddComponent<Light>();
        dl.type      = LightType.Directional;
        dl.intensity = 0.8f;
        dl.color     = new Color(0.90f, 0.85f, 0.75f);
        go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    // --- HELPERS ---

    static void MakeWall(string name, Vector3 pos, Vector3 scale)
    {
        GameObject w = GameObject.CreatePrimitive(PrimitiveType.Cube);
        w.name = name;
        w.transform.position   = pos;
        w.transform.localScale = scale;
        SetColor(w, new Color(0.16f, 0.14f, 0.12f));
    }

    static void MakePillar(string name, Vector3 pos)
    {
        GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cube);
        p.name = name;
        p.transform.position   = pos;
        p.transform.localScale = new Vector3(1.5f, 2f, 1.5f);
        SetColor(p, new Color(0.28f, 0.24f, 0.20f));
    }

    static void SetColor(GameObject go, Color color)
    {
        Renderer r = go.GetComponent<Renderer>();
        if (r == null) return;
        Material m = new Material(Shader.Find("Standard")) { color = color };
        r.sharedMaterial = m;
    }

    static TextMeshProUGUI MakeTMPLabel(string name, Transform parent, Vector2 anchoredPos, string defaultText)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 1f); // top-left anchor
        rt.anchorMax        = new Vector2(0f, 1f);
        rt.pivot            = new Vector2(0f, 1f);
        rt.sizeDelta        = new Vector2(300f, 28f);
        rt.anchoredPosition = anchoredPos;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text     = defaultText;
        tmp.fontSize = 18;
        tmp.color    = Color.white;

        return tmp;
    }
}
