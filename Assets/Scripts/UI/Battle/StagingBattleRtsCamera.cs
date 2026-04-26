using UnityEngine;

namespace GlobalDomination.UI.Battle
{
    /// <summary>
    /// Perspective RTS-style camera: pan (WASD, middle-mouse drag), zoom (scroll), orbit (Q/E).
    /// Attached to the staging battle camera after <see cref="StagingBattleWorld"/> builds it.
    /// </summary>
    public sealed class StagingBattleRtsCamera : MonoBehaviour
    {
        [SerializeField] private float zoomPerScroll = 0.12f;
        /// <summary>Minimum distance from focus so the camera never crosses the look-at point (no max — scroll zoom is unbounded).</summary>
        [SerializeField] private float minDistanceFromFocus = 0.08f;
        [SerializeField] private float maxDistanceFromFocus = 1e6f;
        [SerializeField] private float wasdPanSpeed = 22f;
        [SerializeField] private float mousePanSensitivity = 0.42f;
        [SerializeField] private float orbitDegreesPerSecond = 55f;
        [SerializeField] private Vector2 focusClampX = new Vector2(-45f, 45f);
        [SerializeField] private Vector2 focusClampZ = new Vector2(-28f, 48f);

        private Camera _camera;
        private Vector3 _focusOnGround;
        private Vector3 _dirFromFocusToCamera;
        private float _distance;

        /// <summary>Call once after the camera transform and look direction are set.</summary>
        public void Initialize()
        {
            _camera = GetComponent<Camera>();
            if (_camera == null)
            {
                return;
            }

            Plane ground = new Plane(Vector3.up, Vector3.zero);
            Ray ray = new Ray(transform.position, transform.forward);
            if (ground.Raycast(ray, out float enter))
            {
                _focusOnGround = ray.GetPoint(enter);
            }
            else
            {
                _focusOnGround = transform.position + transform.forward * 45f;
                _focusOnGround.y = 0f;
            }

            Vector3 offset = transform.position - _focusOnGround;
            _distance = Mathf.Max(0.5f, offset.magnitude);
            _dirFromFocusToCamera = offset.normalized;
        }

        private void LateUpdate()
        {
            if (_camera == null)
            {
                return;
            }

            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.001f)
            {
                float factor = 1f - scroll * zoomPerScroll;
                _distance = Mathf.Clamp(_distance * factor, minDistanceFromFocus, maxDistanceFromFocus);
            }

            Vector3 right = transform.right;
            right.y = 0f;
            if (right.sqrMagnitude > 0.0001f)
            {
                right.Normalize();
            }

            Vector3 flatForward = transform.forward;
            flatForward.y = 0f;
            if (flatForward.sqrMagnitude > 0.0001f)
            {
                flatForward.Normalize();
            }

            Vector3 pan = Vector3.zero;
            if (Input.GetKey(KeyCode.W))
            {
                pan += flatForward;
            }

            if (Input.GetKey(KeyCode.S))
            {
                pan -= flatForward;
            }

            if (Input.GetKey(KeyCode.A))
            {
                pan -= right;
            }

            if (Input.GetKey(KeyCode.D))
            {
                pan += right;
            }

            if (pan.sqrMagnitude > 1.01f)
            {
                pan.Normalize();
            }

            pan *= wasdPanSpeed * Time.deltaTime;
            _focusOnGround += pan;

            if (Input.GetMouseButton(2))
            {
                float mx = Input.GetAxis("Mouse X");
                float my = Input.GetAxis("Mouse Y");
                _focusOnGround += (-right * mx - flatForward * my) * mousePanSensitivity;
            }

            if (Input.GetKey(KeyCode.Q))
            {
                _dirFromFocusToCamera = Quaternion.AngleAxis(-orbitDegreesPerSecond * Time.deltaTime, Vector3.up)
                    * _dirFromFocusToCamera;
            }

            if (Input.GetKey(KeyCode.E))
            {
                _dirFromFocusToCamera = Quaternion.AngleAxis(orbitDegreesPerSecond * Time.deltaTime, Vector3.up)
                    * _dirFromFocusToCamera;
            }

            _focusOnGround.x = Mathf.Clamp(_focusOnGround.x, focusClampX.x, focusClampX.y);
            _focusOnGround.z = Mathf.Clamp(_focusOnGround.z, focusClampZ.x, focusClampZ.y);
            _focusOnGround.y = 0f;

            transform.position = _focusOnGround + _dirFromFocusToCamera * _distance;
            transform.LookAt(_focusOnGround, Vector3.up);
        }
    }
}
