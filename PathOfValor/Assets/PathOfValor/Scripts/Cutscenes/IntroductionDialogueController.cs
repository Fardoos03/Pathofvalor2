using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Cainos.PixelArtTopDown_Basic;

namespace PathOfValor.Cutscenes
{
    /// <summary>
    /// Handles the proximity prompt, dialogue flow, and simple villain
    /// entrance/kidnapping sequence that opens the campaign.
    /// Attach this to Cassius' GameObject in the Introduction scene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class IntroductionDialogueController : MonoBehaviour
    {
        private enum LineEvent
        {
            None,
            Rumble,
            PortalOpen,
            TrapCast,
            PortalClose,
            FadeOut
        }

        private readonly struct DialogueLine
        {
            public DialogueLine(string speaker, string text, LineEvent lineEvent = LineEvent.None)
            {
                Speaker = speaker;
                Text = text;
                Event = lineEvent;
            }

            public string Speaker { get; }
            public string Text { get; }
            public LineEvent Event { get; }
        }

        private static readonly DialogueLine[] Script =
        {
            new DialogueLine("Cassius", "Dad, when I grow up, I want to swing a sword like you!"),
            new DialogueLine("Eldric", "Hah! A sword's only as good as the heart that wields it. You've got time to learn, little one."),
            new DialogueLine("Cassius", "But you always say strength comes from the heart, not muscles."),
            new DialogueLine("Eldric", "That's right. Remember, courage isn't about fighting -- it's about protecting what you love."),
            new DialogueLine(string.Empty, "(Sudden rumble; the sky darkens, thunder cracks. Purple energy swirls in the distance.)", LineEvent.Rumble),
            new DialogueLine("Eldric", "Cassius... stay behind me."),
            new DialogueLine(string.Empty, "(A dark portal tears open. Malrik steps through, surrounded by black flame.)", LineEvent.PortalOpen),
            new DialogueLine("Malrik (mocking tone)", "Touching words for a farmer playing hero. Tell me, Eldric... how strong is your courage when it's ripped away?"),
            new DialogueLine("Eldric", "Malrik...! You have no place here!"),
            new DialogueLine("Malrik", "Oh, but I do. I've come for what gives you strength."),
            new DialogueLine(string.Empty, "(Malrik raises his staff. A burst of energy traps Cassius in a shadow orb.)", LineEvent.TrapCast),
            new DialogueLine("Cassius", "Dad! Help!"),
            new DialogueLine("Eldric", "CASSIUS!!!"),
            new DialogueLine("Malrik (grinning)", "You'll never reach me in your current state. Come find me -- if you survive."),
            new DialogueLine(string.Empty, "(Malrik disappears with Cassius through a collapsing portal. Silence follows. Rain begins to fall.)", LineEvent.PortalClose),
            new DialogueLine("Eldric", "No... Cassius... what have I done?"),
            new DialogueLine(string.Empty, "(Screen fades to black -- soft music begins, symbolizing loss and resolve.)", LineEvent.FadeOut)
        };

        [Header("Interaction")]
        [SerializeField] private float interactRadius = 1.75f;
        [SerializeField] private KeyCode talkKey = KeyCode.T;
        [SerializeField] private string playerTag = "Player";

        [Header("Villain")]
        [SerializeField] private Vector2 villainSpawnOffset = new Vector2(-3f, 3.25f);
        [SerializeField] private Vector2 villainExitOffset = new Vector2(6f, 4f);
        [SerializeField] private float villainTravelSeconds = 2f;
        [SerializeField] private float kidnappingDelaySeconds = 0.9f;
        [SerializeField] private float resumeControlDelay = 1.5f;

        private Transform player;
        private TopDownCharacterController playerController;
        private Rigidbody2D playerBody;
        private SimpleDialogueUI ui;
        private SpriteRenderer[] cassiusSprites;
        private SpriteRenderer primaryCassiusRenderer;
        private Collider2D cassiusCollider;
        private GameObject villainInstance;
        private bool conversationStarted;
        private bool conversationFinished;
        private int lineIndex = -1;
        private ParticleSystem rainSystem;
        private bool rumbleTriggered;
        private bool villainSummoned;
        private bool cassiusCaptured;
        private bool villainDismissed;
        private bool completionRoutineStarted;
        private bool playerFrozen;

        private void Awake()
        {
            cassiusSprites = GetComponentsInChildren<SpriteRenderer>(true);
            primaryCassiusRenderer = GetComponent<SpriteRenderer>();
            if (primaryCassiusRenderer == null && cassiusSprites != null)
            {
                foreach (var sprite in cassiusSprites)
                {
                    if (sprite != null && sprite.transform == transform)
                    {
                        primaryCassiusRenderer = sprite;
                        break;
                    }
                }
            }
            cassiusCollider = GetComponent<Collider2D>();
            ui = new SimpleDialogueUI(talkKey);
        }

        private void OnDestroy()
        {
            ui?.Dispose();

            if (villainInstance != null)
            {
                Destroy(villainInstance);
                villainInstance = null;
            }

            if (rainSystem != null)
            {
                Destroy(rainSystem.gameObject);
                rainSystem = null;
            }

            if (conversationStarted && playerController != null)
            {
                playerController.enabled = true;
            }
        }

        private void Update()
        {
            EnsurePlayerReference();

            if (playerController != null && !conversationStarted && playerFrozen)
            {
                FreezePlayer(false);
            }

            if (conversationFinished || player == null)
            {
                ui.SetPromptVisible(false);
                return;
            }

            if (!conversationStarted)
            {
                bool playerClose = Vector2.Distance(player.position, transform.position) <= interactRadius;
                ui.SetPromptVisible(playerClose);

                if (!playerClose)
                {
                    return;
                }
            }

            if (Input.GetKeyDown(talkKey))
            {
                if (!conversationStarted)
                {
                    BeginConversation();
                }
                else
                {
                    AdvanceDialogue();
                }
            }
        }

        private void EnsurePlayerReference()
        {
            if (player != null)
            {
                return;
            }

            var found = GameObject.FindGameObjectWithTag(playerTag);
            if (found == null)
            {
                return;
            }

            player = found.transform;
            playerController = found.GetComponent<TopDownCharacterController>();
            playerBody = found.GetComponent<Rigidbody2D>();
        }

        private void BeginConversation()
        {
            conversationStarted = true;
            ui.SetPromptVisible(false);
            FreezePlayer(true);
            AdvanceDialogue();
        }

        private void AdvanceDialogue()
        {
            var nextIndex = lineIndex + 1;
            if (nextIndex >= Script.Length)
            {
                return;
            }

            lineIndex = nextIndex;
            var line = Script[lineIndex];
            bool isLastLine = lineIndex == Script.Length - 1;
            ui.ShowLine(line.Speaker, line.Text, isLastLine);
            HandleLineEvent(line.Event);

            if (isLastLine)
            {
                conversationFinished = true;
                StartCompletionRoutine();
            }
        }

        private void HandleLineEvent(LineEvent lineEvent)
        {
            switch (lineEvent)
            {
                case LineEvent.Rumble:
                    if (!rumbleTriggered)
                    {
                        rumbleTriggered = true;
                        StartCoroutine(RumbleRoutine());
                    }

                    break;
                case LineEvent.PortalOpen:
                    if (!villainSummoned)
                    {
                        villainSummoned = true;
                        StartCoroutine(SummonVillainRoutine());
                    }

                    break;
                case LineEvent.TrapCast:
                    if (!cassiusCaptured)
                    {
                        cassiusCaptured = true;
                        StartCoroutine(CaptureCassiusRoutine());
                    }

                    break;
                case LineEvent.PortalClose:
                    if (!villainDismissed)
                    {
                        villainDismissed = true;
                        StartCoroutine(DismissVillainRoutine());
                    }

                    break;
                case LineEvent.FadeOut:
                    StartCompletionRoutine();
                    break;
            }
        }

        private void StartCompletionRoutine()
        {
            if (completionRoutineStarted)
            {
                return;
            }

            completionRoutineStarted = true;
            StartCoroutine(CompleteSequenceRoutine());
        }

        private IEnumerator RumbleRoutine()
        {
            var timer = 0f;
            const float duration = 1.2f;
            var originalColor = Camera.main != null ? Camera.main.backgroundColor : Color.black;
            var stormColor = new Color(0.2f, 0.22f, 0.35f);

            while (timer < duration && Camera.main != null)
            {
                timer += Time.deltaTime;
                var lerp = Mathf.Clamp01(timer / duration);
                Camera.main.backgroundColor = Color.Lerp(originalColor, stormColor, lerp);
                yield return null;
            }
        }

        private IEnumerator SummonVillainRoutine()
        {
            villainInstance = CreateVillain();
            var spawnPosition = (Vector2)transform.position + villainSpawnOffset;
            var startPosition = new Vector3(spawnPosition.x, spawnPosition.y, transform.position.z);
            villainInstance.transform.position = startPosition;

            var targetPosition = transform.position + new Vector3(0f, 0.5f, 0f);
            var timer = 0f;
            var duration = Mathf.Max(0.25f, villainTravelSeconds);

            while (timer < duration)
            {
                timer += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, timer / duration);
                villainInstance.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                yield return null;
            }

            villainInstance.transform.position = targetPosition;
        }

