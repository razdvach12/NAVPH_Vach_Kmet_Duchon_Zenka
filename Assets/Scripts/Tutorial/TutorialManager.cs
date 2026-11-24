using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Interaktívny tutoriál v TutorialScene (kópia Encounter scény).
// Pripraví ľahký súboj, krok po kroku vysvetlí hru (zvýrazní, o čom sa práve hovorí) a na konci vráti hráča do menu.
// Každý krok, celú kapitolu aj celý tutoriál sa dá preskočiť.
// Awake musí bežať PRED TurnManagerom a ShopUIControllerom - pripraví PlayerStats a tréningovú room.
[DefaultExecutionOrder(-1000)]
public class TutorialManager : MonoBehaviour
{
    [Header("Výbava hráča v tutoriáli")]
    [Tooltip("Z tejto postavy sa vezmú zbrane a portrét")]
    [SerializeField] private CharacterData character;
    [SerializeField] private int tutorialStamina = 6;
    [SerializeField] private ConsumableData bomb;
    [SerializeField] private ConsumableData healthPotion;
    [Tooltip("Relikvie, ktoré hráč v tutoriáli má (napr. Shield of Courage, Fast Feather)")]
    [SerializeField] private List<ItemData> relics = new List<ItemData>();

    [Header("Tréningový nepriateľ")]
    [SerializeField] private string enemyName = "Training Golem";
    [SerializeField] private Sprite enemySprite;

    [Header("Vzhľad")]
    [Tooltip("Font textov tutoriálu (prázdne = predvolený TextMeshPro font)")]
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private Color highlightColor = new Color(1f, 0.85f, 0.2f, 1f);

    // --- KROKY TUTORIÁLU ---

    private class Step
    {
        public string chapter;
        public string title;
        public string text;
        public Func<Rect?> target;   // Čo zvýrazniť (obdĺžnik na obrazovke), null = nič
        public Func<bool> isDone;    // null = informačný krok (tlačidlo Next), inak čaká na akciu hráča
        public Action onEnter;
        public Action onSkip;
        public bool needsCombat;     // Po skončení súboja sa krok automaticky preskočí
        public bool playerActs;      // Počas kroku je ovládanie hráča odomknuté
        public bool isFinal;
    }

    private readonly List<Step> steps = new List<Step>();
    private int currentIndex = -1;
    private bool advanceRequested;
    private bool chapterSkipRequested;
    private bool exiting;

    private TurnManager turnManager;
    private ShopUIController shopUI;
    private PlayerStats stats;

    private int playerTurnsStarted;
    private int weaponsUsed;
    private bool combatOver;
    private bool playerDefeated;

    // UI
    private RectTransform canvasRect;
    private RectTransform panel;
    private RectTransform frame;
    private Image[] frameBars;
    private TextMeshProUGUI chapterLabel;
    private TextMeshProUGUI titleLabel;
    private TextMeshProUGUI bodyLabel;
    private Button nextButton;
    private TextMeshProUGUI nextButtonLabel;
    private Button skipStepButton;
    private Button skipChapterButton;
    private Button exitButton;

    private void Awake()
    {
        turnManager = FindAnyObjectByType<TurnManager>();
        if (turnManager == null)
        {
            Debug.LogError("TutorialManager: v scéne chýba TurnManager.");
            enabled = false;
            return;
        }

        turnManager.HandleCombatEndExternally = true;
        turnManager.InputLocked = true;
        turnManager.onPlayerTurnStart.AddListener(() => playerTurnsStarted++);
        turnManager.onVictory.AddListener(() => combatOver = true);
        turnManager.onDefeat.AddListener(() => { combatOver = true; playerDefeated = true; });
        turnManager.WeaponUsed += OnWeaponUsed;

        SetupPlayer();
        SetupTrainingRoom();
    }

    private void OnDestroy()
    {
        if (turnManager != null) turnManager.WeaponUsed -= OnWeaponUsed;
    }

    private void OnWeaponUsed() => weaponsUsed++;

    private IEnumerator Start()
    {
        shopUI = FindAnyObjectByType<ShopUIController>();
        BuildUI();
        BuildSteps();

        // Počkáme, kým sa vygenerujú gridy a začne prvý ťah hráča
        while (playerTurnsStarted == 0 && !combatOver) yield return null;

        yield return RunSteps();
    }

