using UnityEngine;

namespace Convai.Scripts.Utils
{
    /// <summary>
    ///     This class provides head tracking functionalities for an object (like a character) with an Animator.
    ///     It requires the Animator component to be attached to the same GameObject.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [DisallowMultipleComponent]
    [AddComponentMenu("Convai/Character Head & Eye Tracking")]
    [HelpURL(
        "https://docs.convai.com/api-docs/plugins-and-integrations/unity-plugin/scripts-overview")]
    public class ConvaiHeadTracking : MonoBehaviour
    {
        private const float POSITION_UPDATE_DELAY = 2f;

        [Header("Tracking Properties")] [Tooltip("The object that the head should track.")] [SerializeField]
        private Transform targetObject;

        [Range(0.0f, 100.0f)]
        [Tooltip("The maximum distance at which the head must still track target.")]
        [SerializeField]
        private float trackingDistanceThreshold = 10f;

        [Header("Look At Weights")]
        [Range(0f, 1f)]
        [Tooltip(
            "Controls the amount of rotation applied to the body to achieve the 'Look At' target. The closer to 1, the more the body will rotate to follow the target.")]
        [SerializeField]
        private float bodyLookAtWeight = 0.6f;

        [Range(0f, 1f)]
        [Tooltip(
            "Controls the amount of rotation applied to the head to achieve the 'Look At' target. The closer to 1, the more the head will rotate to follow the target.")]
        [SerializeField]
        private float headLookAtWeight = 0.8f;

        [Range(0f, 1f)]
        [Tooltip(
            "Controls the amount of rotation applied to the eyes to achieve the 'Look At' target. The closer to 1, the more the eyes will rotate to follow the target.")]
        [SerializeField]
        private float eyesLookAtWeight = 1f;

        [Tooltip(
            "Set this to true if you want the character to look away randomly, false to always look at the target")]
        [SerializeField]
        private bool lookAway;

        private Animator _animator;

        private float _appliedBodyLookAtWeight;
        private float _currentLookAtWeight = 1f;
        private float _desiredLookAtWeight = 1f;
        private Transform _headPivot;

        private void Start()
        {
            InitializeTargetObject();
            _animator = GetComponent<Animator>();
            CreateHeadPivot();
            InvokeRepeating(nameof(UpdateTarget), 0, POSITION_UPDATE_DELAY);
        }

        private void Update()
        {
            DrawRayToTarget();
        }

        /// <summary>
        ///     Unity's built-in method called during the IK pass.
        /// </summary>
        public void OnAnimatorIK(int layerIndex)
        {
            PerformHeadTracking();
        }

        private void InitializeTargetObject()
        {
            if (targetObject != null) return;

            Debug.LogWarning("No target object set for head tracking. Setting default target as main camera");
            if (Camera.main != null) targetObject = Camera.main.transform;
        }

        private void CreateHeadPivot()
        {
            _headPivot = new GameObject("HeadPivot").transform;
            _headPivot.transform.parent = transform;
            _headPivot.localPosition = new Vector3(0, 1.6f, 0);
        }

        /// <summary>
        ///     Updates the target weight for the look-at.
        /// </summary>
        private void UpdateTarget()
        {
            _desiredLookAtWeight = lookAway ? Random.Range(0.2f, 1.0f) : 1f;
        }

        /// <summary>
        ///     Performs the head tracking towards the target object.
        /// </summary>
        private void PerformHeadTracking()
        {
            float distance = Vector3.Distance(transform.position, targetObject.position);

            // only perform head tracking if within threshold distance
            if (!(distance < trackingDistanceThreshold / 2)) return;

            _headPivot.transform.LookAt(targetObject); // orient the pivot towards the target object
            SetCurrentLookAtWeight(); // set the current look at weight based on how much rotation is needed

            // limit the head rotation
            float headRotation = _headPivot.localRotation.y;
            if (Mathf.Abs(headRotation) > 0.80f)
            {
                // clamp rotation if more than 80 degrees
                headRotation = Mathf.Sign(headRotation) * 0.80f;
                Quaternion localRotation = _headPivot.localRotation;
                localRotation.y = headRotation;
                _headPivot.localRotation = localRotation;
            }

            // adjust body rotation weight based on how much the head is rotated
            float targetBodyLookAtWeight = Mathf.Abs(_headPivot.localRotation.y) > 0.45f
                ? bodyLookAtWeight / 3f
                : 0f;

            // smooth transition between current and target body rotation weight
            _appliedBodyLookAtWeight = Mathf.Lerp(_appliedBodyLookAtWeight, targetBodyLookAtWeight, Time.deltaTime);

            // Apply rotation weights to the Animator
            AdjustAnimatorLookAt();
        }


        private void SetCurrentLookAtWeight()
        {
            float angleDifference = _headPivot.localRotation.y;

            // Lerp the currentLookAtWeight towards the desiredLookAtWeight or towards 0 if above a certain threshold.
            _currentLookAtWeight = Mathf.Abs(angleDifference) < 0.65f
                ? Mathf.Lerp(Mathf.Clamp(_currentLookAtWeight, 0, 1), Mathf.Clamp(_desiredLookAtWeight, 0, 1),
                    Time.deltaTime * POSITION_UPDATE_DELAY)
                : Mathf.Lerp(Mathf.Clamp(_currentLookAtWeight, 0, 1), 0, Time.deltaTime * POSITION_UPDATE_DELAY);
        }

        /// <summary>
        ///     Method to apply rotation weights to the Animator
        /// </summary>
        private void AdjustAnimatorLookAt()
        {
            // Check if Animator or TargetObject are null
            if (!_animator || targetObject == null)
            {
                // If either is null, set the look-at weight to 0 and return, effectively ending the method early
                _animator.SetLookAtWeight(0);
                return;
            }

            // Set the look-at weights in the Animator.
            // This is used to dictate how much the body, head or eyes should turn to "look at" the target.
            // `Mathf.Clamp` is used to ensure the weight values lie between 0 and 1 (inclusive).
            // The body weight is clamped between 0 to 0.5 since it's less advisable to rotate the body too much versus the head or eyes.
            _animator.SetLookAtWeight(Mathf.Clamp(_currentLookAtWeight, 0, 1),
                Mathf.Clamp(_appliedBodyLookAtWeight, 0, .5f),
                Mathf.Clamp(headLookAtWeight / 1.25f, 0, .8f), Mathf.Clamp(eyesLookAtWeight, 0, 1));

            // Set the look-at position for the Animator (where the body/head/eyes will turn toward)
            _animator.SetLookAtPosition(targetObject.position);
        }

        /// <summary>
        ///     Debug utility to visualize the tracking mechanism
        /// </summary>
        private void DrawRayToTarget()
        {
            Vector3 pos = transform.position;
            // Draw a debug ray from our position to the normalized direction towards the target, scaled by half of the tracking distance threshold.
            // The purpose is to visualize the direction and focus of the head tracking, and it's a useful debug tool in Unity's Scene view.
            // "Normalized" ensures that the vector has a magnitude (length) of 1, keeping the scaling of the vector consistent.
            // This ray appears red in the Scene view.
            Debug.DrawRay(pos,
                (targetObject.position - pos).normalized * trackingDistanceThreshold / 2, Color.red);
        }
    }
}