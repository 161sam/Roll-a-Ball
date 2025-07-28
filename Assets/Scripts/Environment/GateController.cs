using UnityEngine;

namespace RollABall.Environment
{
    /// <summary>
    /// Simple gate controller that can be opened via script.
    /// </summary>
    [AddComponentMenu("Roll-a-Ball/Environment/Gate Controller")]
    public class GateController : MonoBehaviour
    {
        [SerializeField] private GameObject gateObject;
        private bool opened = false;

        private void Awake()
        {
            if (!gateObject)
                gateObject = gameObject;
        }

        /// <summary>
        /// Opens the gate by disabling visuals and collider.
        /// </summary>
        public void TriggerOpen()
        {
            if (opened) return;
            opened = true;

            if (gateObject)
            {
                var col = gateObject.GetComponent<Collider>();
                if (col) col.enabled = false;
                var rend = gateObject.GetComponent<Renderer>();
                if (rend) rend.enabled = false;
            }
        }
    }
}