    // --- PRÍPRAVA SÚBOJA ---

    private void SetupPlayer()
    {
        stats = PlayerStats.EnsureInstance();
        if (character != null) stats.InitializeCharacter(character);

        stats.maxStamina = tutorialStamina;
        stats.playerHealthPreset = BuildPreset("TutorialPlayerHp", new[]
        {
            "........",
            "..BBBB..",
            ".BLBBIB.",
            ".BBBBBB.",
            ".NBBBBR.",
            ".BBBBBB.",
            "..BBBB..",
            "........"
        }, (x, y) => 2);

        // Hráč začína trochu zranený, aby mal čo liečiť Health Potionom
        GridPreset hp = stats.playerHealthPreset;
        stats.playerCurrentHp = new int[hp.width * hp.height];
        for (int x = 0; x < hp.width; x++)
        {
            for (int y = 0; y < hp.height; y++)
            {
                CellSetup cell = hp.GetCell(x, y);
                if (cell.element != BlockElement.None) stats.playerCurrentHp[y * hp.width + x] = cell.thickness;
            }
        }
        foreach (Vector2Int damaged in new[] { new Vector2Int(2, 3), new Vector2Int(3, 3), new Vector2Int(4, 5), new Vector2Int(3, 6) })
        {
            stats.playerCurrentHp[damaged.y * hp.width + damaged.x] = 1;
        }

        stats.consumables.Clear();
        if (bomb != null) stats.TryAddConsumable(bomb);
        if (healthPotion != null) stats.TryAddConsumable(healthPotion);

        stats.relics.Clear();
        foreach (ItemData relic in relics)
        {
            if (relic != null) stats.AddPurchasedItem(relic);
        }
    }

    private void SetupTrainingRoom()
    {
        GridPreset body = BuildPreset("TrainingGolem", new[]
        {
            ".BBB.",
            "BLBIB",
            "BBRBB",
            "BNBBB",
            ".BBB."
        }, (x, y) => (x >= 1 && x <= 3 && y >= 1 && y <= 3) ? 2 : 1);
        body.weaponIcon = enemySprite;

        GridPreset poke = BuildPreset("Poke", new[] { "B", "B" }, (x, y) => 1);

        EnemyData enemy = ScriptableObject.CreateInstance<EnemyData>();
        enemy.name = enemyName;
        enemy.enemyName = enemyName;
        enemy.enemySprite = enemySprite;
        enemy.tier = 1;
        enemy.bodyPreset = body;
        enemy.goldReward = 0;
        enemy.attacks = new List<EnemyAttack> { new EnemyAttack { pattern = poke, targeting = EnemyTargeting.Random } };

        Vector2Int board = turnManager.enemyGrid != null ? turnManager.enemyGrid.BoardSize : new Vector2Int(20, 8);
        RoomData room = ScriptableObject.CreateInstance<RoomData>();
        room.name = "TutorialRoom";
        room.difficulty = RoomDifficulty.Easy;
        room.slots = new List<RoomSlot>
        {
            new RoomSlot
            {
                tier = 1,
                fixedEnemy = enemy,
                position = new Vector2Int(Mathf.Max(0, (board.x - body.width) / 2), Mathf.Max(0, (board.y - body.height) / 2))
            }
        };

        EncounterSelection.NextRoom = room;
        EncounterSelection.NextDifficulty = null;
    }

    // Vytvorí grid z textu: B = Basic, L = Lava, I = Ice, N = Nature, R = Rock, . = prázdne
    private static GridPreset BuildPreset(string presetName, string[] rows, Func<int, int, int> thickness)
    {
        GridPreset preset = ScriptableObject.CreateInstance<GridPreset>();
        preset.name = presetName;
        preset.height = rows.Length;
        preset.width = rows[0].Length;
        preset.actionType = CellSetup.Gridtype.Character;
        preset.cells = new CellSetup[preset.width * preset.height];

        for (int y = 0; y < preset.height; y++)
        {
            for (int x = 0; x < preset.width; x++)
            {
                BlockElement element = rows[y][x] switch
                {
                    'B' => BlockElement.Basic,
                    'L' => BlockElement.Lava,
                    'I' => BlockElement.Ice,
                    'N' => BlockElement.Nature,
                    'R' => BlockElement.Rock,
                    _ => BlockElement.None
                };
                preset.cells[y * preset.width + x] = new CellSetup { element = element, thickness = Mathf.Clamp(thickness(x, y), 1, 3) };
            }
        }
        return preset;
    }