        private IEnumerator CaptureCassiusRoutine()
        {
            var elapsed = 0f;
            var duration = Mathf.Max(0.1f, kidnappingDelaySeconds);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var pulse = 0.5f + Mathf.PingPong(Time.time * 2f, 0.5f);
                var color = Color.Lerp(Color.white, new Color(0.4f, 0.1f, 0.8f), pulse);
                foreach (var sprite in cassiusSprites)
                {
                    if (sprite != null)
                    {
                        sprite.color = color;
                    }
                }

                yield return null;
            }

            foreach (var sprite in cassiusSprites)
            {
                if (sprite != null)
                {
                    sprite.enabled = false;
                }
            }

            if (cassiusCollider != null)
            {
                cassiusCollider.enabled = false;
            }
        }

        private IEnumerator DismissVillainRoutine()
        {
            if (villainInstance == null)
            {
                yield break;
            }

            var start = villainInstance.transform.position;
            var exitOffset = (Vector2)transform.position + villainExitOffset;
            var exit = new Vector3(exitOffset.x, exitOffset.y, start.z);
            var timer = 0f;
            var duration = Mathf.Max(0.25f, villainTravelSeconds * 0.75f);

            while (timer < duration)
            {
                timer += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, timer / duration);
                villainInstance.transform.position = Vector3.Lerp(start, exit, t);
                yield return null;
            }

            Destroy(villainInstance);
            villainInstance = null;
            StartRain();
        }

        private IEnumerator CompleteSequenceRoutine()
        {
            yield return new WaitForSeconds(resumeControlDelay);
            ui.HideDialogue();
            FreezePlayer(false);
        }

        private GameObject CreateVillain()
        {
            const string resourcePath = "Prefabs/Enemies/Boses/Boss_Big_Demon";
            var prefab = Resources.Load<GameObject>(resourcePath);
            GameObject instance;

            if (prefab != null)
            {
                instance = Instantiate(prefab);
            }
            else
            {
                instance = CreateFallbackVillain();
            }

            instance.name = "Malverik";
            NeutralizeBehaviours(instance);
            return instance;
        }

        private void StartRain()
        {
            if (rainSystem != null)
            {
                return;
            }

            var rainRoot = new GameObject("CutsceneRain");
            if (Camera.main != null)
            {
                rainRoot.transform.SetParent(Camera.main.transform, false);
                rainRoot.transform.localPosition = new Vector3(0f, 6f, 0f);
            }
            else
            {
                rainRoot.transform.position = transform.position + new Vector3(0f, 8f, 0f);
            }

            rainSystem = rainRoot.AddComponent<ParticleSystem>();
            var main = rainSystem.main;
            main.startLifetime = 1.4f;
            main.startSpeed = 0f;
            main.startSize = 0.05f;
            main.maxParticles = 600;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = rainSystem.emission;
            emission.rateOverTime = 260f;

            var shape = rainSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(30f, 1f, 1f);

            var velocity = rainSystem.velocityOverLifetime;
            velocity.enabled = true;
            velocity.y = -14f;

            var renderer = rainSystem.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.sortingLayerID = primaryCassiusRenderer != null ? primaryCassiusRenderer.sortingLayerID : 0;
            renderer.sortingOrder = primaryCassiusRenderer != null ? primaryCassiusRenderer.sortingOrder - 1 : 0;

            rainSystem.Play();
        }

        private GameObject CreateFallbackVillain()
        {
            var fallback = new GameObject("Malverik");
            var spriteRenderer = fallback.AddComponent<SpriteRenderer>();
            if (primaryCassiusRenderer != null)
            {
                spriteRenderer.sprite = primaryCassiusRenderer.sprite;
                spriteRenderer.sharedMaterial = primaryCassiusRenderer.sharedMaterial;
                spriteRenderer.sortingLayerID = primaryCassiusRenderer.sortingLayerID;
                spriteRenderer.sortingOrder = primaryCassiusRenderer.sortingOrder + 1;
            }

            spriteRenderer.color = new Color(0.4f, 0.15f, 0.6f, 1f);
            return fallback;
        }

        private static void NeutralizeBehaviours(GameObject root)
        {
            var behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var behaviour in behaviours)
            {
                behaviour.enabled = false;
            }

            var rigidbodies = root.GetComponentsInChildren<Rigidbody2D>(true);
            foreach (var body in rigidbodies)
            {
                body.simulated = false;
#if UNITY_2022_2_OR_NEWER
                body.linearVelocity = Vector2.zero;
#else
                body.velocity = Vector2.zero;
#endif
            }

            var rigidbodies3D = root.GetComponentsInChildren<Rigidbody>(true);
            foreach (var body in rigidbodies3D)
            {
                body.isKinematic = true;
                body.detectCollisions = false;
                body.linearVelocity = Vector3.zero;
            }

            var colliders2D = root.GetComponentsInChildren<Collider2D>(true);
            foreach (var collider in colliders2D)
            {
                collider.enabled = false;
            }

            var colliders3D = root.GetComponentsInChildren<Collider>(true);
            foreach (var collider in colliders3D)
            {
                collider.enabled = false;
            }
        }

        private void FreezePlayer(bool freeze)
        {
            playerFrozen = freeze;

            if (playerController != null)
            {
                playerController.enabled = !freeze;
            }

            if (playerBody != null)
            {
#if UNITY_2022_2_OR_NEWER
                playerBody.linearVelocity = Vector2.zero;
#else
                playerBody.velocity = Vector2.zero;
#endif
            }
        }

        private sealed class SimpleDialogueUI : System.IDisposable
        {
            private readonly GameObject canvasRoot;
            private readonly GameObject promptRoot;
            private readonly GameObject dialogueRoot;
            private readonly Text promptText;
            private readonly Text speakerText;
            private readonly Text bodyText;
            private readonly Text continueHint;

            public SimpleDialogueUI(KeyCode talkKey)
            {
                canvasRoot = new GameObject("IntroductionDialogueUI");
                var canvas = canvasRoot.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = short.MaxValue;
                var scaler = canvasRoot.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                canvasRoot.AddComponent<GraphicRaycaster>();

                var font = LoadDefaultFont();

                promptRoot = BuildPanel("PromptPanel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(420f, 46f), new Color(0f, 0f, 0f, 0.55f));
                promptRoot.transform.SetParent(canvasRoot.transform, false);
                promptText = BuildText("PromptLabel", promptRoot.transform, font, 26, TextAnchor.MiddleCenter);
                promptText.text = $"Press {talkKey.ToString().ToUpperInvariant()} to talk";
                promptRoot.SetActive(false);

                dialogueRoot = BuildPanel("DialoguePanel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(900f, 220f), new Color(0f, 0f, 0f, 0.7f));
                dialogueRoot.transform.SetParent(canvasRoot.transform, false);

                speakerText = BuildText("SpeakerLabel", dialogueRoot.transform, font, 30, TextAnchor.UpperLeft);
                var speakerRect = speakerText.rectTransform;
                speakerRect.anchorMin = new Vector2(0f, 1f);
                speakerRect.anchorMax = new Vector2(0f, 1f);
                speakerRect.pivot = new Vector2(0f, 1f);
                speakerRect.anchoredPosition = new Vector2(30f, -25f);
                speakerRect.sizeDelta = new Vector2(840f, 40f);

                bodyText = BuildText("BodyLabel", dialogueRoot.transform, font, 26, TextAnchor.UpperLeft);
                var bodyRect = bodyText.rectTransform;
                bodyRect.anchorMin = new Vector2(0f, 0f);
                bodyRect.anchorMax = new Vector2(1f, 1f);
                bodyRect.pivot = new Vector2(0f, 1f);
                bodyRect.anchoredPosition = new Vector2(30f, -75f);
                bodyRect.sizeDelta = new Vector2(-60f, -110f);
                bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
                bodyText.verticalOverflow = VerticalWrapMode.Overflow;

                continueHint = BuildText("ContinueHint", dialogueRoot.transform, font, 22, TextAnchor.LowerRight);
                var hintRect = continueHint.rectTransform;
                hintRect.anchorMin = new Vector2(1f, 0f);
                hintRect.anchorMax = new Vector2(1f, 0f);
                hintRect.pivot = new Vector2(1f, 0f);
                hintRect.anchoredPosition = new Vector2(-20f, 20f);
                hintRect.sizeDelta = new Vector2(260f, 30f);
                continueHint.text = $"[{talkKey.ToString().ToUpperInvariant()}] Continue";

                dialogueRoot.SetActive(false);
            }

            public void SetPromptVisible(bool visible)
            {
                if (promptRoot != null)
                {
                    promptRoot.SetActive(visible);
                }
            }

            public void ShowLine(string speaker, string text, bool isLastLine)
            {
                if (dialogueRoot == null)
                {
                    return;
                }

                dialogueRoot.SetActive(true);
                if (string.IsNullOrWhiteSpace(speaker))
                {
                    speakerText.gameObject.SetActive(false);
                }
                else
                {
                    speakerText.gameObject.SetActive(true);
                    speakerText.text = speaker;
                }

                bodyText.text = text;
                continueHint.gameObject.SetActive(!isLastLine);
            }

            public void HideDialogue()
            {
                if (dialogueRoot != null)
                {
                    dialogueRoot.SetActive(false);
                }
            }

            public void Dispose()
            {
                if (canvasRoot != null)
                {
                    Object.Destroy(canvasRoot);
                }
            }

            private static GameObject BuildPanel(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, Color color)
            {
                var panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                var rect = panel.GetComponent<RectTransform>();
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMax;
                rect.pivot = pivot;
                rect.anchoredPosition = anchoredPosition;
                rect.sizeDelta = size;
                var image = panel.GetComponent<Image>();
                image.color = color;
                image.raycastTarget = false;
                return panel;
            }

            private static Text BuildText(string name, Transform parent, Font font, int fontSize, TextAnchor anchor)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                go.transform.SetParent(parent, false);
                var text = go.GetComponent<Text>();
                text.font = font;
                text.fontSize = fontSize;
                text.alignment = anchor;
                text.color = Color.white;
                text.raycastTarget = false;
                return text;
            }

            private static Font LoadDefaultFont()
            {
                if (TryLoadBuiltInFont("LegacyRuntime.ttf", out var font))
                {
                    return font;
                }

                if (TryLoadBuiltInFont("Arial.ttf", out font))
                {
                    return font;
                }

                Debug.LogWarning("IntroductionDialogueController: Unable to find built-in LegacyRuntime/Arial font. Falling back to dynamic Arial.");
                return Font.CreateDynamicFontFromOSFont("Arial", 28);
            }

            private static bool TryLoadBuiltInFont(string resourceName, out Font font)
            {
                try
                {
                    font = Resources.GetBuiltinResource<Font>(resourceName);
                    return font != null;
                }
                catch (System.ArgumentException)
                {
                    font = null;
                    return false;
                }
            }
        }
    }
}
