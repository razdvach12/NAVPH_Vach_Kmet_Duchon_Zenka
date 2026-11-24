using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public enum TurnState { PlayerTurn, EnemyTurn, Victory, Defeat, Fled }

public class TurnManager : MonoBehaviour
{
    [Header("Gridy v súboji")]
    public GridManager playerGrid;
    public GridManager enemyGrid;

    [Header("Room (vyberie sa náhodná room so zvolenou obtiažnosťou)")]
    public RoomDifficulty difficulty = RoomDifficulty.Easy;
    public List<RoomData> rooms = new List<RoomData>();

    [Tooltip("Všetci nepriatelia, z ktorých sa náhodne vyberá podľa tieru miesta v room")]
    public List<EnemyData> enemyPool = new List<EnemyData>();

    [Header("Stamina hráča")]
    [Tooltip("Použije sa iba ak neexistuje PlayerStats (napr. pri spustení scény priamo v editore)")]
    public int maxStamina = 5;
    public int currentStamina;

    [Header("Časovanie útokov nepriateľov")]
    [SerializeField] private float attackPreviewTime = 0.7f; // Ako dlho svieti cieľ útoku pred zásahom
    [SerializeField] private float delayBetweenAttacks = 0.4f;
    [SerializeField] private float lavaPreviewTime = 0.4f; // Ako dlho svieti cieľ lávového zásahu

    [Header("Udalosti (napr. pre UI obrazovky)")]
    public UnityEvent onPlayerTurnStart;
    public UnityEvent onVictory;
    public UnityEvent onDefeat;
    public UnityEvent onFlee;

    [Header("Koniec súboja (výhra / útek cez Smoke Bomb)")]
    [Tooltip("Build Index scény, kam sa hráč vráti po výbere odmeny alebo po úteku (mapa)")]
    [FormerlySerializedAs("fleeSceneBuildIndex")]
    [SerializeField] private int mapSceneBuildIndex = SceneIds.Map;

    [Tooltip("Popup s výberom odmeny po výhre - ak nie je priradený, nájde sa automaticky")]
    [SerializeField] private VictoryRewardUI victoryReward;

    [Tooltip("Popup po prehre (návrat do menu) - ak nie je priradený, nájde sa automaticky")]
    [SerializeField] private DefeatPopupUI defeatPopup;

    // Zlato za porazených nepriateľov - pridá sa iba ak si ho hráč vyberie v popupe
    private int pendingGoldReward;
    private bool rewardChosen; // Ochrana proti dvojkliku na tlačidlá

    [Header("Zobrazenie staminy v PlayerSection (objekt Stamina)")]
    [Tooltip("Rodičovský objekt Stamina v PlayerSection - ak nie je priradený, nájde sa automaticky")]
    [SerializeField] private Transform staminaContainer;
    [Tooltip("Prefab políčka staminy (GridSlot)")]
    [SerializeField] private GameObject staminaGridSlotPrefab;
    [Tooltip("Prefab výplne staminy (SlotStamina)")]
    [SerializeField] private GameObject slotStaminaPrefab;
    [Tooltip("Rýchlosť pulzovania alfy staminy pri hoveri nad zbraňou")]
    [SerializeField] private float staminaPulseSpeed = 7f;
    [Tooltip("Minimálna hodnota alfy pri pulzovaní staminy")]
    [SerializeField] private float staminaMinPulseAlpha = 0.2f;

    // Cena zbrane, nad ktorou je myš (0 = nič sa nezobrazuje)
    private int previewStaminaCost;
    private float staminaPulseStartTime;
    private readonly List<SpriteRenderer[]> activeStaminaSlotRenderers = new List<SpriteRenderer[]>();

    // Mystický prášok - najbližší útok zbraňou je zadarmo (platí, kým sa nevyužije)
    private bool nextAttackFree;

    private ShopUIController shopUI;

    public TurnState State { get; private set; }
    public bool CanPlayerAct => State == TurnState.PlayerTurn && !InputLocked;

