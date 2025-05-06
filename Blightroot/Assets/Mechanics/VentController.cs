using UnityEngine;
using UnityEngine.InputSystem;
using TarodevController;

public class VentController : MonoBehaviourBase {
  [Header("Vent Config")]
  [SerializeField] private Transform ventHinge;
  [SerializeField] private float leanRotation   = 15f;
  [SerializeField] private float openRotation   = 45f;
  [SerializeField] private float rotationSpeed  = 180f;  // ° per second
  [SerializeField] private float peekThreshold  = 0.7f;

  private PlayerInputActions input;
  private Vector2 moveInput;
  private bool actionTriggered, isPlayerNear, isLeaning, isOpen;
  private Quaternion targetRotation = Quaternion.identity;

  protected override void Awake() {
    base.Awake();
    input = new PlayerInputActions();
  }

  private void OnEnable() {
    input.Player.Enable();
    input.Player.Move.performed  += OnMove;
    input.Player.Move.canceled   += OnMove;
    input.Player.Action.performed += OnAction;
  }

  private void OnDisable() {
    input.Player.Move.performed  -= OnMove;
    input.Player.Move.canceled   -= OnMove;
    input.Player.Action.performed -= OnAction;
    input.Player.Disable();
  }

  private void Update() {
    // 1) handle “open” once
    if (actionTriggered) {
      isOpen = true;
      targetRotation = Quaternion.Euler(0, 0, -openRotation);
      actionTriggered = false;
    }

    // 2) handle lean input while near & not open
    if (!isOpen && isPlayerNear) {
      if (moveInput.x > peekThreshold && !isLeaning) {
        isLeaning = true;
        targetRotation = Quaternion.Euler(0, 0, -leanRotation);
      } else if (moveInput.x <= peekThreshold && isLeaning) {
        isLeaning = false;
        targetRotation = Quaternion.identity;
      }
    }

    // 3) rotate the hinge at a constant speed
    if (ventHinge != null && (isLeaning || isOpen)) {
      float step = rotationSpeed * Time.deltaTime;
      ventHinge.localRotation = Quaternion.RotateTowards(
        ventHinge.localRotation,
        targetRotation,
        step
      );
    }
  }

  private void OnMove(InputAction.CallbackContext ctx) {
    moveInput = ctx.ReadValue<Vector2>();
  }

  private void OnAction(InputAction.CallbackContext ctx) {
    if (isPlayerNear && !isOpen) actionTriggered = true;
  }

  private void OnCollisionEnter2D(Collision2D col) {
    if (col.gameObject.CompareTag("Player")) {
      isPlayerNear = true;
    }
  }

  private void OnCollisionExit2D(Collision2D col) {
    if (col.gameObject.CompareTag("Player")) {
      isPlayerNear = false;
      // reset lean if they walk away
      if (!isOpen) {
        isLeaning = false;
        targetRotation = Quaternion.identity;
      }
    }
  }

  protected override void DrawGizmosSafe() {
    if (!showGizmos) return;
    Gizmos.color = Color.cyan;
    Gizmos.DrawWireCube(transform.position, new Vector3(1f, 1f, 0.1f));
  }
}
