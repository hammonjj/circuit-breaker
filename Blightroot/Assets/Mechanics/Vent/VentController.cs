using UnityEngine;
using UnityEngine.InputSystem;
using TarodevController;
using Unity.Cinemachine;

public class VentController : MonoBehaviourBase {
  [Header("Vent Config")]
  [SerializeField] private float peekThreshold = 0.7f;

  [Header("Cinemachine Lean")]
  [Tooltip("Your Cinemachine Virtual Camera")]
  [SerializeField] private CinemachineCamera vCam;
  [Tooltip("Max horizontal offset for peek (world units)")]
  [SerializeField] private float leanDistance = 2f;
  [Tooltip("How fast the offset lerps to target")]
  [SerializeField] private float leanSpeed = 5f;

  private PlayerInputActions input;
  private Vector2 moveInput;
  private bool isPlayerNear;
  private bool isLeaning;
  private bool isOpen;

  private LayerMask leanMask;
  private LayerMask ventMask;

  private Animator animator;
  private CinemachinePositionComposer _composer;

  protected override void Awake() {
    base.Awake();
    ventMask = gameObject.layer;
    leanMask = LayerMask.NameToLayer("Mask");
    animator = GetComponent<Animator>();

    input = new PlayerInputActions();

    if (vCam != null) {
      var comp = vCam.GetCinemachineComponent(CinemachineCore.Stage.Body);
      _composer = comp as CinemachinePositionComposer;

      if (_composer == null) {
        LogDebug("VentController: could not find CinemachinePositionComposer on vCam");
      }
    }
  }

  protected override void OnEnable() {
    base.OnEnable();
    input.Player.Enable();
    input.Player.Move.performed += OnMove;
    input.Player.Move.canceled  += OnMove;
    input.Player.Action.performed += OnAction;
  }

  private void OnDisable() {
    input.Player.Move.performed -= OnMove;
    input.Player.Move.canceled  -= OnMove;
    input.Player.Action.performed -= OnAction;
    input.Player.Disable();
  }

  private void Update() {
    if (!isPlayerNear || isOpen || _composer == null) {return;}

    MoveCamera();
    
  }

  private void MoveCamera() {
    float x = moveInput.x;
    bool shouldLean = Mathf.Abs(x) > peekThreshold;
    if (shouldLean != isLeaning) {
      isLeaning = shouldLean;
      updateLayerMask(isLeaning);
    }

    // pick target: ±leanDistance or 0
    float targetX = isLeaning ? Mathf.Sign(x) * leanDistance : 0f;

    // lerp the virtual‑camera’s offset
    var off = _composer.TargetOffset;
    off.x = Mathf.Lerp(off.x, targetX, leanSpeed * Time.deltaTime);
    _composer.TargetOffset = off;
  }

  private void updateLayerMask(bool isLeaning) {
    if (isLeaning) {
      gameObject.layer = leanMask;
    } else {
      gameObject.layer = ventMask;
    }
  }

  private void OnMove(InputAction.CallbackContext ctx) {
    moveInput = ctx.ReadValue<Vector2>();
  }

  private void OnAction(InputAction.CallbackContext ctx) {
    if (isPlayerNear && !isOpen) {
      animator.SetTrigger("Open");
      isOpen = true;
      isLeaning = false;
    }
  }

  private void OnCollisionEnter2D(Collision2D col) {
    if (col.gameObject.CompareTag("Player")) {
      isPlayerNear = true;
    }
  }

  private void OnCollisionExit2D(Collision2D col) {
    if (col.gameObject.CompareTag("Player")) {
      isPlayerNear = false;
      isLeaning = false;

      updateLayerMask(false);
    }
  }

  protected override void DrawGizmosSafe() {
    if (!showGizmos) return;
    Gizmos.color = Color.cyan;
    var top = transform.position + Vector3.up * 1.5f;
    Gizmos.DrawLine(top, top + Vector3.right * peekThreshold);
    Gizmos.DrawLine(top, top + Vector3.left  * peekThreshold);
  }
}
