using UnityEngine;

namespace RollABall.Environment
{
    /// <summary>
    /// Trigger component that opens a connected gate when the player enters.
    /// </summary>
    [AddComponentMenu("Roll-a-Ball/Environment/Switch Trigger")]
    public class SwitchTrigger : MonoBehaviour
    {
        [SerializeField] private GateController connectedGate;
        private bool activated = false;

        public void SetGate(GateController gate)
        {
            connectedGate = gate;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (activated) return;
            if (other.CompareTag("Player"))
            {
                connectedGate?.TriggerOpen();
                activated = true;
            }
        }
    }
}
