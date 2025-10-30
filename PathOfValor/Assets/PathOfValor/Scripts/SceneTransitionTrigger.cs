using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PathOfValor
{
    [RequireComponent(typeof(Collider2D))]
    public class SceneTransitionTrigger : MonoBehaviour
    {
        public enum ActivationMode
        {
            AutoOnEnter,
            PressKey
        }

        [SerializeField] private ActivationMode activationMode = ActivationMode.PressKey;
        [SerializeField] private string sceneToLoad = "LevelOne";
        [SerializeField] private KeyCode activationKey = KeyCode.E;
        [FormerlySerializedAs("autoLoadOnEnter")]
        [SerializeField, HideInInspector] private bool autoLoadLegacy;
#if UNITY_EDITOR
        [SerializeField] private SceneAsset sceneAsset;
#endif

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
            if (activationMode == ActivationMode.AutoOnEnter)
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
            if (!playerInRange || activationMode != ActivationMode.PressKey) return;

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

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (autoLoadLegacy)
            {
                activationMode = ActivationMode.AutoOnEnter;
                autoLoadLegacy = false;
            }

            if (sceneAsset == null) return;

            var scenePath = AssetDatabase.GetAssetPath(sceneAsset);
            if (string.IsNullOrEmpty(scenePath))
            {
                sceneToLoad = sceneAsset.name;
                return;
            }

            sceneToLoad = Path.GetFileNameWithoutExtension(scenePath);
        }
#endif
    }
}
