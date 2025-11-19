using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PathOfValor
{
    [ExecuteAlways]
    public class ScenePlayerSpawner : MonoBehaviour
    {
        private const string DefaultPlayerResourcePath = "Prefabs/player_main";

        private static GameObject cachedPlayerPrefab;

        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private bool snapCameraOnSpawn = true;
        [SerializeField] private string playerResourcePath = DefaultPlayerResourcePath;

        private void OnEnable()
        {
            TrySpawnOrRepositionPlayer();
        }

        private void TrySpawnOrRepositionPlayer()
        {
            if (!IsSceneContextValid())
                return;

            var prefabToSpawn = ResolvePlayerPrefab();
            if (prefabToSpawn == null)
                return;

            var activeSpawn = spawnPoint == null ? transform : spawnPoint;
            var player = FindScenePlayer();
            if (player == null)
            {
                player = InstantiatePlayer(prefabToSpawn);
                player.name = prefabToSpawn.name;
            }

            player.transform.SetPositionAndRotation(activeSpawn.position, activeSpawn.rotation);
            ResetPlayerVelocity(player);
            SnapCameraToPlayer(player.transform);
        }

        private bool IsSceneContextValid()
        {
            if (!isActiveAndEnabled)
                return false;

            if (Application.isPlaying)
                return true;

            var scene = gameObject.scene;
            return scene.IsValid() && !string.IsNullOrEmpty(scene.path);
        }

        private GameObject FindScenePlayer()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                return null;

            if (!Application.isPlaying && player.scene != gameObject.scene)
                return null;

            return player;
        }

        private GameObject InstantiatePlayer(GameObject prefab)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var instance = PrefabUtility.InstantiatePrefab(prefab, gameObject.scene) as GameObject;
                if (instance != null)
                {
                    return instance;
                }
            }
#endif
            return Instantiate(prefab);
        }

        private void ResetPlayerVelocity(GameObject player)
        {
            var body = player.GetComponent<Rigidbody2D>();
            if (body == null)
                return;

#if UNITY_2022_2_OR_NEWER
            body.linearVelocity = Vector2.zero;
#else
            body.velocity = Vector2.zero;
#endif
        }

        private void SnapCameraToPlayer(Transform playerTransform)
        {
            if (!snapCameraOnSpawn)
                return;

            if (!Application.isPlaying)
                return;

            var camera = Camera.main;
            if (camera == null)
                return;

            var follow = camera.GetComponent<Cainos.PixelArtTopDown_Basic.CameraFollow>();
            if (follow != null)
            {
                follow.SetTarget(playerTransform, true);
            }
        }

        private GameObject ResolvePlayerPrefab()
        {
            if (playerPrefab != null)
                return playerPrefab;

            if (cachedPlayerPrefab == null)
            {
                var resourcePath = string.IsNullOrWhiteSpace(playerResourcePath)
                    ? DefaultPlayerResourcePath
                    : playerResourcePath;
                cachedPlayerPrefab = Resources.Load<GameObject>(resourcePath);
                if (cachedPlayerPrefab == null)
                {
                    Debug.LogError(
                        $"{nameof(ScenePlayerSpawner)} on {name} could not load player prefab at Resources/{resourcePath}.",
                        this);
                }
            }

            return cachedPlayerPrefab;
        }
    }
}
