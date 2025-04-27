using UnityEngine;
using Spine;
using Spine.Unity;

public class SpineLegIK : MonoBehaviour {
    [Header("Spine Setup")]
    [Tooltip("Drag your SkeletonAnimation here.")]
    public SkeletonAnimation skeletonAnimation;

    [Header("Foot Targets (4)")]
    [Tooltip("Place your four foot-target Transforms in this order: FrontRight, FrontLeft, BackRight, BackLeft")]
    public Transform[] footTargets = new Transform[4];

    [Header("Ground & IK Settings")]
    public LayerMask groundMask;
    public float rayHeight     = 1.0f;   // start ray this high above target
    public float footOffsetY   = 0.05f;  // lift off ground a bit
    public int   ccdIterations = 5;
    public float ccdTolerance  = 0.01f;

    [Header("Debug Gizmos")]
    public bool drawGizmos = true;
    public Color footTargetColor = Color.green;
    public Color rayColor        = Color.yellow;
    public Color chainColor      = Color.cyan;
    public float gizmoSphereRadius = 0.05f;

    // internal bone chains: [0]=FrontRight, [1]=FrontLeft, [2]=BackRight, [3]=BackLeft
    private Bone[][] boneChains;

    void Awake() {
        var skel = skeletonAnimation.Skeleton;
        boneChains = new Bone[][] {
            new [] {
                //skel.FindBone("FrontForegroundThigh-1"),
                skel.FindBone("FrontForegroundThigh-2"),
                //skel.FindBone("FrontForegroundKnee"),
                skel.FindBone("FrontForegroundFoot")
            },
            new [] {
                //skel.FindBone("FrontBackgroundThigh-1"),
                skel.FindBone("FrontBackgroundThigh-2"),
                //skel.FindBone("FrontBackgroundKnee"),
                skel.FindBone("FrontBackgroundFoot")
            },
            new [] {
                //skel.FindBone("BackForegroundThigh-1"),
                skel.FindBone("BackForegroundThigh-2"),
                //skel.FindBone("BackForegroundKnee"),
                skel.FindBone("BackForegroundFoot")
            },
            new [] {
                //skel.FindBone("BackBackgroundThigh-1"),
                skel.FindBone("BackBackgroundThigh-2"),
                //skel.FindBone("BackBackgroundKnee"),
                skel.FindBone("BackBackgroundFoot")
            }
        };
    }

    void LateUpdate() {
        // 1) place & solve each leg
        for (int i = 0; i < boneChains.Length; i++) {
            if (i < footTargets.Length && footTargets[i] != null) {
                Vector3 worldTarget = PlaceFoot(footTargets[i]);
                SolveCCD(boneChains[i], worldTarget);
            }
        }

        // 2) Update world transforms with Spine Physics API
        skeletonAnimation.Skeleton.UpdateWorldTransform(Skeleton.Physics.Update);
    }

    Vector3 PlaceFoot(Transform footT) {
        Vector3 origin = footT.position + Vector3.up * rayHeight;
        if (Physics.Raycast(origin, Vector3.down, out var hit, rayHeight * 2, groundMask)) {
            Vector3 p = hit.point + Vector3.up * footOffsetY;
            footT.position = p;
            return p;
        }
        return footT.position;
    }

    void SolveCCD(Bone[] chain, Vector3 targetWorld) {
        // convert world→skeleton local
        Vector3 local3 = skeletonAnimation.transform.InverseTransformPoint(targetWorld);
        Vector2 targetLocal = new Vector2(local3.x, local3.y);

        // cache world positions
        Vector2[] pos = new Vector2[chain.Length];
        for (int i = 0; i < chain.Length; i++)
            pos[i] = new Vector2(chain[i].WorldX, chain[i].WorldY);

        // CCD iterations
        for (int iter = 0; iter < ccdIterations; iter++) {
            for (int i = chain.Length - 2; i >= 0; i--) {
                Vector2 bonePos  = pos[i];
                Vector2 effPos   = pos[chain.Length - 1];
                Vector2 toEff    = (effPos - bonePos).normalized;
                Vector2 toTarg   = (targetLocal - bonePos).normalized;
                float deltaAngle = Vector2.SignedAngle(toEff, toTarg);
                chain[i].Rotation += deltaAngle;

                // apply intermediate transform
                skeletonAnimation.Skeleton.UpdateWorldTransform(Skeleton.Physics.Update);

                // recompute positions
                for (int j = i; j < chain.Length; j++)
                    pos[j] = new Vector2(chain[j].WorldX, chain[j].WorldY);

                if ((pos[chain.Length - 1] - targetLocal).sqrMagnitude < ccdTolerance * ccdTolerance)
                    return;
            }
        }
    }

    void OnDrawGizmos() {
        if (!drawGizmos || footTargets == null)
            return;

        // Draw foot targets and rays
        Gizmos.color = footTargetColor;
        foreach (var foot in footTargets) {
            if (foot == null) continue;
            Gizmos.DrawWireSphere(foot.position, gizmoSphereRadius);
            Vector3 origin = foot.position + Vector3.up * rayHeight;
            Gizmos.color = rayColor;
            Gizmos.DrawLine(origin, origin + Vector3.down * (rayHeight * 2));
            Gizmos.color = footTargetColor;
        }

        // Draw IK chains (only in play mode when boneChains populated)
        if (Application.isPlaying && boneChains != null) {
            Gizmos.color = chainColor;
            foreach (var chain in boneChains) {
                Vector3 prev = Vector3.zero;
                bool first = true;
                foreach (var bone in chain) {
                    Vector3 world = skeletonAnimation.transform.TransformPoint(bone.WorldX, bone.WorldY, 0);
                    if (first) {
                        prev = world;
                        first = false;
                    } else {
                        Gizmos.DrawLine(prev, world);
                        prev = world;
                    }
                }
            }
        }
    }
}