    // --- OBSAH TUTORIÁLU ---

    private void BuildSteps()
    {
        const string C1 = "Welcome", C2 = "Battlefield", C3 = "Attacking", C4 = "Items", C5 = "Enemy turn",
                     C6 = "Elements", C7 = "Healing", C8 = "Win the fight", C9 = "After the fight";

        // 1. Cieľ hry
        InfoStep(C1, "Welcome to Reticulum!",
            "This short tutorial teaches you everything you need for your first run.\n" +
            "Use <b>Next</b> to continue, <b>Skip chapter</b> to jump over a topic you already know, or <b>Exit tutorial</b> at any time.");
        InfoStep(C1, "Goal of the game",
            "Travel across the map from the start to the very end and <b>defeat the Boss</b>.\n" +
            "On the way you fight enemies, buy gear in shops, open chests and rest in meadows.\n" +
            "If you lose <b>all</b> your health cells, the run is over and you start again.");

        // 2. Bojisko
        InfoStep(C2, "Your health grid",
            "This is <b>you</b>. Every cell is a piece of your health. Cells have <b>1–3 layers</b> – every point of damage removes one layer.\n" +
            "The white outline marks your <b>Max HP</b>. A destroyed Max HP cell can be healed back later.\n" +
            "Damage stays with you between fights!", PlayerGridRect);
        InfoStep(C2, "The enemy",
            "This is your enemy's health grid. Destroy <b>every</b> cell of all enemies to win the fight.\n" +
            "Enemies use the same cells and elements as you do.", EnemyGridRect);
        InfoStep(C2, "Portraits",
            "At the top you see your character and the enemies in this fight.\n" +
            "During the enemy turn a <color=#FF5555>red dot</color> shows which enemy is attacking.", PortraitsRect);
        InfoStep(C2, "Stamina",
            "Every turn you get full <b>Stamina</b>. Weapons and items cost stamina.\n" +
            "When you run out, your turn ends automatically.", StaminaRect);

        // 3. Útok
        InfoStep(C3, "Weapons",
            "These are your weapons. <b>Hover</b> over a weapon to see its <b>shape</b> and its <b>stamina cost</b> (the stamina it would use blinks).\n" +
            "Every cell of the shape deals damage equal to its layers.", WeaponsRect);
        ActionStep(C3, "Attack!",
            "Click a weapon (or press <b>1–4</b>), move the mouse over the enemy and <b>left click</b> to attack.\n" +
            "<b>Right click</b> rotates the weapon by 90°, <b>Esc</b> cancels the selection.",
            WeaponsAndEnemyRect, () => weaponsUsed > weaponsAtStepStart, onEnter: () => weaponsAtStepStart = weaponsUsed);

        // 4. Predmety
        InfoStep(C4, "Potions & items",
            "Items are used once and also cost stamina. You can carry up to <b>4</b>:\n" +
            "• <b>Bomb</b> – hits the enemy with a random shape\n" +
            "• <b>Health Potion</b> – heals your own cells\n" +
            "• <b>Smoke Bomb</b> – escape from a fight (no reward)\n" +
            "• <b>Mystic Powder</b> – your next weapon attack is free", ConsumablesRect);
        ActionStep(C4, "Throw the Bomb",
            "Click the <b>Bomb</b> and place it on the enemy with a left click (right click rotates it).",
            ConsumablesAndEnemyRect, () => bomb == null || !stats.consumables.Contains(bomb));
        InfoStep(C4, "Relics",
            "Relics are permanent passive bonuses:\n" +
            "• <b>Shield of Courage</b> – at the start of your turn one random cell is protected for the whole round (needs at least 4 living cells)\n" +
            "• <b>Fast Feather</b> – +2 maximum stamina in every fight", RelicsRect);

        // 5. Ťah nepriateľa
        ActionStep(C5, "End your turn",
            "You can still attack, but when you are done, click <b>End Turn</b>.",
            EndTurnRect, () => playerTurnsStarted >= 2 || turnManager.State == TurnState.EnemyTurn,
            onSkip: ForceEndTurn);
        WaitStep(C5, "Enemy turn",
            "Now the enemy attacks. Its attack name and shape appear next to your weapons and the <color=#FF5555>red cells</color> show where it will hit.\n" +
            "After all attacks, the <b>elements</b> of both sides activate (orange cells = lava hits).",
            PlayerGridRect, () => playerTurnsStarted >= 2);

        // 6. Živly
        InfoStep(C6, "Elements",
            "Some cells have an <b>element</b>. Their abilities trigger at the <b>end of every round</b> – for your cells and for the enemy's cells.\n" +
            "<b>Basic</b> cells have no ability.", PlayerGridRect);
        InfoStep(C6, "Lava",
            "Every Lava cell deals <b>1 damage</b> to a random cell of the opponent each round.\n" +
            "Lava deals <b>+1</b> damage to <b>Ice</b> but <b>nothing</b> to <b>Rock</b>.", () => ElementRect(BlockElement.Lava));
        InfoStep(C6, "Ice",
            "Ice cells can <b>dodge</b> any hit. The more layers, the better: 1 layer = 30 %, 2 layers = 50 %, 3 layers = 70 %.",
            () => ElementRect(BlockElement.Ice));
        InfoStep(C6, "Nature",
            "Each round one Nature cell <b>grows</b> to a random neighbour:\n" +
            "• empty space -> new Nature cell with 1 layer\n" +
            "• Basic cell -> turns into Nature\n" +
            "• Nature cell -> +1 layer", () => ElementRect(BlockElement.Nature));
        InfoStep(C6, "Rock",
            "Each round every Rock cell has a <b>50 %</b> chance to gain <b>+1 layer</b> (up to 3) – even above its original maximum.",
            () => ElementRect(BlockElement.Rock));
        InfoStep(C6, "Tough enemies",
            "Some enemies (like the Boss) <b>rotate</b> or <b>change their elements</b> every round: Lava -> Ice -> Nature -> Rock -> Lava.\n" +
            "Tip: destroy enemy <b>Lava</b> cells early – they hurt you every round!", EnemyGridRect);

        // 7. Liečenie
        ActionStep(C7, "Drink the Health Potion",
            "You took some damage. Click the <b>Health Potion</b> and place it on <b>your own</b> grid to restore lost layers.",
            ConsumablesAndPlayerRect, () => healthPotion == null || !stats.consumables.Contains(healthPotion));

        // 8. Dokonči súboj
        ActionStep(C8, "Finish the fight",
            "Destroy every enemy cell to win. Use your weapons, end your turn when you run out of stamina and keep going!",
            EnemyGridRect, () => combatOver);

        // 9. Po súboji
        InfoStep(C9, "Rewards",
            "After winning you get <b>gold</b> and choose one reward:\n" +
            "• <b>+Max HP</b> – add new cells to your health grid\n" +
            "• <b>Element cell</b> – turn one of your cells into Lava, Ice, Nature or Rock\n" +
            "• <b>+1 Weapon cell</b> – make one of your weapons bigger");
        InfoStep(C9, "The map",
            "Choose your path on the map:\n" +
            "• <b>Encounter</b> – a fight (medium encounters are harder)\n" +
            "• <b>Shop</b> – spend gold on weapons, potions, relics and upgrades\n" +
            "• <b>Treasure</b> – a chest with loot\n" +
            "• <b>Rest (Meadow)</b> – heal all your cells to full\n" +
            "• <b>?</b> – a random fight, treasure or rest\n" +
            "• <b>Boss</b> – the final fight");
        InfoStep(C9, "Tips",
            "• Your health does <b>not</b> refill between fights – plan your rests.\n" +
            "• Hover over weapons to compare cost and damage before attacking.\n" +
            "• Ice and Rock cells make great armour, Lava and Nature make great attackers.");
        steps.Add(new Step
        {
            chapter = C9,
            title = "You are ready!",
            text = "That's everything. Good luck on your first run!",
            isFinal = true
        });
    }

