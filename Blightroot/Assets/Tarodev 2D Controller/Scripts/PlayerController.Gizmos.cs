using UnityEngine;

namespace TarodevController {
  public partial class PlayerController {
    [Header("Gizmos")]
    [SerializeField] private bool   _drawJumpIndicator        = true;
    [SerializeField] private float  _jumpIndicatorRadius      = 2f;
    [SerializeField, Range(4, 64)]
    private int    _jumpIndicatorSegments  = 24;
    [SerializeField] private Color  _jumpIndicatorArcColor    = Color.green;
    [SerializeField] private Color  _jumpIndicatorPointColor  = Color.red;
    [SerializeField] private float  _jumpIndicatorPointSize   = 0.1f;

    private void OnDrawGizmos() {
      if (!_drawGizmos) return;
      DrawCharacterBoundsGizmo();
      DrawGroundCollisionRaysGizmo();
      DrawWallDetectionBoundsGizmo();
      DrawGroundDirectionRayGizmo();
      DrawJumpIndicatorGizmo();
    }

    private void DrawCharacterBoundsGizmo() {
      Vector3 center = transform.position + Vector3.up * (_character.Height / 2f);
      Vector3 size   = new Vector3(_character.Width, _character.Height,   0f);
      Gizmos.color   = Color.red;
      Gizmos.DrawWireCube(center, size);
    }

    private void DrawGroundCollisionRaysGizmo() {
      Vector3 origin = transform.position + Vector3.up * _character.StepHeight;
      Vector3 dir    = Vector3.down    * _character.StepHeight;
      Gizmos.color   = Color.magenta;
      Gizmos.DrawRay(origin, dir);

      foreach (var offset in GenerateRayOffsets()) {
        Vector3 off = Vector3.right * offset;
        Gizmos.DrawRay(origin + off, dir);
        Gizmos.DrawRay(origin - off, dir);
      }
    }

    private void DrawWallDetectionBoundsGizmo() {
      Vector3 center = transform.position + (Vector3)_wallDetectionBounds.center;
      Vector3 size   = _wallDetectionBounds.size;
      Gizmos.color   = Color.yellow;
      Gizmos.DrawWireCube(center, size);
    }

    private void DrawGroundDirectionRayGizmo() {
      Vector3 rayOrigin = (Vector3)RayPoint;
      Gizmos.color      = Color.black;
      Gizmos.DrawRay(rayOrigin, Vector3.right);
    }

    private void DrawJumpIndicatorGizmo() {
      if (!_drawJumpIndicator) return;

      Vector3 center = transform.position;
      Gizmos.color   = _jumpIndicatorArcColor;

      // semi‑circle arc
      Vector3 prev = center + Vector3.right * _jumpIndicatorRadius;
      for (int i = 1; i <= _jumpIndicatorSegments; i++) {
        float t     = (float)i / _jumpIndicatorSegments;
        float ang   = Mathf.PI * t; // 0→π
        Vector3 next = center + new Vector3(
          Mathf.Cos(ang),
          Mathf.Sin(ang),
          0f
        ) * _jumpIndicatorRadius;
        Gizmos.DrawLine(prev, next);
        prev = next;
      }

      // key‑angle markers
      Gizmos.color = _jumpIndicatorPointColor;
      Gizmos.DrawSphere(center + Vector3.right * _jumpIndicatorRadius, _jumpIndicatorPointSize);
      Gizmos.DrawSphere(center + Vector3.up    * _jumpIndicatorRadius, _jumpIndicatorPointSize);
      Gizmos.DrawSphere(center + Vector3.left  * _jumpIndicatorRadius, _jumpIndicatorPointSize);
    }
  }
}
