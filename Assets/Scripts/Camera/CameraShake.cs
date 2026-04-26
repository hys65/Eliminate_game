using UnityEngine;

namespace EliminateGame.Camera
{
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        private Coroutine shakeCoroutine;
        private Vector3 originalLocalPosition;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            originalLocalPosition = transform.localPosition;
        }

        public void Shake(float duration = 0.08f, float magnitude = 0.045f)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
                transform.localPosition = originalLocalPosition;
                shakeCoroutine = null;
            }

            originalLocalPosition = transform.localPosition;
            shakeCoroutine = StartCoroutine(ShakeCoroutine(duration, magnitude));
        }

        private System.Collections.IEnumerator ShakeCoroutine(float duration, float magnitude)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                Vector2 randomOffset = Random.insideUnitCircle * magnitude;
                transform.localPosition = new Vector3(
                    originalLocalPosition.x + randomOffset.x,
                    originalLocalPosition.y + randomOffset.y,
                    originalLocalPosition.z);
                yield return null;
            }

            transform.localPosition = originalLocalPosition;
            shakeCoroutine = null;
        }

        private void OnDisable()
        {
            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
                shakeCoroutine = null;
            }

            transform.localPosition = originalLocalPosition;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
