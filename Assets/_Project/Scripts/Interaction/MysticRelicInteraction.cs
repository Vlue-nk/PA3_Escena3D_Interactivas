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

        private static readonly int EmissionStrengthId = Shader.PropertyToID("_EmissionStrength");
        private MaterialPropertyBlock propertyBlock;
        private float lastActivation = -100f;

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
            if (PulseLight != null) PulseLight.intensity = 0f;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            Activate();
        }

        public void Activate()
        {
            if (Time.time < lastActivation + CooldownSeconds) return;
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
                if (PulseLight != null) PulseLight.intensity = Mathf.Lerp(0f, 5f, pulse);
                yield return null;
            }
            if (PulseLight != null) PulseLight.intensity = 0f;
        }
    }
}
