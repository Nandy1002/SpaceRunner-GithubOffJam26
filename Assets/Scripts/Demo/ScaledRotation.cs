using UnityEngine;

namespace SpaceRunner.Demo
{
    public sealed class ScaledRotation : MonoBehaviour
    {
        private void Update() => transform.Rotate(0, 45 * Time.deltaTime, 0, Space.World);
    }
}
