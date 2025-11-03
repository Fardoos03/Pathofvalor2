using UnityEngine;

namespace PathOfValor
{
    public class ScenePlayerSpawner : MonoBehaviour
    {
        private const string DefaultPlayerResourcePath = "Prefabs/player_main";

        private static GameObject cachedPlayerPrefab;

        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private bool snapCameraOnSpawn = true;
        [SerializeField] private string playerResourcePath = DefaultPlayerResourcePath;

        private void Awake()
        {
            var prefabToSpawn = ResolvePlayerPrefab();
            if (prefabToSpawn == null)
                return;

            var activeSpawn = spawnPoint == null ? transform : spawnPoint;
            var player = GameObject.FindGameObjectWithTag("Player");

            if (player == null)
            {
                player = Instantiate(prefabToSpawn, activeSpawn.position, activeSpawn.rotation);
                player.name = prefabToSpawn.name;
            }
            else
            {
                player.transform.SetPositionAndRotation(activeSpawn.position, activeSpawn.rotation);
            }

            var body = player.GetComponent<Rigidbody2D>();
            if (body != null)
            {
#if UNITY_2022_2_OR_NEWER
                body.linearVelocity = Vector2.zero;
#else
                body.velocity = Vector2.zero;
#endif
            }

            if (!snapCameraOnSpawn) return;

            var camera = Camera.main;
            if (camera == null) return;

            var follow = camera.GetComponent<Cainos.PixelArtTopDown_Basic.CameraFollow>();
            if (follow != null)
            {
                follow.SetTarget(player.transform, true);
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