    private int weaponsAtStepStart;

    private void InfoStep(string chapter, string title, string text, Func<Rect?> target = null)
    {
        steps.Add(new Step { chapter = chapter, title = title, text = text, target = target });
    }

    private void ActionStep(string chapter, string title, string text, Func<Rect?> target, Func<bool> isDone,
        Action onEnter = null, Action onSkip = null)
    {
        steps.Add(new Step
        {
            chapter = chapter, title = title, text = text, target = target, isDone = isDone,
            onEnter = onEnter, onSkip = onSkip, needsCombat = true, playerActs = true
        });
    }

    // Krok bez akcie hráča - čaká, kým sa niečo v hre stane (napr. ťah nepriateľa)
    private void WaitStep(string chapter, string title, string text, Func<Rect?> target, Func<bool> isDone)
    {
        steps.Add(new Step { chapter = chapter, title = title, text = text, target = target, isDone = isDone, needsCombat = true });
    }

    // --- PRIEBEH ---

    private IEnumerator RunSteps()
    {
        for (currentIndex = 0; currentIndex < steps.Count; currentIndex++)
        {
            Step step = steps[currentIndex];
            if (step.needsCombat && combatOver) continue;

            advanceRequested = false;
            chapterSkipRequested = false;
            step.onEnter?.Invoke();
            ShowStep(step);

            while (!advanceRequested && !chapterSkipRequested)
            {
                if (playerDefeated)
                {
                    yield return ShowDefeat();
                    yield break;
                }
                if (step.isDone != null && (step.isDone() || (step.needsCombat && combatOver))) break;

                // Ovládanie je odomknuté iba počas krokov, kde má hráč niečo urobiť
                turnManager.InputLocked = !step.playerActs;
                yield return null;
            }

            turnManager.InputLocked = true;

            if (chapterSkipRequested)
            {
                string chapter = step.chapter;
                while (currentIndex + 1 < steps.Count && steps[currentIndex + 1].chapter == chapter) currentIndex++;
            }

            // Krátka pauza, aby hráč videl výsledok svojej akcie
            if (step.isDone != null && !advanceRequested && !chapterSkipRequested) yield return new WaitForSeconds(0.6f);
        }
    }