    // --- TUTORIÁL ---
    // Počas vysvetľovania tutoriál zamkne ovládanie (hráč nemôže útočiť ani ukončiť ťah)
    public bool InputLocked { get; set; }
    // Tutoriál si koniec súboja rieši sám - bez popupu odmeny/prehry, bez zlata a bez návratu na mapu
    public bool HandleCombatEndExternally { get; set; }
    // Zavolá sa po každom úspešnom útoku zbraňou
    public event System.Action WeaponUsed;
    // Objekt Stamina v PlayerSection (napr. pre zvýraznenie v tutoriáli)
    public Transform StaminaContainer
    {
        get
        {
            ResolveStaminaReferences();
            return staminaContainer;
        }
    }

    private void Awake()
    {
        // Ak playerGrid nie je priradený alebo je vo vypnutom objekte, nájdeme aktívny hráčov grid v scéne
        if (playerGrid == null || !playerGrid.gameObject.activeInHierarchy)
        {
            GridManager[] allGrids = FindObjectsByType<GridManager>(FindObjectsSortMode.None);
            foreach (GridManager gm in allGrids)
            {
                if (gm != null && gm.isPlayerGrid && gm.gameObject.name != "WeaponGrid")
                {
                    playerGrid = gm;
                    break;
                }
            }
        }

        PlayerStats stats = PlayerStats.EnsureInstance();

        if (stats != null)
        {
            maxStamina = stats.maxStamina;
            if (stats.HasRelic(RelicEffect.FastFeather)) maxStamina += 2;
        }
        currentStamina = maxStamina;
        UpdateStaminaUI();

        // Ak hráč prišiel z výberu postavy, použijeme jeho grid životov
        if (playerGrid != null && stats.playerHealthPreset != null)
        {
            playerGrid.SetSinglePreset(stats.playerHealthPreset);
        }

        // Výber z mapy má prednosť pred hodnotou z inšpektora (použije sa iba raz)
        RoomData selectedRoom = EncounterSelection.NextRoom;
        EncounterSelection.NextRoom = null;

        if (EncounterSelection.NextDifficulty.HasValue)
        {
            difficulty = EncounterSelection.NextDifficulty.Value;
            EncounterSelection.NextDifficulty = null;
        }

        if (selectedRoom != null) difficulty = selectedRoom.difficulty;

        if (enemyGrid != null)
        {
            RoomData room = selectedRoom != null ? selectedRoom : PickRandomRoom();
            if (room != null) enemyGrid.SetRoomEnemies(room.PickEnemies(enemyPool));
        }
    }

    // Bossfight má vlastnú hudbu, bežný súboj náhodne jeden z dvoch trackov
    private void PlayEncounterMusic()
    {
        if (MusicManager.Instance == null) return;

        if (difficulty == RoomDifficulty.Boss)
        {
            MusicManager.Instance.PlayMusic("BossBackground");
        }
        else
        {
            MusicManager.Instance.PlayMusic(Random.value < 0.5f ? "EncounterBackground1" : "EncounterBackground2");
        }
    }

    private RoomData PickRandomRoom()
    {
        var candidates = rooms.FindAll(r => r != null && r.difficulty == difficulty);
        if (candidates.Count == 0)
        {
            Debug.LogWarning($"Nie je priradená žiadna room s obtiažnosťou {difficulty}.");
            return null;
        }
        return candidates[Random.Range(0, candidates.Count)];
    }

    private IEnumerator Start()
    {
        // Tréningový súboj v tutoriáli sa do štatistík runu neráta
        if (!HandleCombatEndExternally && PlayerStats.Instance != null) PlayerStats.Instance.RecordEncounterVisited();

        PlayEncounterMusic();

        // Počkáme jeden frame, aby gridy stihli vygenerovať svoje políčka
        yield return null;

        RefreshUpperPartEnemies(-1);
        StartPlayerTurn();
    }

    private void RefreshUpperPartEnemies(int attackingEnemyIndex = -1)
    {
        if (shopUI == null) shopUI = FindFirstObjectByType<ShopUIController>();
        if (shopUI != null) shopUI.UpdateEnemyPortraits(attackingEnemyIndex);
    }

    public bool HasStaminaFor(GridPreset preset)
    {
        return CanPlayerAct && preset != null && currentStamina >= GetStaminaCost(preset);
    }

    // Skutočná cena útoku zbraňou - 0, ak je aktívny mystický prášok
    public int GetStaminaCost(GridPreset preset)
    {
        return nextAttackFree ? 0 : preset.staminaCost;
    }

    public void GrantFreeAttack()
    {
        nextAttackFree = true;
        UpdateStaminaUI();
    }

