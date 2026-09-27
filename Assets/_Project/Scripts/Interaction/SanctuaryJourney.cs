using UnityEngine;
using TMPro;
using PA3.Cinematics;

namespace PA3.Interaction
{
    // Owns the small exploration loop; environment prefabs have no player dependencies.
    public sealed class SanctuaryJourney : MonoBehaviour
    {
        public FirstPersonController Player;
        public IntroSequenceController Intro;
        public MysticRelicInteraction[] Relics;
        public Light SanctuaryLight;
        private Vector3 spawn;
        private bool paused;
        public TMP_Text Heading;
        public TMP_Text Hint;
        public TMP_Text Controls;
        public int Awakened { get; private set; }
        public bool Ready => Intro == null || Intro.IsFinished;

        void Start() { spawn = Player.transform.position; }

        void Update()
        {
            if (!Ready) return;
            if (Input.GetKeyDown(KeyCode.Escape)) SetPaused(!paused);
            if (paused && Input.GetMouseButtonDown(0)) SetPaused(false);
            if (Input.GetKeyDown(KeyCode.R) || Player.transform.position.y < -1.3f) ReturnToShore();
            if (SanctuaryLight != null)
                SanctuaryLight.intensity = Mathf.MoveTowards(SanctuaryLight.intensity, Awakened == Relics.Length ? 7f : 0f, Time.deltaTime * 2);
        }

        public void RegisterAwakening()
        {
            Awakened++;
            Debug.Log("Santuario: memorias despertadas " + Awakened + "/" + Relics.Length);
        }

        void SetPaused(bool value)
        {
            paused = value;
            Player.playerCanMove = Player.cameraCanMove = !value;
            Player.enableJump = !value;
            Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = value;
        }

        public void ReturnToShore()
        {
            var body = Player.GetComponent<Rigidbody>();
            body.linearVelocity = Vector3.zero;
            body.position = spawn;
            body.rotation = Quaternion.identity;
            Player.playerCamera.transform.localPosition = Vector3.zero;
            Debug.Log("Santuario: regreso seguro al inicio.");
        }

        void LateUpdate()
        {
            if (Heading == null || Hint == null || Controls == null) return;
            if (!Ready)
            {
                Heading.text = "EL SANTUARIO DORMIDO";
                Hint.text = "";
                Controls.text = "ESPACIO · omitir introducción";
                return;
            }
            Heading.text = Awakened == Relics.Length ? "EL SANTUARIO HA DESPERTADO" : "MEMORIAS   " + Awakened + " / " + Relics.Length;
            string message = paused ? "Haz clic para continuar" : "";
            if (!paused) foreach (var relic in Relics)
                if (relic != null && relic.InReach && !relic.IsAwakened) { message = "E · despertar memoria"; break; }
            Hint.text = message;
            Controls.text = "WASD · caminar    SHIFT · correr    ESPACIO · saltar    E · interactuar    ESC · cursor    R · volver";
        }
    }
}
