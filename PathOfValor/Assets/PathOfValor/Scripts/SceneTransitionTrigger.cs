using UnityEngine;
using UnityEngine.SceneManagement;

namespace PathOfValor
{
    [RequireComponent(typeof(Collider2D))]
    public class SceneTransitionTrigger : MonoBehaviour
    {
        [SerializeField] private string sceneToLoad = "LevelOne";
        [SerializeField] private KeyCode activationKey = KeyCode.E;
        [SerializeField] private bool autoLoadOnEnter;

        private bool playerInRange;

        private void Reset()
        {
            var collider = GetComponent<Collider2D>();
            if (collider != null) collider.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            playerInRange = true;
            if (autoLoadOnEnter)
            {
                LoadScene();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            playerInRange = false;
        }

        private void Update()
        {
            if (autoLoadOnEnter || !playerInRange) return;

            if (Input.GetKeyDown(activationKey))
            {
                LoadScene();
            }
        }

        private void LoadScene()
        {
            if (string.IsNullOrEmpty(sceneToLoad))
            {
                Debug.LogWarning($"{nameof(SceneTransitionTrigger)} on {name} has no scene set to load.", this);
                return;
            }

            SceneManager.LoadScene(sceneToLoad);
        }
    }
}
