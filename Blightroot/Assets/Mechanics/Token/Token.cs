using TarodevController;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

//[RequireComponent(typeof(BoxCollider2D), typeof(UIDocument))]
public class Token : MonoBehaviourBase
{
    [Header("UI Toolkit (World Space)")]
    [Tooltip("UIDocument on this GameObject, set to use a World Space PanelSettings.")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Token Settings")]
    [Tooltip("The gem GameObject to collect.")]
    [SerializeField] private GameObject gem;
    [Tooltip("Seconds required to hold Action before collecting.")]
    [SerializeField] private float holdTime = 2f;
    [Tooltip("How close the player must be to start collecting.")]
    [SerializeField] private float detectionRadius = 2f;

    private Transform _player;
    private VisualElement _rootVE;
    private ProgressBar _progressBar;
    private bool _isCollecting;
    private float _timer;
    private Vector3 _startPos;
    private PlayerInputActions input;
    private bool _isPlayerNear;

    protected override void OnEnable()
    {
        base.OnEnable();
        input.Player.Enable();
        input.Player.Action.performed += OnAction;
    }

    protected void OnDisable()
    {
        input.Player.Action.performed -= OnAction;
        input.Player.Disable();
    }
    
    protected override void Awake()
    {
        base.Awake();
        input = new PlayerInputActions();

        // cache player transform
        _player = GameObject.FindWithTag("Player")?.transform;
        if (_player == null)
        {
            LogWarning("Player tag not found; disabling.");
            enabled = false;
            return;
        }

        // get UIDocument (must be set to World Space in inspector)
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            LogWarning("UIDocument missing; disabling.");
            enabled = false;
            return;
        }

        // grab root and hide it
        _rootVE = uiDocument.rootVisualElement;
        _rootVE.style.position       = Position.Absolute;
        _rootVE.style.left           = 0;
        _rootVE.style.top            = 0;
        _rootVE.style.width          = new StyleLength(Length.Percent(100));
        _rootVE.style.height         = new StyleLength(Length.Percent(100));
        _rootVE.style.justifyContent = Justify.Center;
        _rootVE.style.alignItems     = Align.Center;
        _rootVE.style.display        = DisplayStyle.None;

        // query the ProgressBar in your UXML by its name
        _progressBar = _rootVE.Q<ProgressBar>("token-progress");
        if (_progressBar == null)
            LogWarning("ProgressBar 'token-progress' not found in UXML.");
    }

    private void Update()
    {
        if (gem == null || !gem.activeInHierarchy)
            return;

        if (_isCollecting)
        {
            // cancel if move or release
            if (Vector3.Distance(_player.position, _startPos) > 0.01f)
            {
                EndCollect();
                return;
            }

            // tick timer & update UI
            _timer += Time.deltaTime;
            _progressBar.value = _timer;

            // complete
            if (_timer >= holdTime)
                FinishCollect();
        }
    }

    private void OnTriggerEnter2D(Collider2D other) {
        LogDebug($"TriggerEnter2D: {other.name}");
        if (!other.CompareTag("Player")) {
            return;
        }

        _isPlayerNear = true;
    }

    private void BeginCollect()
    {
        if (_progressBar == null) return;
        _progressBar.lowValue  = 0;
        _progressBar.highValue = holdTime;
        _progressBar.value     = 0;
        _rootVE.style.display  = DisplayStyle.Flex;
    }

    private void EndCollect()
    {
        _isCollecting         = false;
        _timer                = 0f;
        _rootVE.style.display = DisplayStyle.None;
    }

    private void FinishCollect()
    {
        EndCollect();
        gem.SetActive(false);
        LogInfo($"Collected and disabled '{gem.name}'.");
    }

    private void OnAction(InputAction.CallbackContext ctx) {
        LogDebug("Action performed");

        if (gem == null || !gem.activeInHierarchy)
            return;

        if (!_isPlayerNear)
        {
            if (_isCollecting) EndCollect();
            return;
        }

        _isCollecting = true;
        _timer       = 0f;
        _startPos    = _player.position;
        BeginCollect();
    }
}
