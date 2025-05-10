using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TarodevController;

[RequireComponent(typeof(Collider2D))]
public class CoverObject : MonoBehaviourBase {
    [Header("Cover Settings")]
    [Tooltip("Where the player should snap to when entering cover")]
    [SerializeField] private Transform coverPoint;
    [Tooltip("Sorting Layer the player uses while in cover")]
    [SerializeField] private string coverSortingLayer = "Foreground";
    [Tooltip("Sorting Order offset while in cover")]
    [SerializeField] private int coverOrderOffset = 1;
    [Tooltip("Time in seconds to move player into cover")]
    [SerializeField] private float moveDuration = 0.2f;

    private PlayerInputActions input;
    private bool isPlayerNear;
    private bool isInCover;
    private bool isMoving;
    private GameObject player;
    private Renderer playerRenderer;
    private string originalSortingLayer;
    private int originalSortingOrder;

    protected override void Awake() {
        base.Awake();
        LogDebug("CoverObject Awake");
        input = new PlayerInputActions();
    }

    protected override void OnEnable() {
        base.OnEnable();
        input.Player.Enable();
        input.Player.Action.performed += OnAction;

        MessageBus.Subscribe<DisableInputExceptActionEvent>(OnRestrictInput);
        MessageBus.Subscribe<EnableInputExceptActionEvent>(OnRestoreInput);

        LogDebug("CoverObject input enabled");
    }

    private void OnDisable() {
        input.Player.Action.performed -= OnAction;
        input.Player.Disable();

        MessageBus.Subscribe<DisableInputExceptActionEvent>(OnRestrictInput);
        MessageBus.Subscribe<EnableInputExceptActionEvent>(OnRestoreInput);

        LogDebug("CoverObject input disabled");
    }

    private void OnRestrictInput(DisableInputExceptActionEvent _)
    {
        input.Player.Disable();
        input.Player.Action.Enable();
        LogDebug("Restricted full input map");
    }

    private void OnRestoreInput(EnableInputExceptActionEvent _)
    {
        input.Player.Enable();
        LogDebug("Restored full input map");
    }

    private void OnTriggerEnter2D(Collider2D other) {
        LogDebug($"TriggerEnter2D: {other.name}");
        if (!other.CompareTag("Player")) {
            return;
        }

        isPlayerNear = true;
        player = other.gameObject;

        // Try to find any Renderer on the player or its children
        Renderer[] renderers = player.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0) {
            playerRenderer = renderers[0];
            LogDebug($"Found renderer: {playerRenderer.GetType().Name} on {playerRenderer.gameObject.name}");
        } else {
            LogError("No Renderer found on player or its children. Assign one to enable cover visuals.");
            playerRenderer = null;
        }
    }

    private void OnTriggerExit2D(Collider2D other) {
        LogDebug($"TriggerExit2D: {other.name}");
        if (!other.CompareTag("Player")) {
            return;
        }

        isPlayerNear = false;
        LogDebug("Player left cover trigger");
        if (isInCover) {
            ExitCover();
        }
    }

    private void OnAction(InputAction.CallbackContext ctx) {
        LogDebug("Action performed");
        if (!isPlayerNear || isMoving) {
            LogDebug($"Cannot toggle cover (near={isPlayerNear}, moving={isMoving})");
            return;
        }
        if (isInCover) {
            LogDebug("Exiting cover");
            ExitCover();
        } else {
            LogDebug("Entering cover");
            EnterCover();
        }
    }

    private void EnterCover() {
        LogDebug("EnterCover() called");
        if (player == null || playerRenderer == null || coverPoint == null) {
            LogError("EnterCover aborted: missing player, renderer, or coverPoint");
            return;
        }

        // Cache original sorting
        originalSortingLayer = playerRenderer.sortingLayerName;
        originalSortingOrder = playerRenderer.sortingOrder;
        LogDebug($"Cached sorting: layer={originalSortingLayer}, order={originalSortingOrder}");

        // Apply cover sorting
        playerRenderer.sortingLayerName = coverSortingLayer;
        playerRenderer.sortingOrder = originalSortingOrder + coverOrderOffset;
        LogDebug($"Applied cover sorting: layer={coverSortingLayer}, order={playerRenderer.sortingOrder}");

        // Restrict controls to Move & Action only
        input.Player.Disable();
        input.Player.Action.Enable();
        LogDebug("Restricted input to Move & Action");
        
        MessageBus.Publish(new DisableInputExceptActionEvent());

        // Smoothly move into cover
        isMoving = true;
        StartCoroutine(LerpToCover());
    }

    private IEnumerator LerpToCover() {
        LogDebug("LerpToCover() start");
        Vector3 start = player.transform.position;
        Vector3 end = coverPoint.position;
        float elapsed = 0f;

        while (elapsed < moveDuration) {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            player.transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        player.transform.position = end;
        isInCover = true;
        isMoving = false;
        LogDebug("LerpToCover() complete");
    }

    private void ExitCover() {
        LogDebug("ExitCover() called");
        if (playerRenderer == null) {
            LogError("ExitCover aborted: no renderer to restore");
            return;
        }

        // Restore original sorting
        playerRenderer.sortingLayerName = originalSortingLayer;
        playerRenderer.sortingOrder = originalSortingOrder;
        LogDebug($"Restored sorting: layer={originalSortingLayer}, order={originalSortingOrder}");

        isInCover = false;
        MessageBus.Publish(new EnableInputExceptActionEvent());
    }

    protected override void DrawGizmosSafe() {
        if (!showGizmos) {
            return;
        }
        Gizmos.color = Color.yellow;
        Collider2D col = GetComponent<Collider2D>();
        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        if (coverPoint != null) {
            Gizmos.DrawWireSphere(coverPoint.position, 0.15f);
        }
    }
}
