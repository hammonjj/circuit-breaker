using Spine.Unity;
using UnityEngine;

[RequireComponent(typeof(SkeletonAnimation))]
public class PlayerAnim : MonoBehaviour
{
    [SpineAnimation] public string Idle, Walk, Jump, Attack, Climb, WallJump;
    private SkeletonAnimation _skeleton;
    private string _currentAnimation;

    [SerializeField] private string idleAnim = "Idle";
    [SerializeField] private string walkAnim = "Walk";
    [SerializeField] private string jumpAnim = "Jump";
    [SerializeField] private string attackAnim = "Attack";
    [SerializeField] private string climbAnim = "Climb";
    [SerializeField] private string wallJumpAnim = "WallJump";

    [SerializeField] private float flipThreshold = 0.01f;
    [SerializeField] private bool defaultFacingRight = true;

    private void Awake()
    {
        _skeleton = GetComponent<SkeletonAnimation>();
    }

    public void TickUpdate(Vector2 input, bool grounded)
    {
        // Flip sprite
        if (Mathf.Abs(input.x) > flipThreshold)
        {
            bool movingRight = input.x > 0;
            bool shouldFaceRight = defaultFacingRight ? movingRight : !movingRight;
            _skeleton.Skeleton.ScaleX = shouldFaceRight ? 1f : -1f;
        }

        // Handle animation state
        if (!grounded)
        {
            ClearAnimation();
            //SetAnimation(Jump);
        }
        else if (Mathf.Abs(input.x) > 0.1f)
        {
            SetAnimation(Walk);
        }
        else
        {
            ClearAnimation();
            //SetAnimation(Idle);
        }
    }

    private void SetAnimation(string animation, bool loop = true)
    {
        if (_currentAnimation == animation || string.IsNullOrWhiteSpace(animation)) return;
        Debug.Log($"Setting animation to {animation}");
        _skeleton.AnimationState.SetAnimation(0, animation, loop);
        _currentAnimation = animation;
    }

    public void ClearAnimation()
    {
        if (_currentAnimation == null) { return; }

        _skeleton.AnimationState.ClearTrack(0);
        _currentAnimation = null;
    }
}
