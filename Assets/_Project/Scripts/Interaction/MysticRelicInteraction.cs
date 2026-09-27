using System.Collections;
using UnityEngine;

namespace PA3.Interaction
{
    public sealed class MysticRelicInteraction : MonoBehaviour
    {
        public ParticleSystem ActivationVfx;
        public AudioSource FeedbackAudio;
        public Renderer CoreRenderer;
        public Light PulseLight;
        public float CooldownSeconds = 2f;
        public SanctuaryJourney Journey;
        public bool InReach { get; private set; }
        public bool IsAwakened { get; private set; }
        private Vector3 coreOrigin;

        private static readonly int EmissionStrengthId = Shader.PropertyToID("_EmissionStrength");
        private MaterialPropertyBlock propertyBlock;
        private float lastActivation = -100f;

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
            if (PulseLight != null) PulseLight.intensity = 0f;
            if (CoreRenderer != null) coreOrigin = CoreRenderer.transform.localPosition;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<FirstPersonController>() == null) return;
            InReach = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<FirstPersonController>() != null) InReach = false;
        }

        private void Update()
        {
            if (CoreRenderer != null)
            {
                CoreRenderer.transform.localPosition = coreOrigin + Vector3.up * (.09f * Mathf.Sin(Time.time * 1.7f));
                CoreRenderer.transform.Rotate(0, 20 * Time.deltaTime, 0, Space.Self);
            }
            if (InReach && !IsAwakened && (Journey == null || Journey.Ready) && Cursor.lockState == CursorLockMode.Locked && Input.GetKeyDown(KeyCode.E)) Activate();
        }

        public void Activate()
        {
            if (IsAwakened || Time.time < lastActivation + CooldownSeconds) return;
            IsAwakened = true;
            if (Journey != null) Journey.RegisterAwakening();
            lastActivation = Time.time;
            if (ActivationVfx != null)
            {
                ActivationVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ActivationVfx.Play(true);
            }
            if (FeedbackAudio != null && FeedbackAudio.clip != null) FeedbackAudio.Play();
            StopAllCoroutines();
            StartCoroutine(PulseFeedback());
            Debug.Log("PA3 INTERACTION: la reliquia mística respondió al jugador.");
        }

        private IEnumerator PulseFeedback()
        {
            const float duration = 1.25f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var pulse = Mathf.Sin(t * Mathf.PI);
                if (CoreRenderer != null)
                {
                    CoreRenderer.GetPropertyBlock(propertyBlock);
                    propertyBlock.SetFloat(EmissionStrengthId, Mathf.Lerp(2.7f, 8f, pulse));
                    CoreRenderer.SetPropertyBlock(propertyBlock);
                }
                if (PulseLight != null) PulseLight.intensity = Mathf.Lerp(1.5f, 5f, pulse);
                yield return null;
            }
            if (PulseLight != null) PulseLight.intensity = 1.5f;
            if (CoreRenderer != null)
            {
                propertyBlock.SetFloat(EmissionStrengthId, 5f);
                CoreRenderer.SetPropertyBlock(propertyBlock);
            }
        }
    }
}
