using UnityEngine;

namespace TarodevController.Demo
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private float _smoothTime = 0.3f;
        [SerializeField] private Vector3 _offset = new Vector3(0, 1);
        [SerializeField] private float _lookAheadDistance = 2;
        [SerializeField] private float _lookAheadSpeed = 1;

        private Vector3 _velOffset;
        private Vector3 _vel;
        private Vector3 _lookAheadVel;
        private Transform _playerTransform;
        private IPlayerController _playerController;
        

        private void Awake() 
        {
            InitializePlayer();   
        }

        private void InitializePlayer()
        {
            if (_playerTransform != null) 
            {
                return;
            }

            var player = GameObject.FindGameObjectWithTag("Player");
            _playerTransform = player?.transform;
            _playerTransform?.TryGetComponent(out _playerController);
        }

        private void LateUpdate()
        {
            if (_playerController != null)
            {
                var projectedPos = _playerController.Velocity.normalized * _lookAheadDistance;
                _velOffset = Vector3.SmoothDamp(_velOffset, projectedPos, ref _lookAheadVel, _lookAheadSpeed);
            }

            Step(_smoothTime);
        }

        private void OnValidate() => Step(0);

        private void Step(float time)
        {
            if(_playerTransform == null) {
                return;
            }
            
            var goal = _playerTransform.position + _offset + _velOffset;
            goal.z = -10;
            transform.position = Vector3.SmoothDamp(transform.position, goal, ref _vel, time);
        }
    }
}