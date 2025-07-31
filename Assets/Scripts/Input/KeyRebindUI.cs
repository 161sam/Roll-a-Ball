using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RollABall.InputSystem
{
    /// <summary>
    /// UI helper to rebind a single key at runtime.
    /// </summary>
    [AddComponentMenu("Roll-a-Ball/Input/Key Rebind UI")]
    public class KeyRebindUI : MonoBehaviour
    {
        [SerializeField] private string bindingName = "Restart";
        [SerializeField] private TMP_Text keyLabel;
        [SerializeField] private Button rebindButton;

        private void Awake()
        {
            if (rebindButton) rebindButton.onClick.AddListener(StartRebind);
            UpdateLabel(GetCurrentKey());
        }

        private void StartRebind()
        {
            if (InputManager.Instance == null) return;
            if (keyLabel) keyLabel.text = "Press any key...";
            InputManager.Instance.ListenForKey(OnKeyCaptured);
        }

        private void OnKeyCaptured(KeyCode key)
        {
            if (InputManager.Instance == null) return;
            if (bindingName == "Pause")
                InputManager.Instance.SetPauseKey(key);
            else if (bindingName == "Restart")
                InputManager.Instance.SetRestartKey(key);
            UpdateLabel(key);
        }

        private void UpdateLabel(KeyCode key)
        {
            if (keyLabel) keyLabel.text = key.ToString();
        }

        private KeyCode GetCurrentKey()
        {
#if ENABLE_INPUT_SYSTEM
            return KeyCode.None;
#else
            if (InputManager.Instance == null) return KeyCode.None;
            return bindingName == "Pause" ? InputManager.Instance.PauseKey :
                   bindingName == "Restart" ? InputManager.Instance.RestartKey : KeyCode.None;
#endif
        }
    }
}
