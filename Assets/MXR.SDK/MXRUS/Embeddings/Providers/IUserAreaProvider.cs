using UnityEngine;

namespace MXR.SDK {
    /// <summary>
    /// Metadata embedding for the user area in an MXRUS environment
    /// </summary>
    interface IUserAreaProvider {
        /// <summary>
        /// The start position of the user in this environment
        /// </summary>
        Vector3 UserStartPosition { get; }

        /// <summary>
        /// The start rotation (direction) of the user in this environment.
        /// </summary>
        Quaternion UserStartRotation { get; }

        /// <summary>
        /// The distance the user is allowed to walk around in relative to <see cref="UserStartPosition"/>
        /// </summary>
        float UserWalkableRadius { get; }
    }
}