    // Pre consumables bez tvaru (napr. prášok) - cena je v ConsumableData.staminaCost
    public bool HasStamina(int cost)
    {
        return CanPlayerAct && currentStamina >= cost;
    }

    public void SpendStamina(int cost)
    {
        if (cost <= 0) return;

        currentStamina = Mathf.Max(0, currentStamina - cost);
        UpdateStaminaUI();
        if (currentStamina <= 0) EndPlayerTurn();
    }

    // Zavolá WeaponTargetingController po úspešnom útoku hráča
    public void OnPlayerUsedWeapon(GridPreset preset)
    {
        currentStamina = Mathf.Max(0, currentStamina - GetStaminaCost(preset));
        nextAttackFree = false;
        UpdateStaminaUI();
        WeaponUsed?.Invoke();
        if (CheckCombatEnd()) return;

        // Zatiaľ sa ťah končí automaticky po minutí celej staminy
        if (currentStamina <= 0) EndPlayerTurn();
    }

    // Zavolá WeaponTargetingController po úspešnom použití consumable s tvarom (Bomba, Health Potion)
    // Na rozdiel od zbrane nevyužije mystický prášok, ale skontroluje koniec súboja (bomba môže zabiť posledného nepriateľa)
    public void OnPlayerUsedConsumable(int cost)
    {
        currentStamina = Mathf.Max(0, currentStamina - Mathf.Max(0, cost));
        UpdateStaminaUI();
        if (CheckCombatEnd()) return;

        if (currentStamina <= 0) EndPlayerTurn();
    }

    public void EndPlayerTurn()
    {
        if (!CanPlayerAct) return;
        StartCoroutine(EnemyTurnRoutine());
    }

    private void StartPlayerTurn()
    {
        State = TurnState.PlayerTurn;

        // Maximálna stamina patrí hráčovi - ak existuje PlayerStats, berieme ju odtiaľ
        if (PlayerStats.Instance != null)
        {
            maxStamina = PlayerStats.Instance.maxStamina;
            if (PlayerStats.Instance.HasRelic(RelicEffect.FastFeather)) maxStamina += 2;
        }
        currentStamina = maxStamina;
        UpdateStaminaUI();
        ApplyShieldOfCourage();
        onPlayerTurnStart?.Invoke();
    }

    // Shield of Courage: na začiatku ťahu ochráni jedno náhodné živé políčko na celé kolo (ak má hráč aspoň 4 živé)
    private const int ShieldOfCourageMinAlive = 4;

    private void ApplyShieldOfCourage()
    {
        if (playerGrid == null) return;

        ItemData shield = PlayerStats.Instance != null ? PlayerStats.Instance.GetRelic(RelicEffect.ShieldOfCourage) : null;
        if (shield == null)
        {
            playerGrid.ClearProtectedSlot();
            return;
        }

        playerGrid.ProtectRandomAliveSlot(shield.icon, ShieldOfCourageMinAlive);
    }

    private void ResolveStaminaReferences()
    {
        if (staminaContainer == null)
        {
            staminaContainer = FindStaminaContainer(playerGrid != null ? playerGrid.transform : null);
        }

        if (staminaGridSlotPrefab == null && playerGrid != null && playerGrid.GridSlotPrefab != null)
        {
            staminaGridSlotPrefab = playerGrid.GridSlotPrefab;
        }

#if UNITY_EDITOR
        if (staminaGridSlotPrefab == null)
        {
            staminaGridSlotPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Grid/Hp/GridSlot.prefab");
        }
        if (slotStaminaPrefab == null)
        {
            slotStaminaPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Grid/SlotStamina.prefab");
        }
#endif
    }

    public static Transform FindStaminaContainer(Transform reference = null)
    {
        if (reference != null)
        {
            Transform current = reference;
            while (current != null)
            {
                Transform found = current.Find("Stamina");
                if (found != null) return found;
                current = current.parent;
            }
        }

        GameObject go = GameObject.Find("Stamina");
        return go != null ? go.transform : null;
    }

    public void UpdateStaminaUI()
    {
        ResolveStaminaReferences();
        PopulateStaminaContainer(staminaContainer, staminaGridSlotPrefab, slotStaminaPrefab, maxStamina, currentStamina);
        CacheStaminaSlotRenderers();
        ApplyStaminaPulseAlpha();
    }

