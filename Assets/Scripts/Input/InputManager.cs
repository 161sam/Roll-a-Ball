using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using System.Collections;

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

        private bool waitingForKey;
        private System.Action<KeyCode> onKeyCaptured;

        [Header("Key Bindings")]
        private const string PauseKeyPref = "PauseKey";
        private const string RestartKeyPref = "RestartKey";

#if ENABLE_INPUT_SYSTEM
        [Header("Input Actions")]
        [SerializeField] private InputActionAsset inputActions;
        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction flyAction;
        private InputAction sprintAction;
        private InputAction slideAction;
        private InputAction pauseAction;
        private InputAction restartAction;
#else
        [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
        [SerializeField] private KeyCode restartKey = KeyCode.R;
        [SerializeField] private KeyCode jumpKey = KeyCode.Space;
        [SerializeField] private KeyCode flyKey = KeyCode.F;
        [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode slideKey = KeyCode.LeftControl;
#endif
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

#if ENABLE_INPUT_SYSTEM
            InitializeInputActions();
#else
            LoadKeyBindings();
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private void OnEnable()
        {
            inputActions?.Enable();
        }

        private void OnDisable()
        {
            inputActions?.Disable();
        }
#endif
#if ENABLE_INPUT_SYSTEM
        private void InitializeInputActions()
        {
            if (inputActions == null)
            {
                // Attempt to load a default asset when none is assigned
                inputActions = Resources.Load<InputActionAsset>("Input/InputActions");
                if (inputActions == null)
                {
                    Debug.LogWarning("[InputManager] No InputActionAsset assigned and none found in Resources.", this);
                    return;
                }
            }

            moveAction = inputActions.FindAction("Move");
            jumpAction = inputActions.FindAction("Jump");
            flyAction = inputActions.FindAction("Fly");
            sprintAction = inputActions.FindAction("Sprint");
            slideAction = inputActions.FindAction("Slide");
            pauseAction = inputActions.FindAction("Pause");
            restartAction = inputActions.FindAction("Restart");

            inputActions.Enable();
        }
#endif

#if !ENABLE_INPUT_SYSTEM
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
#endif

        public void SetPauseKey(KeyCode key)
        {
#if !ENABLE_INPUT_SYSTEM
            pauseKey = key;
            PlayerPrefs.SetString(PauseKeyPref, key.ToString());
#else
            pauseAction?.ApplyBindingOverride(0, key.ToString());
#endif
        }

        public void SetRestartKey(KeyCode key)
        {
#if !ENABLE_INPUT_SYSTEM
            restartKey = key;
            PlayerPrefs.SetString(RestartKeyPref, key.ToString());
#else
            restartAction?.ApplyBindingOverride(0, key.ToString());
#endif
        }

#if !ENABLE_INPUT_SYSTEM
        public KeyCode PauseKey => pauseKey;
        public KeyCode RestartKey => restartKey;
#endif

        /// <summary>
        /// Returns true when the pause command was triggered this frame.
        /// </summary>
#if ENABLE_INPUT_SYSTEM
        public bool PausePressed => pauseAction != null && pauseAction.WasPressedThisFrame();
#else
        public bool PausePressed => UnityEngine.Input.GetKeyDown(pauseKey);
#endif

        /// <summary>
        /// Returns true when the restart command was triggered this frame.
        /// </summary>
#if ENABLE_INPUT_SYSTEM
        public bool RestartPressed => restartAction != null && restartAction.WasPressedThisFrame();
#else
        public bool RestartPressed => UnityEngine.Input.GetKeyDown(restartKey);
#endif

        public Vector2 Movement
#if ENABLE_INPUT_SYSTEM
            => moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
#else
            => new Vector2(UnityEngine.Input.GetAxis("Horizontal"), UnityEngine.Input.GetAxis("Vertical"));
#endif

        public bool JumpPressed
#if ENABLE_INPUT_SYSTEM
            => jumpAction != null && jumpAction.WasPressedThisFrame();
#else
            => UnityEngine.Input.GetKeyDown(jumpKey) || UnityEngine.Input.GetButtonDown("Jump");
#endif

        public bool FlyHeld
#if ENABLE_INPUT_SYSTEM
            => flyAction != null && flyAction.IsPressed();
#else
            => UnityEngine.Input.GetKey(flyKey) || UnityEngine.Input.GetButton("Fire1");
#endif

        public bool SprintHeld
#if ENABLE_INPUT_SYSTEM
            => sprintAction != null && sprintAction.IsPressed();
#else
            => UnityEngine.Input.GetKey(sprintKey) || UnityEngine.Input.GetButton("Fire3");
#endif

        public bool SlidePressed
#if ENABLE_INPUT_SYSTEM
            => slideAction != null && slideAction.WasPressedThisFrame();
#else
            => UnityEngine.Input.GetKeyDown(slideKey) || UnityEngine.Input.GetButtonDown("Fire2");
#endif

        public bool SlideReleased
#if ENABLE_INPUT_SYSTEM
            => slideAction != null && slideAction.WasReleasedThisFrame();
#else
            => UnityEngine.Input.GetKeyUp(slideKey) || UnityEngine.Input.GetButtonUp("Fire2");
#endif

        public void ListenForKey(System.Action<KeyCode> callback)
        {
            if (waitingForKey) return;
            onKeyCaptured = callback;
            StartCoroutine(WaitForKey());
        }

        private IEnumerator WaitForKey()
        {
            waitingForKey = true;
            while (!UnityEngine.Input.anyKeyDown)
            {
                yield return null;
            }
            foreach (KeyCode code in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (UnityEngine.Input.GetKeyDown(code))
                {
                    onKeyCaptured?.Invoke(code);
                    break;
                }
            }
            waitingForKey = false;
            // TODO: Switch to Input System events to capture keys without polling
        }
    }
}
