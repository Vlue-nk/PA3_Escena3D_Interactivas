using UnityEngine;
using UnityEngine.Playables;
using Unity.Cinemachine;

namespace PA3.Cinematics
{
    public sealed class IntroSequenceController : MonoBehaviour
    {
        public PlayableDirector Director;
        public CinemachineCamera IntroCamera;
        public GameObject Player;

        private FirstPersonController _playerController;
        private bool _sequenceCompleted;

        private void Start()
        {
            _playerController = Player != null ? Player.GetComponentInChildren<FirstPersonController>(true) : null;
            SetPlayerControl(false);
            if (IntroCamera != null) IntroCamera.gameObject.SetActive(true);
            if (Director == null)
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
            if (IntroCamera != null) IntroCamera.gameObject.SetActive(false);
            SetPlayerControl(true);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Debug.Log("PA3 CINEMATIC: introducción completada; control cedido al jugador.");
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