    private void CacheStaminaSlotRenderers()
    {
        activeStaminaSlotRenderers.Clear();
        if (staminaContainer == null) return;

        for (int i = 0; i < staminaContainer.childCount; i++)
        {
            Transform slotTr = staminaContainer.GetChild(i);
            if (slotTr == null) continue;

            Transform staminaTr = slotTr.Find("SlotStamina");
            if (staminaTr != null)
            {
                activeStaminaSlotRenderers.Add(staminaTr.GetComponentsInChildren<SpriteRenderer>(true));
            }
        }
    }

    // Vygeneruje podľa maxStamina políčka GridSlot v objekte Stamina a podľa currentStamina do nich vloží SlotStamina
    public static void PopulateStaminaContainer(
        Transform container,
        GameObject gridSlotPrefab,
        GameObject slotStaminaPrefab,
        int maxStaminaCount,
        int currentStaminaCount)
    {
        if (container == null || gridSlotPrefab == null) return;
        if (!container.gameObject.scene.isLoaded) return;

        int totalSlots = Mathf.Max(0, maxStaminaCount);
        int filledSlots = Mathf.Clamp(currentStaminaCount, 0, totalSlots);

        for (int i = container.childCount - 1; i >= 0; i--)
        {
            GameObject child = container.GetChild(i).gameObject;
            if (Application.isPlaying)
            {
                child.transform.SetParent(null);
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }

        const float slotScale = 83.1f;
        const float slotSpacingY = 54f;
        const float slotOffsetX = 4f;
        const float slotCenterY = -2f;

        float topY = slotCenterY + (totalSlots - 1) * 0.5f * slotSpacingY;

        for (int i = 0; i < totalSlots; i++)
        {
            GameObject slotObj = Instantiate(gridSlotPrefab, container, false);
            slotObj.name = i == 0 ? "GridSlot" : $"GridSlot ({i})";

            float y = topY - i * slotSpacingY;
            Vector3 worldPos = slotObj.transform.position;
            slotObj.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);
            slotObj.transform.localPosition = new Vector3(slotOffsetX, y, slotObj.transform.localPosition.z);
            slotObj.transform.localRotation = Quaternion.identity;
            slotObj.transform.localScale = new Vector3(slotScale, slotScale, slotScale);

            Collider2D slotCol = slotObj.GetComponent<Collider2D>();
            if (slotCol != null) slotCol.enabled = false;

            if (i < filledSlots && slotStaminaPrefab != null)
            {
                GameObject staminaObj = Instantiate(slotStaminaPrefab, slotObj.transform, false);
                staminaObj.name = "SlotStamina";
                staminaObj.transform.localPosition = new Vector3(0f, 0f, -0.1f);
                staminaObj.transform.localRotation = Quaternion.identity;
                staminaObj.transform.localScale = new Vector3(0.9181709f, 0.9181709f, 0.9181709f);

                Collider2D staminaCol = staminaObj.GetComponent<Collider2D>();
                if (staminaCol != null) staminaCol.enabled = false;
            }
        }
    }

    public void ShowStaminaCost(int cost)
    {
        int clampedCost = Mathf.Max(0, cost);
        if (previewStaminaCost != clampedCost)
        {
            previewStaminaCost = clampedCost;
            staminaPulseStartTime = Time.unscaledTime;
        }
        ApplyStaminaPulseAlpha();
    }

    public void HideStaminaCost()
    {
        if (previewStaminaCost == 0) return;
        previewStaminaCost = 0;
        ApplyStaminaPulseAlpha();
    }

    private void Update()
    {
        if (previewStaminaCost > 0)
        {
            ApplyStaminaPulseAlpha();
        }
    }

