using UnityEngine;

// Renders the Sentinel's vision cone as a real mesh so it's visible in the Game window
// Attach this to a child object of the CryptSentinel — it reads data from VisionCone on the parent
// Cyan = searching / patrolling, Red = player detected (matches the prof's demo convention)
public class VisionConeMesh : MonoBehaviour
{
    [Header("References")]
    public VisionCone visionCone;

    [Header("Appearance")]
    public int   segments    = 24;
    public Color defaultColor = new Color(0f, 1f, 1f, 0.30f); // cyan
    public Color alertColor   = new Color(1f, 0f, 0f, 0.30f); // red

    // --- RUNTIME COMPONENTS ---

    MeshFilter   mf;
    MeshRenderer mr;
    Material     mat;
    Mesh         coneMesh;

    // --- UNITY LIFECYCLE ---

    void Awake()
    {
        mf       = gameObject.AddComponent<MeshFilter>();
        mr       = gameObject.AddComponent<MeshRenderer>();
        coneMesh = new Mesh { name = "VisionConeFan" };
        mf.mesh  = coneMesh;

        // Standard shader in Transparent mode — reliable in Built-in RP, visible in Game view
        mat = new Material(Shader.Find("Standard"));
        mat.SetFloat("_Mode", 3f);                                                     // Transparent
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        mat.color = defaultColor;
        mr.material          = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows    = false;
    }

    void Update()
    {
        if (visionCone == null) return;

        // Swap colour based on whether the Sentinel can currently see the player
        mat.color = visionCone.IsDetecting ? alertColor : defaultColor;

        RebuildMesh();
    }

    // --- MESH GENERATION ---

    void RebuildMesh()
    {
        float radius    = visionCone.viewRadius;
        float halfAngle = visionCone.viewAngle * 0.5f;

        // vertices: origin + (segments + 1) arc points
        Vector3[] verts = new Vector3[segments + 2];
        int[]     tris  = new int[segments * 3];

        // Tip of the cone at the origin (local space)
        verts[0] = Vector3.zero;

        // Arc spread left-to-right across the full view angle
        for (int i = 0; i <= segments; i++)
        {
            float t     = (float)i / segments;
            float angle = Mathf.Lerp(-halfAngle, halfAngle, t) * Mathf.Deg2Rad;
            // Flat on the XZ plane so it lies on the floor
            verts[i + 1] = new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);
        }

        // Triangle fan from origin to each arc edge
        for (int i = 0; i < segments; i++)
        {
            tris[i * 3]     = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = i + 2;
        }

        coneMesh.Clear();
        coneMesh.vertices  = verts;
        coneMesh.triangles = tris;
        coneMesh.RecalculateNormals();
    }
}
