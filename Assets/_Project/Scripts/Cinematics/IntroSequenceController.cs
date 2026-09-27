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

        private void Start()
        {
            if (IntroCamera != null) IntroCamera.gameObject.SetActive(true);
            if (Director == null) return;
            Director.stopped += OnSequenceStopped;
            Director.Play();
        }

        private void OnSequenceStopped(PlayableDirector director)
        {
            if (IntroCamera != null) IntroCamera.gameObject.SetActive(false);
            Debug.Log("PA3 CINEMATIC: introducción completada; control cedido al jugador.");
        }

        private void OnDestroy()
        {
            if (Director != null) Director.stopped -= OnSequenceStopped;
        }
    }
}
