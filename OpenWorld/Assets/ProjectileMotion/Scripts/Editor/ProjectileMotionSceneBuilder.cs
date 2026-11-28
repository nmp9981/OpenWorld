using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// ProjectileMotion scene 에 UI·Sphere·제어 객체를 만든다.
/// 메뉴: Tools/ProjectileMotion/Build Scene. 다시 실행하면 이전에 만든 객체를 지우고 새로 만든다.
/// </summary>
public static class ProjectileMotionSceneBuilder
{
    private const string ScenePath = "Assets/ProjectileMotion/ProjectileMotion.unity";
    private const string ResourceDir = "Assets/ProjectileMotion/Resources";
    private const string BallMatPath = ResourceDir + "/BallMaterial.mat";
    private const string GroundMatPath = ResourceDir + "/GroundMaterial.mat";

    private static readonly string[] OwnedNames = { "Ball", "Ground", "ProjectileCanvas", "EventSystem", "ProjectileMotion" };

    [MenuItem("Tools/ProjectileMotion/Build Scene")]
    public static void Build()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            scene = EditorSceneManager.OpenScene(ScenePath);
        }

        foreach (var name in OwnedNames)
        {
            var old = GameObject.Find(name);
            if (old != null) Object.DestroyImmediate(old);
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 발사체: Unity 기본 Sphere. 기본 Sphere 에는 Rigidbody 가 없지만 혹시 있으면 제거한다.
        var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "Ball";
        ball.transform.position = new Vector3(0, 0.5f, 0);
        var rb = ball.GetComponent<Rigidbody>();
        if (rb != null) Object.DestroyImmediate(rb);
        ball.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial(BallMatPath, new Color(0.95f, 0.45f, 0.15f));

        // 바닥: 거리 확인용. +x 방향으로 길게 둔다.
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = new Vector3(100, 0, 0);
        ground.transform.localScale = new Vector3(22, 1, 2);
        ground.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial(GroundMatPath, new Color(0.35f, 0.55f, 0.35f));

        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(0, 4, -18);
            cam.transform.rotation = Quaternion.Euler(8, 0, 0);

            // Main Camera 가 공을 따라다니게 한다.
            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            var followSo = new SerializedObject(follow);
            followSo.FindProperty("target").objectReferenceValue = ball.transform;
            followSo.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            Debug.LogWarning("[ProjectileMotion] MainCamera 태그가 붙은 카메라가 없어 카메라 추적을 붙이지 못했습니다.");
        }

        // EventSystem (Input System 용 UI 모듈)
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        // Canvas
        var canvasGo = new GameObject("ProjectileCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var res = new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
        };

        CreateLabel(canvasGo.transform, "SpeedLabel", "속도 (m/s)", font, new Vector2(30, -30));
        var speed = CreateInput(canvasGo.transform, "SpeedInput", "예: 20", font, res, new Vector2(230, -30));
        CreateLabel(canvasGo.transform, "AngleLabel", "각도 (도)", font, new Vector2(30, -90));
        var angle = CreateInput(canvasGo.transform, "AngleInput", "예: 45", font, res, new Vector2(230, -90));
        var distance = CreateLabel(canvasGo.transform, "DistanceText", "+x 이동 거리: 0.0 m", font, new Vector2(30, -160), 600);
        distance.fontSize = 36;
        var status = CreateLabel(canvasGo.transform, "StatusText", "", font, new Vector2(30, -220), 900);

        // 제어 객체
        var ctrlGo = new GameObject("ProjectileMotion");
        var ctrl = ctrlGo.AddComponent<ProjectileMotionController>();
        var so = new SerializedObject(ctrl);
        so.FindProperty("speedInput").objectReferenceValue = speed;
        so.FindProperty("angleInput").objectReferenceValue = angle;
        so.FindProperty("distanceText").objectReferenceValue = distance;
        so.FindProperty("statusText").objectReferenceValue = status;
        so.FindProperty("ball").objectReferenceValue = ball.transform;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[ProjectileMotion] scene 구성 완료: " + ScenePath);
    }

    private static Text CreateLabel(Transform parent, string name, string text, Font font, Vector2 pos, float width = 190)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        SetTopLeft(rt, pos, new Vector2(width, 50));
        var t = go.GetComponent<Text>();
        t.font = font;
        t.fontSize = 28;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleLeft;
        t.text = text;
        return t;
    }

    private static InputField CreateInput(Transform parent, string name, string placeholder, Font font,
                                          DefaultControls.Resources res, Vector2 pos)
    {
        var go = DefaultControls.CreateInputField(res);
        go.name = name;
        go.transform.SetParent(parent, false);
        SetTopLeft((RectTransform)go.transform, pos, new Vector2(240, 50));

        var input = go.GetComponent<InputField>();
        input.textComponent.font = font;
        input.textComponent.fontSize = 26;
        var ph = (Text)input.placeholder;
        ph.font = font;
        ph.fontSize = 26;
        ph.text = placeholder;
        return input;
    }

    private static void SetTopLeft(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static Material GetOrCreateMaterial(string path, Color color)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        mat.color = color;
        AssetDatabase.CreateAsset(mat, path);
        AssetDatabase.SaveAssets();
        return mat;
    }
}
