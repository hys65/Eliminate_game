using UnityEngine;

namespace EliminateGame.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class SfxController : MonoBehaviour
    {
        public static SfxController Instance { get; private set; }

        [SerializeField] private AudioClip selectionClickClip;
        [SerializeField] private AudioClip patternHitClip;
        [SerializeField] private AudioClip comboHitClip;

        private AudioSource audioSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            audioSource = GetComponent<AudioSource>();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void PlaySelectionClick()
        {
            PlayClip(selectionClickClip);
        }

        public void PlayPatternHit(int comboCount)
        {
            if (comboCount >= 2)
            {
                if (comboHitClip != null)
                {
                    PlayClip(comboHitClip);
                    return;
                }

                if (patternHitClip != null)
                {
                    PlayClipWithPitch(patternHitClip, 1.12f);
                }

                return;
            }

            PlayClip(patternHitClip);
        }

        private void PlayClip(AudioClip clip)
        {
            if (clip == null || audioSource == null)
            {
                return;
            }

            float originalPitch = audioSource.pitch;
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(clip);
            audioSource.pitch = originalPitch;
        }

        private void PlayClipWithPitch(AudioClip clip, float pitch)
        {
            if (clip == null || audioSource == null)
            {
                return;
            }

            float originalPitch = audioSource.pitch;
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(clip);
            audioSource.pitch = originalPitch;
        }
    }
}
