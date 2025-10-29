using UnityEngine;

namespace PathOfValor
{
    public class ScenePlayerSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private bool snapCameraOnSpawn = true;

        private void Awake()
        {
            if (playerPrefab == null)
            {
                Debug.LogWarning($"{nameof(ScenePlayerSpawner)} on {name} is missing a player prefab reference.", this);
                return;
            }

            var activeSpawn = spawnPoint == null ? transform : spawnPoint;
            var player = GameObject.FindGameObjectWithTag("Player");

            if (player == null)
            {
                player = Instantiate(playerPrefab, activeSpawn.position, activeSpawn.rotation);
                player.name = playerPrefab.name;
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
    }
}
