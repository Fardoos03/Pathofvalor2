using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// ReSharper disable Unity.InefficientPropertyAccess
// ReSharper disable Unity.PerformanceCriticalCodeInvocation

namespace PathOfValor.RikoIntegration
{
    /// <summary>
    /// Bootstraps the PathOfValor scenes with the systems shipped with Riko – The Adventurer.
    /// Instantiates the required singletons, wires up UI references, and ensures the player prefab is active.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class RikoBootstrapper : MonoBehaviour
    {
        private const string GameManagerPrefabPath = "Prefabs/GameManager";
        private const string AudioManagerPrefabPath = "Prefabs/AudioManager";
        private const string DataControllerPrefabPath = "Prefabs/Data/Datacontroller";
        private const string HudPrefabPath = "Prefabs/UI/Canvas_Hud";
        private const string PlayerPrefabPath = "Prefabs/player_main";
        private const string FloatingTextPrefabPath = "Prefabs/Others/FloatingText";
        private const string GameOverClipPath = "Audio/Characters/Player/382310__myfox14__game-over-arcade";

        private static bool _isSubscribed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (_isSubscribed)
            {
                return;
            }

            SceneManager.sceneLoaded += HandleSceneLoaded;
            _isSubscribed = true;

            HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            try
            {
                BootstrapScene();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RikoBootstrapper] Failed to bootstrap scene '{scene.name}': {ex}");
            }
        }

        private static void BootstrapScene()
        {
            var dataController = EnsureSingleton<DataController>(DataControllerPrefabPath, true);
            var gameManager = EnsureSingleton<GameManager>(GameManagerPrefabPath, true);
            EnsureSingleton<AudioController>(AudioManagerPrefabPath, true);
            var hudRoot = EnsureHud(out var floatingTextManager);
            var player = EnsurePlayer();

            if (gameManager == null || player == null)
            {
                Debug.LogWarning("[RikoBootstrapper] Skipping binding – missing GameManager or Player.");
                return;
            }

            BindHud(gameManager, hudRoot, floatingTextManager);
            BindPlayer(gameManager, player);
            BindData(gameManager, dataController);
            BindSceneSystems(gameManager);
        }

        private static T EnsureSingleton<T>(string resourcePath, bool dontDestroyOnLoad) where T : MonoBehaviour
        {
            var existingInstances = FindObjectsOfType<T>();
            if (existingInstances != null && existingInstances.Length > 0)
            {
                var primary = existingInstances[0];
                if (existingInstances.Length > 1)
                {
                    for (var i = 1; i < existingInstances.Length; i++)
                    {
                        var duplicate = existingInstances[i];
                        if (duplicate != null)
                        {
                            Destroy(duplicate.gameObject);
                        }
                    }
                }

                if (dontDestroyOnLoad)
                {
                    DontDestroyOnLoad(primary.gameObject);
                }

                return primary;
            }

            var prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
            {
                Debug.LogError($"[RikoBootstrapper] Unable to find prefab at Resources/{resourcePath} for {typeof(T).Name}.");
                return null;
            }

            var instance = Instantiate(prefab);
            var component = instance.GetComponent<T>();
            if (component == null)
            {
                Debug.LogError($"[RikoBootstrapper] Prefab '{prefab.name}' does not contain component of type {typeof(T).Name}.");
                Destroy(instance);
                return null;
            }

            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(instance);
            }

            return component;
        }

        private static GameObject EnsureHud(out FloatingTextManager floatingTextManager)
        {
            floatingTextManager = FindObjectOfType<FloatingTextManager>();
            if (floatingTextManager != null)
            {
                var hudRoot = floatingTextManager.transform.root.gameObject;
                DontDestroyOnLoad(hudRoot);
                return hudRoot;
            }

            var prefab = Resources.Load<GameObject>(HudPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[RikoBootstrapper] Unable to locate HUD prefab at Resources/{HudPrefabPath}.");
                floatingTextManager = null;
                return null;
            }

            var instance = Instantiate(prefab);
            DontDestroyOnLoad(instance);

            floatingTextManager = instance.GetComponentInChildren<FloatingTextManager>(true);
            if (floatingTextManager == null)
            {
                Debug.LogWarning("[RikoBootstrapper] Canvas_Hud is missing a FloatingTextManager component.");
            }
            else if (floatingTextManager.textPrefab == null)
            {
                var floatingTextPrefab = Resources.Load<GameObject>(FloatingTextPrefabPath);
                if (floatingTextPrefab != null)
                {
                    floatingTextManager.textPrefab = floatingTextPrefab;
                }
            }

            return instance;
        }

        private static Player EnsurePlayer()
        {
            var players = FindObjectsOfType<Player>();
            Player primary = null;

            if (players != null && players.Length > 0)
            {
                primary = players[0];
                for (var i = 1; i < players.Length; i++)
                {
                    var duplicate = players[i];
                    if (duplicate != null)
                    {
                        Destroy(duplicate.gameObject);
                    }
                }

                var gunSprite = primary != null
                    ? primary.GetComponentInChildren<GunSpriteChanger>(true)
                    : null;

                if (gunSprite == null)
                {
                    Debug.LogWarning("[RikoBootstrapper] Existing Player is missing GunSpriteChanger and will be replaced with player_main prefab.");
                    if (primary != null)
                    {
                        Destroy(primary.gameObject);
                    }

                    primary = null;
                }
            }

            if (primary != null)
            {
                DontDestroyOnLoad(primary.gameObject);
                primary.gameObject.name = primary.gameObject.name.Replace("(Clone)", string.Empty);
                primary.gameObject.tag = "Player";
                return primary;
            }

            Player player;
            var prefab = Resources.Load<GameObject>(PlayerPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[RikoBootstrapper] Unable to locate player prefab at Resources/{PlayerPrefabPath}.");
                return null;
            }

            var instance = Instantiate(prefab);
            DontDestroyOnLoad(instance);

            player = instance.GetComponent<Player>();
            if (player == null)
            {
                Debug.LogError("[RikoBootstrapper] player_main prefab does not include a Player component.");
                Destroy(instance);
                return null;
            }

            instance.name = prefab.name;
            instance.tag = "Player";
            return player;
        }

        private static void BindHud(GameManager gameManager, GameObject hudRoot, FloatingTextManager floatingTextManager)
        {
            if (hudRoot == null)
            {
                Debug.LogWarning("[RikoBootstrapper] HUD root is missing; GameManager UI references will remain null.");
                return;
            }

            gameManager.healthBar = FindComponent<RectTransform>(hudRoot, "currentHealth");
            gameManager.deathMenuAnimator = FindComponent<Animator>(hudRoot, "DeathMenu");
            gameManager.pauseMenuAnimator = FindComponent<Animator>(hudRoot, "PanelPause");
            gameManager.toastMessageAnimator = FindComponent<Animator>(hudRoot, "panel_ToastMessage");
            gameManager.textToastMessage = FindComponent<Text>(hudRoot, "txt_ToastMessage");
            gameManager.enemyKillText = FindComponent<Text>(hudRoot, "txt_enemykill");
            gameManager.switchWepImage = FindComponent<Image>(hudRoot, "btn_SwichWeapon");
            gameManager.loadLevel = FindComponent<SceneLoadingBarController>(hudRoot, "panel_Loading");

            var joystick = FindComponent<Joystick>(hudRoot, "Floating Joystick");
            if (joystick != null)
            {
                gameManager.movementJoystick = joystick;
            }

        }

        private static void BindPlayer(GameManager gameManager, Player player)
        {
            gameManager.player = player;
#if UNITY_EDITOR || UNITY_STANDALONE
            player.isOnPc = true;
#else
            player.isOnPc = !Application.isMobilePlatform;
#endif

            var gunSpriteChanger = player.GetComponentInChildren<GunSpriteChanger>(true) ??
                                   FindObjectOfType<GunSpriteChanger>();

            if (gameManager.movementJoystick != null)
            {
                player.movementJoystick = gameManager.movementJoystick;
            }

            var weaponContainer = gameManager.transform.Find("weaponContainer");
            if (weaponContainer != null)
            {
                player.weaponContainer = weaponContainer.gameObject;
            }

            var weapons = weaponContainer != null
                ? weaponContainer.GetComponentsInChildren<Weapon>(true)
                : Array.Empty<Weapon>();

            foreach (var weapon in weapons)
            {
                if (weapon == null)
                {
                    continue;
                }

                if (weapon.gunSpriteChanger == null)
                {
                    weapon.gunSpriteChanger = gunSpriteChanger;
                }

                if (weapon.bulletSpawnPoint == null && gunSpriteChanger != null)
                {
                    weapon.bulletSpawnPoint = gunSpriteChanger.bulletSpawnPoint;
                }
            }

            var defaultWeapon = weapons.OrderBy(w => w.weaponID).FirstOrDefault();
            if (defaultWeapon != null)
            {
                defaultWeapon.enabled = true;
                player.weapon = defaultWeapon;
                gameManager.weapon = defaultWeapon;
                if (gunSpriteChanger != null)
                {
                    defaultWeapon.ChangeSprites();
                }
                else
                {
                    Debug.LogWarning("[RikoBootstrapper] Player is missing GunSpriteChanger; weapon visuals will not update.");
                }
            }

            if (player.gameOverClip == null)
            {
                var clip = Resources.Load<AudioClip>(GameOverClipPath);
                if (clip != null)
                {
                    player.gameOverClip = clip;
                }
            }
        }

        private static void BindData(GameManager gameManager, DataController dataController)
        {
            if (dataController == null)
            {
                return;
            }

            gameManager.data = dataController.data;
        }

        private static void BindSceneSystems(GameManager gameManager)
        {
            if (gameManager.enemyActivator == null)
            {
                gameManager.enemyActivator = FindObjectOfType<EnemyActivator>();
                if (gameManager.enemyActivator == null)
                {
                    Debug.LogWarning("[RikoBootstrapper] No EnemyActivator found in scene; enemy waves will not start automatically.");
                }
            }

            if (gameManager.scenePortal == null)
            {
                gameManager.scenePortal = FindObjectOfType<ScenePortal>();
                if (gameManager.scenePortal == null)
                {
                    Debug.LogWarning("[RikoBootstrapper] No ScenePortal found in scene; exits will remain locked.");
                }
            }

            if (gameManager.enemyBatchHandler == null)
            {
                gameManager.enemyBatchHandler = FindObjectOfType<EnemyBatchHandler>();
            }

            var enemies = FindObjectsOfType<Enemy>();
            if (enemies != null && enemies.Length > 0)
            {
                gameManager.totalEnemies = enemies.Length;
            }
        }

        private static T FindComponent<T>(GameObject root, string name) where T : Component
        {
            return root == null
                ? null
                : root.GetComponentsInChildren<T>(true).FirstOrDefault(component => component.name == name);
        }
    }
}