    private IEnumerator ShowDefeat()
    {
        turnManager.InputLocked = true;
        ShowStep(new Step
        {
            chapter = "Defeat",
            title = "You were defeated",
            text = "Don't worry – this was only practice. Head back to the menu and start a real run!",
            isFinal = true
        });
        yield break;
    }

    private void ForceEndTurn()
    {
        turnManager.InputLocked = false;
        turnManager.EndPlayerTurn();
    }

    private void OnNextClicked()
    {
        Step step = CurrentStep;
        if (step != null && step.isFinal)
        {
            ExitTutorial();
            return;
        }
        advanceRequested = true;
    }

    private void OnSkipStepClicked()
    {
        CurrentStep?.onSkip?.Invoke();
        advanceRequested = true;
    }

    private void OnSkipChapterClicked()
    {
        CurrentStep?.onSkip?.Invoke();
        chapterSkipRequested = true;
    }

    private void ExitTutorial()
    {
        if (exiting) return;
        exiting = true;

        if (turnManager != null) turnManager.InputLocked = true;
        LoadingScene.Load(SceneIds.Menu);
    }

    private Step CurrentStep => displayedStep;
    private Step displayedStep;

    private void Update()
    {
        if (panel == null || displayedStep == null) return;

        // Enter / medzerník = Next (iba pri informačných krokoch)
        if (Keyboard.current != null && displayedStep.isDone == null
            && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame))
        {
            OnNextClicked();
        }

