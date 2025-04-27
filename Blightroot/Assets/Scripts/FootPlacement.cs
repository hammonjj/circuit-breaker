using TarodevController;
using UnityEngine;

public class FootPlacement : MonoBehaviour
{
    [Header("Setup")]
    public Transform rootTransform;            // your SkeletonUtility-SkeletonRoot
    public PlayerController playerController;  // drag your PlayerController here

    [Header("Offsets")]
    public Vector2 footOffset = new Vector2(0f, 0.1f);
    public float raycastStartHeight = 0.5f;
    public float raycastDistance     = 2f;

    [Header("Step Tuning")]
    public float forwardFactor = 0.2f;
    public float moveSpeed     = 10f;

    Vector2 _originalLocal;
    Vector2 _targetWorld;
    bool    _initted;

    void Start()
    {
        if (rootTransform    == null) rootTransform    = transform.parent;
        if (playerController == null) playerController = GetComponentInParent<PlayerController>();

        // record the “idle” local-space foot position
        Vector3 worldPos = transform.position;
        _originalLocal   = rootTransform.InverseTransformPoint(worldPos);
        _targetWorld     = worldPos;
        _initted         = true;
    }

    void LateUpdate()
    {
        if (!_initted || playerController == null) return;
        UpdateFootTarget();
        SmoothStep();
    }

    void UpdateFootTarget()
    {
        // 1) build a local space offset + forward bump
        float vx = playerController.Velocity.x;
        Vector2 bumpedLocal = _originalLocal + new Vector2(vx * forwardFactor, 0f);

        // 2) transform back into world (we need a Vector3 for TransformPoint)
        Vector3 local3   = new Vector3(bumpedLocal.x, bumpedLocal.y, 0f);
        Vector3 world3   = rootTransform.TransformPoint(local3);
        Vector2 rayOrigin = (Vector2)world3 + Vector2.up * raycastStartHeight;

        // 3) shoot our 2D ray
        RaycastHit2D hit = Physics2D.Raycast(
            rayOrigin,
            Vector2.down,
            raycastDistance,
            playerController.Stats.CollisionLayers);

        if (hit.collider != null)
        {
            // 4) plant your foot
            _targetWorld = hit.point + footOffset;
        }
    }

    void SmoothStep()
    {
        transform.position = Vector2.Lerp(
            transform.position,
            _targetWorld,
            Time.deltaTime * moveSpeed);
    }
}
