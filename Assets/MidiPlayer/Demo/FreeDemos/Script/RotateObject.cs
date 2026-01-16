using UnityEngine;

namespace DemoMPTK
{
    public class RotateObject : MonoBehaviour
    {
        public Vector3 RotateAmount; // degrees per second to rotate in each axis. Set in inspector.

        // Update is called once per frame
        private void Update()
        {
            transform.Rotate(RotateAmount * Time.unscaledDeltaTime);
        }
    }
}