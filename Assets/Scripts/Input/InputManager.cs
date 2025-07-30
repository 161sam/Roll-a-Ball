using UnityEngine;

namespace RollABall.InputSystem
{
    /// <summary>
    /// Simple wrapper around Unity's legacy Input to allow central configuration
    /// of common gameplay keys. This can later be replaced with the new Input System.
    /// </summary>
    [AddComponentMenu("Roll-a-Ball/Input/Input Manager")]
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        [Header("Key Bindings")]
        private const string PauseKeyPref = "PauseKey";
        private const string RestartKeyPref = "RestartKey";

        [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
        [SerializeField] private KeyCode restartKey = KeyCode.R;
        [SerializeField] private KeyCode jumpKey = KeyCode.Space;
        [SerializeField] private KeyCode flyKey = KeyCode.F;
        [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode slideKey = KeyCode.LeftControl;
        // TODO: Allow runtime key rebinding via settings menu
        // TODO: Support gamepad bindings alongside keyboard controls

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadKeyBindings();
        }

        private void LoadKeyBindings()
        {
            if (PlayerPrefs.HasKey(PauseKeyPref))
            {
                if (System.Enum.TryParse(PlayerPrefs.GetString(PauseKeyPref), out KeyCode key))
                    pauseKey = key;
            }

            if (PlayerPrefs.HasKey(RestartKeyPref))
            {
                if (System.Enum.TryParse(PlayerPrefs.GetString(RestartKeyPref), out KeyCode key))
                    restartKey = key;
            }
        }

        public void SetPauseKey(KeyCode key)
        {
            pauseKey = key;
            PlayerPrefs.SetString(PauseKeyPref, key.ToString());
        }

        public void SetRestartKey(KeyCode key)
        {
            restartKey = key;
            PlayerPrefs.SetString(RestartKeyPref, key.ToString());
        }

        /// <summary>
        /// Returns true when the pause key was pressed this frame.
        /// </summary>
        public bool PausePressed => UnityEngine.Input.GetKeyDown(pauseKey);

        /// <summary>
        /// Returns true when the restart key was pressed this frame.
        /// </summary>
        public bool RestartPressed => UnityEngine.Input.GetKeyDown(restartKey);

        public Vector2 Movement => new Vector2(UnityEngine.Input.GetAxis("Horizontal"), UnityEngine.Input.GetAxis("Vertical"));

        public bool JumpPressed => UnityEngine.Input.GetKeyDown(jumpKey);
        public bool FlyHeld => UnityEngine.Input.GetKey(flyKey);
        public bool SprintHeld => UnityEngine.Input.GetKey(sprintKey);
        public bool SlidePressed => UnityEngine.Input.GetKeyDown(slideKey);
        public bool SlideReleased => UnityEngine.Input.GetKeyUp(slideKey);
    }
}
