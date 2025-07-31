using System.Collections.Generic;
using UnityEngine;

namespace RollABall.Environment
{
    /// <summary>
    /// Controls a sequence of gates that must be opened in a defined order.
    /// </summary>
    [AddComponentMenu("Roll-a-Ball/Environment/Gate Sequence Controller")]
    public class GateSequenceController : MonoBehaviour
    {
        [SerializeField]
        private List<SteampunkGateController> gates = new List<SteampunkGateController>();

        private int currentStep = 0;

        /// <summary>
        /// Returns true if the gate with the given index is allowed to open.
        /// </summary>
        public bool IsStepActive(int index)
        {
            return index == currentStep;
        }

        /// <summary>
        /// Notify the controller that a gate for the current step has opened.
        /// </summary>
        public void NotifyGateOpened(int index)
        {
            if (index == currentStep)
            {
                currentStep++;
            }
        }

        private void OnValidate()
        {
            gates.RemoveAll(g => g == null);
        }
    }
}