        UpdateHighlight();
    }

    // --- UI ---

    private void ShowStep(Step step)
    {
        displayedStep = step;

        int chapterNumber = 0;
        int chapterCount = 0;
        string lastChapter = null;
        foreach (Step s in steps)
        {
            if (s.chapter == lastChapter) continue;
            lastChapter = s.chapter;
            chapterCount++;
            if (s.chapter == step.chapter) chapterNumber = chapterCount;
        }

        chapterLabel.text = chapterNumber > 0 ? $"{chapterNumber}/{chapterCount}  ·  {step.chapter.ToUpper()}" : step.chapter.ToUpper();
        titleLabel.text = step.title;
        bodyLabel.text = step.text;

        bool waitsForGame = step.isDone != null;
        nextButton.gameObject.SetActive(!waitsForGame);
        nextButtonLabel.text = step.isFinal ? "Back to menu" : "Next";
        skipStepButton.gameObject.SetActive(waitsForGame);
        skipChapterButton.gameObject.SetActive(!step.isFinal);
        exitButton.gameObject.SetActive(!step.isFinal);

        UpdateHighlight();
    }

    private void UpdateHighlight()
    {
        Rect? target = displayedStep?.target?.Invoke();
        bool hasTarget = target.HasValue && target.Value.width > 1f && target.Value.height > 1f;
        frame.gameObject.SetActive(hasTarget);

        // Panel dáme na opačnú stranu obrazovky, ako je zvýraznená vec
        bool targetInLowerHalf = hasTarget && target.Value.center.y < Screen.height * 0.5f;
        SetPanelAtTop(targetInLowerHalf);

        if (!hasTarget) return;

        const float padding = 12f;
        Rect r = target.Value;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, r.min, null, out Vector2 min);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, r.max, null, out Vector2 max);
        frame.anchoredPosition = (min + max) * 0.5f;
        frame.sizeDelta = new Vector2(Mathf.Abs(max.x - min.x) + padding * 2f, Mathf.Abs(max.y - min.y) + padding * 2f);

        float alpha = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 5f);
        Color c = highlightColor;
        c.a = alpha;
        foreach (Image bar in frameBars) bar.color = c;
    }

    private void SetPanelAtTop(bool top)
    {
        float y = top ? 1f : 0f;
        panel.anchorMin = new Vector2(0.5f, y);
        panel.anchorMax = new Vector2(0.5f, y);
        panel.pivot = new Vector2(0.5f, y);
        panel.anchoredPosition = new Vector2(0f, top ? -24f : 24f);
    }

    private void BuildUI()
    {
        var canvasGo = new GameObject("TutorialCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500; // Nad hrou, pod loading obrazovkou (999)
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasRect = canvasGo.GetComponent<RectTransform>();

        // Rámik zvýraznenia (4 pásiky okolo cieľa)
        frame = CreateRect("Highlight", canvasRect);
        frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(0.5f, 0.5f);
        const float t = 6f;
        frameBars = new[]
        {
            CreateBar(frame, new Vector2(0, 1), new Vector2(1, 1), new Vector2(-t, 0), new Vector2(t, t)),
            CreateBar(frame, new Vector2(0, 0), new Vector2(1, 0), new Vector2(-t, -t), new Vector2(t, 0)),
            CreateBar(frame, new Vector2(0, 0), new Vector2(0, 1), new Vector2(-t, 0), new Vector2(0, 0)),
            CreateBar(frame, new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 0), new Vector2(t, 0))
        };

        // Panel s textom
        panel = CreateRect("Panel", canvasRect);
        panel.sizeDelta = new Vector2(1240f, 300f);
        Image panelBg = panel.gameObject.AddComponent<Image>();
        panelBg.color = new Color(0.05f, 0.05f, 0.08f, 0.92f);
        AddOutline(panel, new Color(1f, 1f, 1f, 0.35f));

        chapterLabel = CreateText(panel, "Chapter", 20, highlightColor, TextAlignmentOptions.TopLeft);
        SetRect(chapterLabel.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(28, -46), new Vector2(-28, -16));

        titleLabel = CreateText(panel, "Title", 34, Color.white, TextAlignmentOptions.TopLeft);
        titleLabel.fontStyle = FontStyles.Bold;
        SetRect(titleLabel.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(28, -92), new Vector2(-28, -46));

        bodyLabel = CreateText(panel, "Body", 24, new Color(0.9f, 0.9f, 0.9f), TextAlignmentOptions.TopLeft);
        bodyLabel.textWrappingMode = TextWrappingModes.Normal;
        SetRect(bodyLabel.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(28, 74), new Vector2(-28, -98));

        // Tlačidlá: vľavo ukončenie, vpravo preskočenie a Next
        exitButton = CreateButton(panel, "Exit tutorial", new Color(0.45f, 0.15f, 0.15f), ExitTutorial);
        PlaceButton(exitButton, left: true, offset: 28f, width: 200f);

        nextButton = CreateButton(panel, "Next", new Color(0.2f, 0.55f, 0.25f), OnNextClicked);
        nextButtonLabel = nextButton.GetComponentInChildren<TextMeshProUGUI>();
        PlaceButton(nextButton, left: false, offset: 28f, width: 210f);

        skipStepButton = CreateButton(panel, "Skip step", new Color(0.3f, 0.3f, 0.35f), OnSkipStepClicked);
        PlaceButton(skipStepButton, left: false, offset: 28f, width: 210f);

        skipChapterButton = CreateButton(panel, "Skip chapter", new Color(0.3f, 0.3f, 0.35f), OnSkipChapterClicked);
        PlaceButton(skipChapterButton, left: false, offset: 256f, width: 210f);
    }

    private static RectTransform CreateRect(string objectName, Transform parent)
    {
        var go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static Image CreateBar(RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        RectTransform rect = CreateRect("Bar", parent);
        SetRect(rect, anchorMin, anchorMax, offsetMin, offsetMax);
        Image img = rect.gameObject.AddComponent<Image>();
        img.raycastTarget = false;
        return img;
    }

    private static void AddOutline(RectTransform rect, Color color)
    {
        Outline outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(2f, -2f);
    }

    private TextMeshProUGUI CreateText(Transform parent, string objectName, float size, Color color, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(objectName, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.richText = true;
        text.raycastTarget = false;
        return text;
    }

    private Button CreateButton(Transform parent, string label, Color color, UnityEngine.Events.UnityAction onClick)
    {
        RectTransform rect = CreateRect(label + "Button", parent);
        Image img = rect.gameObject.AddComponent<Image>();
        img.color = color;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = img;
        button.onClick.AddListener(onClick);

        TextMeshProUGUI text = CreateText(rect, "Text", 22, Color.white, TextAlignmentOptions.Center);
        text.text = label;
        SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return button;
    }

    private static void PlaceButton(Button button, bool left, float offset, float width)
    {
        RectTransform rect = (RectTransform)button.transform;
        float x = left ? 0f : 1f;
        rect.anchorMin = rect.anchorMax = new Vector2(x, 0f);
        rect.pivot = new Vector2(x, 0f);
        rect.sizeDelta = new Vector2(width, 50f);
        rect.anchoredPosition = new Vector2(left ? offset : -offset, 18f);
    }

    // --- ZVÝRAZNENIE: obdĺžnik na obrazovke ---

    private Rect? PlayerGridRect() => turnManager.playerGrid != null ? ScreenRectOf(turnManager.playerGrid.GetComponentsInChildren<SlotValue>()) : null;

    private Rect? EnemyGridRect()
    {
        if (turnManager.enemyGrid == null) return null;
        var alive = new List<Component>();
        foreach (SlotValue block in turnManager.enemyGrid.GetComponentsInChildren<SlotValue>())
        {
            if (block.IsAlive) alive.Add(block);
        }
        return ScreenRectOf(alive);
    }

    private Rect? StaminaRect() => turnManager.StaminaContainer != null ? ScreenRectOf(new Component[] { turnManager.StaminaContainer }) : null;

    private Rect? PortraitsRect()
    {
        if (shopUI == null) return null;
        var parts = new List<Component>();
        if (shopUI.CharacterPortraitImage != null) parts.Add(shopUI.CharacterPortraitImage);
        if (shopUI.EnemyPortraitImages != null)
        {
            foreach (Image img in shopUI.EnemyPortraitImages)
            {
                if (img != null && img.enabled && img.sprite != null) parts.Add(img);
            }
        }
        return ScreenRectOf(parts);
    }

    private Rect? WeaponsRect() => SlotsRect(shopUI != null ? shopUI.WeaponSlotObjects : null);
    private Rect? ConsumablesRect() => SlotsRect(shopUI != null ? shopUI.ConsumableSlotObjects : null);
    private Rect? RelicsRect() => SlotsRect(shopUI != null ? shopUI.RelicSlotObjects : null);

    private Rect? WeaponsAndEnemyRect() => Union(WeaponsRect(), EnemyGridRect());
    private Rect? ConsumablesAndEnemyRect() => Union(ConsumablesRect(), EnemyGridRect());
    private Rect? ConsumablesAndPlayerRect() => Union(ConsumablesRect(), PlayerGridRect());

    private Rect? EndTurnRect()
    {
        var parts = new List<Component>();
        foreach (EndTurnButton button in FindObjectsByType<EndTurnButton>(FindObjectsInactive.Exclude))
        {
            if (button.isActiveAndEnabled) parts.Add(button);
        }
        return ScreenRectOf(parts);
    }

    // Prvé živé políčko daného živlu - najprv u hráča, potom u nepriateľa
    private Rect? ElementRect(BlockElement element)
    {
        foreach (GridManager grid in new[] { turnManager.playerGrid, turnManager.enemyGrid })
        {
            if (grid == null) continue;
            foreach (SlotValue block in grid.GetComponentsInChildren<SlotValue>())
            {
                if (block.IsAlive && block.element == element) return ScreenRectOf(new Component[] { block });
            }
        }
        return PlayerGridRect();
    }

    private static Rect? SlotsRect(GameObject[] slots)
    {
        if (slots == null) return null;
        var parts = new List<Component>();
        foreach (GameObject slot in slots)
        {
            if (slot != null && slot.activeInHierarchy) parts.Add(slot.transform);
        }
        return ScreenRectOf(parts);
    }

    private static Rect? Union(Rect? a, Rect? b)
    {
        if (!a.HasValue) return b;
        if (!b.HasValue) return a;
        return Rect.MinMaxRect(Mathf.Min(a.Value.xMin, b.Value.xMin), Mathf.Min(a.Value.yMin, b.Value.yMin),
                               Mathf.Max(a.Value.xMax, b.Value.xMax), Mathf.Max(a.Value.yMax, b.Value.yMax));
    }

    // Spojí obdĺžniky na obrazovke všetkých daných objektov (UI aj 2D sprity v scéne)
    private static Rect? ScreenRectOf(IEnumerable<Component> parts)
    {
        Rect? result = null;
        foreach (Component part in parts)
        {
            if (part == null || !part.gameObject.activeInHierarchy) continue;
            result = Union(result, ScreenRectOf(part));
        }
        return result;
    }

    private static Rect? ScreenRectOf(Component part)
    {
        Camera worldCam = Camera.main;

        // UI objekt v Canvase
        Canvas canvas = part.GetComponentInParent<Canvas>();
        if (canvas != null && part.transform is RectTransform rect)
        {
            Canvas root = canvas.rootCanvas;
            Camera cam = root.renderMode == RenderMode.ScreenSpaceOverlay ? null : (root.worldCamera != null ? root.worldCamera : worldCam);
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Rect? r = null;
            foreach (Vector3 corner in corners)
            {
                Vector2 p = RectTransformUtility.WorldToScreenPoint(cam, corner);
                r = Union(r, new Rect(p, Vector2.zero));
            }
            return r;
        }

        // 2D objekt v scéne - hranice jeho rendererov (alebo collidera)
        if (worldCam == null) return null;
        Bounds? bounds = null;
        foreach (Renderer renderer in part.GetComponentsInChildren<Renderer>())
        {
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
            if (bounds.HasValue) { Bounds b = bounds.Value; b.Encapsulate(renderer.bounds); bounds = b; }
            else bounds = renderer.bounds;
        }
        if (!bounds.HasValue)
        {
            Collider2D col = part.GetComponentInChildren<Collider2D>();
            if (col == null) return null;
            bounds = col.bounds;
        }

        Vector3 min = worldCam.WorldToScreenPoint(bounds.Value.min);
        Vector3 max = worldCam.WorldToScreenPoint(bounds.Value.max);
        return Rect.MinMaxRect(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y), Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));
    }
}
