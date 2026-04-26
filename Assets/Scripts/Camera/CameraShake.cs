using UnityEngine;

namespace EliminateGame.Camera
{
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        [SerializeField, Min(0.01f)] private float duration = 0.08f;
        [SerializeField, Min(0.001f)] private float magnitude = 0.05f;

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

        public void Shake()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
                transform.localPosition = originalLocalPosition;
            }

            originalLocalPosition = transform.localPosition;
            shakeCoroutine = StartCoroutine(ShakeCoroutine());
        }

        private System.Collections.IEnumerator ShakeCoroutine()
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                Vector2 randomOffset = Random.insideUnitCircle * magnitude;
                transform.localPosition = originalLocalPosition + new Vector3(randomOffset.x, randomOffset.y, 0f);
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
