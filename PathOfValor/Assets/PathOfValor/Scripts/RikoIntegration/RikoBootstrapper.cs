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
        private const string FallbackEnemyActivatorName = "EnemyActivator (Bootstrap)";
        private const string FallbackScenePortalName = "ScenePortal (Bootstrap)";

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
            var existingInstances = FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
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
            floatingTextManager = FindFirstObjectByType<FloatingTextManager>();
            if (floatingTextManager != null)
            {
                var hudRoot = floatingTextManager.transform.root.gameObject;
                EnsureFloatingTextPrefab(floatingTextManager);
                DontDestroyOnLoad(hudRoot);
                return hudRoot;
            }

            var existingHud = GameObject.Find("Canvas_Hud");
            if (existingHud != null)
            {
                floatingTextManager = EnsureFloatingTextManager(existingHud);
                DontDestroyOnLoad(existingHud);
                return existingHud;
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

            floatingTextManager = EnsureFloatingTextManager(instance);

            return instance;
        }

        private static FloatingTextManager EnsureFloatingTextManager(GameObject hudRoot)
        {
            if (hudRoot == null)
            {
                return null;
            }

            var manager = hudRoot.GetComponentInChildren<FloatingTextManager>(true);
            if (manager == null)
            {
                manager = hudRoot.AddComponent<FloatingTextManager>();
                Debug.LogWarning("[RikoBootstrapper] Canvas_Hud was missing a FloatingTextManager component; one was added at runtime.");
            }

            EnsureFloatingTextPrefab(manager);
            return manager;
        }

        private static void EnsureFloatingTextPrefab(FloatingTextManager floatingTextManager)
        {
            if (floatingTextManager == null || floatingTextManager.textPrefab != null)
            {
                return;
            }

            var floatingTextPrefab = Resources.Load<GameObject>(FloatingTextPrefabPath);
            if (floatingTextPrefab != null)
            {
                floatingTextManager.textPrefab = floatingTextPrefab;
            }
            else
            {
                Debug.LogWarning($"[RikoBootstrapper] Unable to assign FloatingText prefab from Resources/{FloatingTextPrefabPath}.");
            }
        }

        private static Player EnsurePlayer()
        {
            var players = FindObjectsByType<Player>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
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

                if (gunSprite == null && primary != null)
                {
                    if (TryAttachGunSpriteChanger(primary))
                    {
                        gunSprite = primary.GetComponentInChildren<GunSpriteChanger>(true);
                    }
                }

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

        private static bool TryAttachGunSpriteChanger(Player player)
        {
            if (player == null)
            {
                return false;
            }

            var existing = player.GetComponent<GunSpriteChanger>();
            if (existing != null)
            {
                return true;
            }

            var prefab = Resources.Load<GameObject>(PlayerPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("[RikoBootstrapper] Unable to upgrade Player – player_main prefab not found.");
                return false;
            }

            var template = prefab.GetComponent<GunSpriteChanger>();
            if (template == null || template.GunSide == null || template.GunUp == null ||
                template.GunDown == null || template.bulletSpawnPoint == null)
            {
                Debug.LogWarning("[RikoBootstrapper] Unable to upgrade Player – player_main prefab is missing gun sprite references.");
                return false;
            }

            var gunRootTemplate = template.GunSide.transform.parent;
            if (gunRootTemplate == null)
            {
                Debug.LogWarning("[RikoBootstrapper] Unable to upgrade Player – gun sprite container missing on template.");
                return false;
            }

            var gunRootInstance = Instantiate(gunRootTemplate.gameObject, player.transform).transform;
            CopyNamesRecursive(gunRootTemplate, gunRootInstance);

            var bulletSpawnInstance = Instantiate(template.bulletSpawnPoint.gameObject, player.transform).transform;
            bulletSpawnInstance.gameObject.name = template.bulletSpawnPoint.gameObject.name;

            var changer = player.gameObject.AddComponent<GunSpriteChanger>();
            changer.GunSide = MatchSpriteRenderer(gunRootInstance, template.GunSide);
            changer.GunUp = MatchSpriteRenderer(gunRootInstance, template.GunUp);
            changer.GunDown = MatchSpriteRenderer(gunRootInstance, template.GunDown);
            changer.GunDiagUp = MatchSpriteRenderer(gunRootInstance, template.GunDiagUp);
            changer.GunDiagDown = MatchSpriteRenderer(gunRootInstance, template.GunDiagDown);
            changer.bulletSpawnPoint = bulletSpawnInstance;

            if (changer.GunSide == null || changer.GunUp == null || changer.GunDown == null)
            {
                Destroy(gunRootInstance.gameObject);
                Destroy(bulletSpawnInstance.gameObject);
                Destroy(changer);
                Debug.LogWarning("[RikoBootstrapper] Unable to upgrade Player – failed to map gun sprite renderers.");
                return false;
            }

            return true;
        }

        private static void CopyNamesRecursive(Transform template, Transform instance)
        {
            if (template == null || instance == null)
            {
                return;
            }

            instance.name = template.name;
            for (var i = 0; i < instance.childCount; i++)
            {
                var templateChild = i < template.childCount ? template.GetChild(i) : null;
                var instanceChild = instance.GetChild(i);
                CopyNamesRecursive(templateChild, instanceChild);
            }
        }

        private static SpriteRenderer MatchSpriteRenderer(Transform root, SpriteRenderer templateRenderer)
        {
            if (root == null || templateRenderer == null)
            {
                return null;
            }

            var renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer != null && renderer.name == templateRenderer.name)
                {
                    return renderer;
                }
            }

            return null;
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
                                   FindFirstObjectByType<GunSpriteChanger>();

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
                gameManager.enemyActivator = FindFirstObjectByType<EnemyActivator>();
            }

            if (gameManager.enemyActivator == null)
            {
                gameManager.enemyActivator = EnsureFallbackEnemyActivator();
                if (gameManager.enemyActivator != null)
                {
                    Debug.Log("[RikoBootstrapper] No EnemyActivator found in scene; using fallback activator.");
                }
                else
                {
                    Debug.LogWarning("[RikoBootstrapper] No EnemyActivator found in scene; enemy waves will not start automatically.");
                }
            }

            if (gameManager.scenePortal == null)
            {
                gameManager.scenePortal = FindFirstObjectByType<ScenePortal>();
            }

            if (gameManager.scenePortal == null)
            {
                gameManager.scenePortal = EnsureFallbackScenePortal();
                if (gameManager.scenePortal != null)
                {
                    Debug.Log("[RikoBootstrapper] No ScenePortal found in scene; using fallback portal stub.");
                }
                else
                {
                    Debug.LogWarning("[RikoBootstrapper] No ScenePortal found in scene; exits will remain locked.");
                }
            }
            else if (gameManager.scenePortal != null && gameManager.scenePortal.name == FallbackScenePortalName)
            {
                UpdateFallbackScenePortal(gameManager.scenePortal);
            }

            if (gameManager.enemyBatchHandler == null)
            {
                gameManager.enemyBatchHandler = FindFirstObjectByType<EnemyBatchHandler>();
            }

            var enemies = FindObjectsByType<Enemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (enemies != null && enemies.Length > 0)
            {
                gameManager.totalEnemies = enemies.Length;
            }
        }

        private static EnemyActivator EnsureFallbackEnemyActivator()
        {
            var existing = GameObject.Find(FallbackEnemyActivatorName);
            if (existing != null)
            {
                var activator = existing.GetComponent<EnemyActivator>();
                if (activator != null)
                {
                    return activator;
                }
            }

            var go = new GameObject(FallbackEnemyActivatorName);
            DontDestroyOnLoad(go);
            var fallback = go.AddComponent<EnemyActivator>();
            fallback.firstEnemyBatch = null;
            fallback.LockedBarriers = Array.Empty<GameObject>();
            return fallback;
        }

        private static ScenePortal EnsureFallbackScenePortal()
        {
            var existing = GameObject.Find(FallbackScenePortalName);
            if (existing != null)
            {
                var portal = existing.GetComponent<ScenePortal>();
                if (portal != null)
                {
                    UpdateFallbackScenePortal(portal);
                    return portal;
                }
            }

            var go = new GameObject(FallbackScenePortalName);
            DontDestroyOnLoad(go);

            var collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;

            var portalComponent = go.AddComponent<ScenePortal>();
            portalComponent.sceneNames = new[] { SceneManager.GetActiveScene().name };

            var filter = portalComponent.filter;
            filter.useTriggers = true;
            portalComponent.filter = filter;

            var visual = new GameObject("DoorVisual");
            visual.transform.SetParent(go.transform, false);
            visual.AddComponent<SpriteRenderer>();

            return portalComponent;
        }

        private static void UpdateFallbackScenePortal(ScenePortal portal)
        {
            if (portal == null)
            {
                return;
            }

            var sceneName = SceneManager.GetActiveScene().name;
            portal.sceneNames = string.IsNullOrEmpty(sceneName)
                ? new[] { "LevelOne" }
                : new[] { sceneName };
        }

        private static T FindComponent<T>(GameObject root, string name) where T : Component
        {
            return root == null
                ? null
                : root.GetComponentsInChildren<T>(true).FirstOrDefault(component => component.name == name);
        }
    }
}
