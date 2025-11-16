using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using PixelCameraFollow = Cainos.PixelArtTopDown_Basic.CameraFollow;
using FantasyCameraFollow = global::CameraFollow;
using FantasyPlayerMovement = global::PlayerMovement;
using ShooterPlayer = global::Player;

namespace PathOfValor
{
    /// <summary>
    /// Keeps the PF Player instance alive across scene loads and snaps it to any legacy
    /// spawn markers so teleporting between scenes feels seamless.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerScenePersistence : MonoBehaviour
    {
        private static readonly Dictionary<string, Vector3> FallbackSpawnPositions = new()
        {
            { "Introduction", new Vector3(5.8354f, 0.9188f, 0f) },
            { "LevelOne", new Vector3(8.3799f, 3.4633f, 0f) },
            { "LevelTwo", new Vector3(0.4f, 0f, 0f) },
            { "LevelThree", Vector3.zero },
            { "LevelFour", Vector3.zero }
        };

        private static PlayerScenePersistence instance;

        private readonly List<GameObject> legacyPlayers = new();

        private Rigidbody2D body;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            body = GetComponent<Rigidbody2D>();
            SceneManager.sceneLoaded += HandleSceneLoaded;
            HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var spawn = ResolveLegacySpawnTransform();
            if (spawn != null)
            {
                transform.SetPositionAndRotation(spawn.position, spawn.rotation);
            }
            else if (FallbackSpawnPositions.TryGetValue(scene.name, out var fallback))
            {
                transform.position = fallback;
            }

            ResetVelocity();
            ApplyLevelSpecificOverrides(scene.name);
            UpdateCameraTargets();
            CleanupLegacyPlayers();
        }

        private Transform ResolveLegacySpawnTransform()
        {
            legacyPlayers.Clear();
            AppendLegacyCandidates(FindLegacyPlayers<FantasyPlayerMovement>());
            AppendLegacyCandidates(FindLegacyPlayers<ShooterPlayer>());
            AppendLegacyCandidates(CollectCameraAssignedPlayers());
            AppendLegacyCandidates(FindTaggedObjects("PlayerSpawn"));
            AppendLegacyCandidates(FindTaggedObjects("Respawn"));

            foreach (var candidate in legacyPlayers)
            {
                if (candidate == null || candidate == gameObject)
                {
                    continue;
                }

                return candidate.transform;
            }

            return null;
        }

        private void AppendLegacyCandidates(IEnumerable<GameObject> candidates)
        {
            foreach (var candidate in candidates)
            {
                if (candidate == null || legacyPlayers.Contains(candidate))
                {
                    continue;
                }

                legacyPlayers.Add(candidate);
            }
        }

        private static IEnumerable<GameObject> FindLegacyPlayers<T>() where T : Component
        {
            var components = FindAll<T>();
            foreach (var component in components)
            {
                if (component != null)
                {
                    yield return component.gameObject;
                }
            }
        }

        private static IEnumerable<GameObject> CollectCameraAssignedPlayers()
        {
            var cameras = FindAll<FantasyCameraFollow>();
            foreach (var camera in cameras)
            {
                if (camera != null && camera.PlayerCharacter != null)
                {
                    yield return camera.PlayerCharacter;
                }
            }
        }

        private static IEnumerable<GameObject> FindTaggedObjects(string tag)
        {
            if (!IsTagDefined(tag))
            {
                yield break;
            }

            var taggedObjects = GameObject.FindGameObjectsWithTag(tag);
            foreach (var go in taggedObjects)
            {
                if (go != null)
                {
                    yield return go;
                }
            }
        }

        private static bool IsTagDefined(string tag)
        {
            try
            {
                GameObject.FindGameObjectWithTag(tag);
                return true;
            }
            catch (UnityException)
            {
                return false;
            }
        }

        private void UpdateCameraTargets()
        {
            var fantasyCameras = FindAll<FantasyCameraFollow>();
            foreach (var camera in fantasyCameras)
            {
                if (camera == null) continue;
                camera.SetPlayer(gameObject);
            }

            var pixelCameras = FindAll<PixelCameraFollow>();
            foreach (var camera in pixelCameras)
            {
                if (camera == null) continue;
                camera.SetTarget(transform, true);
            }
        }

        private void CleanupLegacyPlayers()
        {
            foreach (var candidate in legacyPlayers)
            {
                if (candidate == null || candidate == gameObject)
                {
                    continue;
                }

                Destroy(candidate);
            }

            legacyPlayers.Clear();
        }

        private void ResetVelocity()
        {
            if (body == null)
            {
                return;
            }

#if UNITY_2022_2_OR_NEWER
            body.linearVelocity = Vector2.zero;
#else
            body.velocity = Vector2.zero;
#endif
        }

        private void ApplyLevelSpecificOverrides(string sceneName)
        {
            if (sceneName != "LevelTwo")
            {
                return;
            }

            // Force a consistent footprint for LevelTwo regardless of where we teleported from.
            transform.localScale = Vector3.one;

            var controller = GetComponent<Cainos.PixelArtTopDown_Basic.TopDownCharacterController>();
            if (controller != null)
            {
                controller.Speed = 0.6f;
            }

            var spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.drawMode != SpriteDrawMode.Simple)
            {
                spriteRenderer.size = new Vector2(0.1f, 0.1f);
            }

            var box = GetComponent<BoxCollider2D>();
            if (box != null)
            {
                box.size = new Vector2(1f, 1f);
                box.offset = Vector2.zero;
            }
        }

        private static T[] FindAll<T>() where T : UnityEngine.Object
        {
#if UNITY_2023_1_OR_NEWER
            return UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            return UnityEngine.Object.FindObjectsOfType<T>(true);
#endif
        }
    }
}
