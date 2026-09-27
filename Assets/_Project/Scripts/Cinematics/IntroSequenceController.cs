using UnityEngine;
using UnityEngine.Playables;
using Unity.Cinemachine;

namespace PA3.Cinematics
{
    [DefaultExecutionOrder(-200)]
    public sealed class IntroSequenceController : MonoBehaviour
    {
        public PlayableDirector Director;
        public CinemachineCamera IntroCamera;
        public GameObject Player;
        public Camera IntroOutput;

        private FirstPersonController _playerController;
        private bool _sequenceCompleted;
        private Camera _camera;
        private CinemachineBrain _brain;
        private Vector3 _cameraPosition;
        private Quaternion _cameraRotation;
        private Rigidbody _body;
        public bool IsFinished => _sequenceCompleted;

        private void Awake()
        {
            _playerController = Player != null ? Player.GetComponentInChildren<FirstPersonController>(true) : null;
            if (_playerController == null) return;
            _camera = _playerController.playerCamera;
            _cameraPosition = Vector3.zero;
            _cameraRotation = Quaternion.identity;
            _brain = _camera.GetComponent<CinemachineBrain>();
            _body = _playerController.GetComponent<Rigidbody>();
            SetPlayerControl(false);
            _playerController.enableJump = false;
            if (IntroOutput != null) { IntroOutput.enabled = true; _camera.enabled = false; }
        }

        private void Start()
        {
            _playerController = Player != null ? Player.GetComponentInChildren<FirstPersonController>(true) : null;
            SetPlayerControl(false);
            if (_body != null) _body.isKinematic = true;
            if (IntroCamera != null) IntroCamera.gameObject.SetActive(true);
            if (Director == null || Director.playableAsset == null)
            {
                CompleteSequence();
                return;
            }

            Director.extrapolationMode = DirectorWrapMode.None;
            Director.stopped += OnSequenceStopped;
            Director.Play();
        }

        private void Update()
        {
            if (_sequenceCompleted || Director == null || Director.playableAsset == null) return;
            if (Input.GetKeyDown(KeyCode.Space)) { CompleteSequence(); return; }
            if (Director.time >= Director.duration - 0.05d)
                CompleteSequence();
        }

        private void OnSequenceStopped(PlayableDirector director)
        {
            CompleteSequence();
        }

        private void CompleteSequence()
        {
            if (_sequenceCompleted) return;
            _sequenceCompleted = true;
            if (Director != null) Director.Stop();
            if (IntroCamera != null) IntroCamera.gameObject.SetActive(false);
            // Cinemachine writes the physical camera transform; disabling a virtual camera
            // alone leaves the player's eyes at the last cinematic position.
            if (_brain != null) _brain.enabled = false;
            if (IntroOutput != null) IntroOutput.enabled = false;
            if (_camera != null)
            {
                _camera.transform.localPosition = _cameraPosition;
                _camera.transform.localRotation = _cameraRotation;
                _camera.fieldOfView = _playerController.fov;
                _camera.enabled = true;
            }
            if (_body != null) { _body.isKinematic = false; _body.linearVelocity = Vector3.zero; }
            SetPlayerControl(true);
            StartCoroutine(EnableJumpAfterRelease());
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Debug.Log("PA3 CINEMATIC: introducción completada; control cedido al jugador.");
        }

        private System.Collections.IEnumerator EnableJumpAfterRelease()
        {
            yield return null;
            while (Input.GetKey(KeyCode.Space)) yield return null;
            if (_playerController != null) _playerController.enableJump = true;
        }

        private void SetPlayerControl(bool enabled)
        {
            if (_playerController == null) return;
            _playerController.playerCanMove = enabled;
            _playerController.cameraCanMove = enabled;
        }

        private void OnDestroy()
        {
            if (Director != null) Director.stopped -= OnSequenceStopped;
            SetPlayerControl(true);
        }
    }
}