    // Pulzuje alfu posledných previewStaminaCost políčok SlotStamina (tých, ktoré sa pri útoku minú)
    private void ApplyStaminaPulseAlpha()
    {
        if (activeStaminaSlotRenderers.Count == 0 && currentStamina > 0 && staminaContainer != null && staminaContainer.childCount > 0)
        {
            CacheStaminaSlotRenderers();
        }

        int filledCount = activeStaminaSlotRenderers.Count;
        if (filledCount == 0) return;

        int pulseCount = previewStaminaCost > 0 ? Mathf.Clamp(previewStaminaCost, 0, filledCount) : 0;
        int pulseStartIndex = filledCount - pulseCount;

        float pulseAlpha = 1f;
        if (pulseCount > 0)
        {
            float t = (Mathf.Cos((Time.unscaledTime - staminaPulseStartTime) * staminaPulseSpeed) + 1f) * 0.5f;
            pulseAlpha = Mathf.Lerp(staminaMinPulseAlpha, 1f, t);
        }

        for (int i = 0; i < filledCount; i++)
        {
            float targetAlpha = i >= pulseStartIndex ? pulseAlpha : 1f;
            SpriteRenderer[] renderers = activeStaminaSlotRenderers[i];
            if (renderers == null) continue;

            for (int r = 0; r < renderers.Length; r++)
            {
                SpriteRenderer sr = renderers[r];
                if (sr == null) continue;

                Color c = sr.color;
                if (!Mathf.Approximately(c.a, targetAlpha))
                {
                    c.a = targetAlpha;
                    sr.color = c;
                }
            }
        }
    }

    private IEnumerator EnemyTurnRoutine()
    {
        State = TurnState.EnemyTurn;
        HideStaminaCost();

        for (int i = 0; i < enemyGrid.Enemies.Count; i++)
        {
            EnemyData enemy = enemyGrid.Enemies[i];
            if (enemy == null || !enemyGrid.IsEnemyAlive(i)) continue;

            EnemyAttack attack = enemy.GetRandomAttack();
            if (attack == null || attack.pattern == null) continue;

            GridSlot target = playerGrid.FindEnemyAttackTarget(attack.pattern, attack.targeting);
            if (target == null) continue;

            // Zvýrazníme práve útočiaceho nepriateľa červeným obrysom Max HP a červenou guličkou nad portrétom
            enemyGrid.SetAttackingEnemy(i);
            RefreshUpperPartEnemies(i);

            // Názov a tvar útoku sa ukáže v stĺpci vedľa zbraní (pod náhľadom zbrane)
            if (shopUI != null) shopUI.ShowEnemyAttackPreview(attack);

            playerGrid.ShowEnemyAttackPreview(target, attack.pattern);
            yield return new WaitForSeconds(attackPreviewTime);

            playerGrid.ApplyEnemyAttack(target, attack.pattern);
            if (MusicManager.Instance != null) MusicManager.Instance.PlayHitSound();
            Debug.Log($"{enemy.enemyName} útočí: {attack.pattern.name}");

            // Po útoku vrátime obrys nepriateľa späť na bielo a skryjeme červenú guličku
            enemyGrid.SetAttackingEnemy(-1);
            RefreshUpperPartEnemies(-1);
            if (shopUI != null) shopUI.HideEnemyAttackPreview();

            if (CheckCombatEnd()) yield break;
            yield return new WaitForSeconds(delayBetweenAttacks);
        }

        // Živly hráča, potom živly nepriateľov
        yield return playerGrid.ApplyElementEffects(enemyGrid, lavaPreviewTime);
        if (CheckCombatEnd()) yield break;
        yield return new WaitForSeconds(delayBetweenAttacks);

        yield return enemyGrid.ApplyElementEffects(playerGrid, lavaPreviewTime);
        if (CheckCombatEnd()) yield break;
        yield return new WaitForSeconds(delayBetweenAttacks);

        ApplyEndOfRoundAbilities();
        StartPlayerTurn();
    }

    // Schopnosti živých nepriateľov na konci kola (napr. boss sa otočí a zmení živly)
    private void ApplyEndOfRoundAbilities()
    {
        for (int i = 0; i < enemyGrid.Enemies.Count; i++)
        {
            EnemyData enemy = enemyGrid.Enemies[i];
            if (enemy == null || !enemyGrid.IsEnemyAlive(i)) continue;

            if (enemy.rotateGridEachRound) enemyGrid.RotateEnemyClockwise(i);
            if (enemy.cycleElementsEachRound) enemyGrid.CycleEnemyElements(i);
        }
    }

