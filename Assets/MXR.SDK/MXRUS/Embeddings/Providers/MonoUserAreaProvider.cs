using UnityEngine;

namespace MXR.SDK {
    /// <summary>
    /// Used to configure the user position and rotation, and walkable radius using a GameObject on the scene.
    /// Using Gizmos, it draws a primitive avatar at the start position and other visuals 
    /// representing start rotation and walkable radius.
    /// </summary>
    [ExecuteInEditMode]
    public class MonoUserAreaProvider : MonoBehaviour, IUserAreaProvider {
        // consts used for creating avatar gizmos
        private const float BODY_HEIGHT = 1.4f;
        private const float HEAD_RADIUS = 0.2f;
        private const float BODY_WIDTH = 0.4f;
        
        private const float ARM_LENGTH = BODY_HEIGHT / 2;
        private const float ARM_WIDTH = 0.2f;

        private const int PERIMETER_SEGMENTS = 32;
        private const int RING_COUNT = 10;

        // Interface properties
        public Vector3 UserStartPosition => transform.position;
        public Quaternion UserStartRotation => transform.rotation;
        public float UserWalkableRadius => walkableRadius;

        // User configurable
        [SerializeField] private float walkableRadius = 5;

        private void Update() {
            // At runtime the MXR homescreen only uses the interface properties
            // The code in this method is only required in editor mode during environment creation.
            if (Application.isPlaying) return;

            // Allow rotation only along the Y axis
            transform.localEulerAngles = new Vector3(0, transform.localEulerAngles.y, 0);
            if (transform.parent != null)
                transform.parent = null;

            // Prevent resizing of the transform so the visuals don't get skewed or scaled
            transform.localScale = Vector3.one;
        }

        private void OnDrawGizmos() {
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
            Gizmos.matrix = rotationMatrix;

            // Draw walkable area
            Gizmos.color = Color.yellow;
            GizmosX.DrawConcentricCirclesXZ(Vector3.zero, walkableRadius, PERIMETER_SEGMENTS, RING_COUNT);

            // Draw lines to show forward and right directions relative to the avatars rotation
            Gizmos.color = Color.white;
            Gizmos.DrawLine(Vector3.zero, Vector3.forward * 2);
            Gizmos.DrawLine(Vector3.zero, Vector3.right * 2);

            // Draw avatar body
            Gizmos.color = Color.cyan;
            Gizmos.DrawCube(Vector3.up * BODY_HEIGHT / 2, new Vector3(BODY_WIDTH, BODY_HEIGHT, BODY_WIDTH));
            Gizmos.DrawCube(
                (Vector3.up * (BODY_HEIGHT - ARM_WIDTH / 2)) + (Vector3.right * (BODY_WIDTH / 2 + ARM_LENGTH / 2)),
                new Vector3(ARM_LENGTH, ARM_WIDTH, ARM_WIDTH)
            );
            Gizmos.DrawCube(
                (Vector3.up * (BODY_HEIGHT - ARM_WIDTH / 2)) + (Vector3.left * (BODY_WIDTH / 2 + ARM_LENGTH / 2)),
                new Vector3(ARM_LENGTH, ARM_WIDTH, ARM_WIDTH)
            );
            Gizmos.DrawSphere(Vector3.up * BODY_HEIGHT + (transform.up * HEAD_RADIUS), HEAD_RADIUS);

            // Reset Gizmos class
            Gizmos.color = Color.white;
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
