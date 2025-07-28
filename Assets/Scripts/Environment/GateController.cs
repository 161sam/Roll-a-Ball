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
        [SerializeField] private bool requiresAllCollectibles = false; // Gate requires all collectibles to be opened
        [SerializeField] private bool requiresSwitchTrigger = true;
        [SerializeField] private bool debugMode = false;

        public bool RequiresAllCollectibles
        {
            get => requiresAllCollectibles;
            set => requiresAllCollectibles = value;
        }

        public bool RequiresSwitchTrigger
        {
            get => requiresSwitchTrigger;
            set => requiresSwitchTrigger = value;
        }

        private bool opened = false;
        private bool switchActivated = false;

        private void Awake()
        {
            if (!gateObject)
                gateObject = gameObject;
        }

        private void Start()
        {
            if (requiresAllCollectibles && LevelManager.Instance)
            {
                LevelManager.Instance.OnCollectibleCountChanged += OnCollectibleCountChanged;

                // Check current state in case collectibles are already gathered
                if (LevelManager.Instance.CollectiblesRemaining == 0 && !requiresSwitchTrigger)
                {
                    OnCollectibleCountChanged(0, LevelManager.Instance.TotalCollectibles);
                }
            }
        }

        private void OnDestroy()
        {
            if (requiresAllCollectibles && LevelManager.Instance)
            {
                LevelManager.Instance.OnCollectibleCountChanged -= OnCollectibleCountChanged;
            }
        }

        /// <summary>
        /// Opens the gate by disabling visuals and collider.
        /// </summary>
        public void TriggerOpen(bool fromSwitch = false)
        {
            if (opened) return;

            if (fromSwitch)
                switchActivated = true;

            if (requiresSwitchTrigger && !switchActivated)
                return;

            if (requiresAllCollectibles && LevelManager.Instance && LevelManager.Instance.CollectiblesRemaining > 0)
            {
                if (debugMode)
                    Debug.Log($"Gate '{name}' waiting for collectibles. Remaining: {LevelManager.Instance.CollectiblesRemaining}");
                return;
            }

            opened = true;

            if (gateObject)
            {
                var col = gateObject.GetComponent<Collider>();
                if (col) col.enabled = false;
                var rend = gateObject.GetComponent<Renderer>();
                if (rend) rend.enabled = false;
            }

            if (debugMode)
            {
                string reason = fromSwitch ? "switch" : "collectibles";
                Debug.Log($"Gate '{name}' opened via {reason}.");
            }
        }

        private void OnCollectibleCountChanged(int remaining, int total)
        {
            if (remaining <= 0)
            {
                if (debugMode)
                    Debug.Log($"Gate '{name}' opening due to collectibles collected.");

                TriggerOpen();
            }
        }
    }
}