    // Vráti true, ak súboj skončil
    private bool CheckCombatEnd()
    {
        // Aktualizujeme portréty nepriateľov (ak bol niektorý nepriateľ zabitý, jeho obrázok zmizne)
        RefreshUpperPartEnemies(-1);

        if (!playerGrid.HasAnyAliveBlock())
        {
            State = TurnState.Defeat;
            Debug.Log("Prehra - hráč prišiel o všetky životy.");
            if (MusicManager.Instance != null)
            {
                MusicManager.Instance.StopMusic();
                MusicManager.Instance.PlaySound("Loss");
            }
            if (!HandleCombatEndExternally) OnDefeat();
            onDefeat?.Invoke();
            return true;
        }

        if (!enemyGrid.HasAnyAliveBlock())
        {
            State = TurnState.Victory;
            Debug.Log("Výhra - všetci nepriatelia sú porazení.");
            if (MusicManager.Instance != null)
            {
                MusicManager.Instance.PlaySound("Win");
                MusicManager.Instance.PlayMusic("MapBackground"); // Pokračuje plynulo aj po návrate na mapu
            }
            if (!HandleCombatEndExternally) OnVictory();
            onVictory?.Invoke();
            return true;
        }

        return false;
    }

    // Po výhre hráč vždy dostane zlato za nepriateľov a popup mu ukáže, koľko získal
    private void OnVictory()
    {
        SavePlayerHp();

        pendingGoldReward = 0;
        foreach (var enemy in enemyGrid.Enemies)
        {
            if (enemy != null) pendingGoldReward += enemy.goldReward;
        }

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.encountersDefeated++;
            PlayerStats.Instance.AddMoney(pendingGoldReward);
        }
        rewardChosen = false;

        if (victoryReward == null) victoryReward = FindAnyObjectByType<VictoryRewardUI>(FindObjectsInactive.Include); // Nájde ho aj vo vypnutom Canvase
        if (victoryReward != null)
        {
            victoryReward.Show(pendingGoldReward);
        }
        else
        {
            // Bez popupu v scéne (napr. testovanie) hráč ostane v scéne
            Debug.LogWarning("V scéne chýba VictoryRewardUI - hráč ostáva v scéne.");
            rewardChosen = true;
        }
    }

    // Tlačidlo "Continue" v popupe - vráti hráča na mapu (zlato už bolo pridané pri výhre)
    public void ContinueAfterVictory()
    {
        if (State != TurnState.Victory || rewardChosen) return;
        rewardChosen = true;

        // Po porazení bossa je hra dohraná - ide sa na záverečnú scénu namiesto mapy
        bool wasBossFight = difficulty == RoomDifficulty.Boss
            || (MapRunState.CurrentNode != null && MapRunState.CurrentNode.type == NodeType.Boss);
        if (wasBossFight)
        {
            MapRunState.Clear();
            LoadingScene.Load(SceneIds.Final);
            return;
        }

        ReturnToMap();
    }

    // Po prehre sa zobrazí death popup s tlačidlom späť do menu
    private void OnDefeat()
    {
        rewardChosen = false;

        if (defeatPopup == null) defeatPopup = FindAnyObjectByType<DefeatPopupUI>(FindObjectsInactive.Include); // Nájde ho aj vo vypnutom Canvase
        if (defeatPopup != null)
        {
            defeatPopup.Show();
        }
        else
        {
            Debug.LogWarning("V scéne chýba DefeatPopupUI - hráč ostáva v scéne.");
            rewardChosen = true;
        }
    }

    // Tlačidlo "Menu" v death popupe - ukončí run a vráti hráča do menu
    public void ReturnToMenuAfterDefeat()
    {
        if (State != TurnState.Defeat || rewardChosen) return;
        rewardChosen = true;

        MapRunState.Clear();
        LoadingScene.Load(SceneIds.Menu);
    }

    private void ReturnToMap()
    {
        // Po výhre už hrá hudba mapy - necháme ju hrať aj počas loadingu
        LoadingScene.Load(mapSceneBuildIndex, State == TurnState.Victory);
    }

    // Stratené HP sa prenesie do ďalšieho encountru
    public void SavePlayerHp()
    {
        if (playerGrid != null)
        {
            playerGrid.SavePlayerHpToStats();
        }
    }

    // Smoke Bomb - hráč utečie zo súboja bez odmeny, stratené HP sa zachová
    public bool TryFlee()
    {
        if (!CanPlayerAct) return false;

        State = TurnState.Fled;
        SavePlayerHp();
        Debug.Log("Útek - hráč utiekol zo súboja.");
        onFlee?.Invoke();

        ReturnToMap();
        return true;
    }
}
