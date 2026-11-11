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
    [SerializeField] private int mapSceneBuildIndex = 5;

    [Tooltip("Popup s výberom odmeny po výhre - ak nie je priradený, nájde sa automaticky")]
    [SerializeField] private VictoryRewardUI victoryReward;

    // Zlato za porazených nepriateľov - pridá sa iba ak si ho hráč vyberie v popupe
    private int pendingGoldReward;
    private bool rewardChosen; // Ochrana proti dvojkliku na tlačidlá

    [Header("Zobrazenie staminy nad gridom hráča")]
    [Tooltip("Ak nie je priradený, vytvorí sa automaticky")]
    [SerializeField] private TextMeshPro staminaText;
    [SerializeField] private float staminaTextOffset = 0.4f; // Vzdialenosť nad horným okrajom gridu

    // Cena zbrane, nad ktorou je myš (0 = nič sa nezobrazuje)
    private int previewStaminaCost;

    // Mystický prášok - najbližší útok zbraňou je zadarmo (platí, kým sa nevyužije)
    private bool nextAttackFree;

    private ShopUIController shopUI;

    public TurnState State { get; private set; }
    public bool CanPlayerAct => State == TurnState.PlayerTurn;

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

        CreateStaminaTextIfMissing();

        // Ak hráč prišiel z výberu postavy, použijeme jeho grid životov
        if (playerGrid != null && PlayerStats.Instance != null && PlayerStats.Instance.playerHealthPreset != null)
        {
            playerGrid.SetSinglePreset(PlayerStats.Instance.playerHealthPreset);
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
        UpdateStaminaText();
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
        UpdateStaminaText();
        if (currentStamina <= 0) EndPlayerTurn();
    }

    // Zavolá WeaponTargetingController po úspešnom útoku hráča
    public void OnPlayerUsedWeapon(GridPreset preset)
    {
        currentStamina = Mathf.Max(0, currentStamina - GetStaminaCost(preset));
        nextAttackFree = false;
        UpdateStaminaText();
        if (CheckCombatEnd()) return;

        // Zatiaľ sa ťah končí automaticky po minutí celej staminy
        if (currentStamina <= 0) EndPlayerTurn();
    }

    // Zavolá WeaponTargetingController po úspešnom použití consumable s tvarom (Bomba, Health Potion)
    // Na rozdiel od zbrane nevyužije mystický prášok, ale skontroluje koniec súboja (bomba môže zabiť posledného nepriateľa)
    public void OnPlayerUsedConsumable(int cost)
    {
        currentStamina = Mathf.Max(0, currentStamina - Mathf.Max(0, cost));
        UpdateStaminaText();
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
        if (PlayerStats.Instance != null) maxStamina = PlayerStats.Instance.maxStamina;
        currentStamina = maxStamina;
        UpdateStaminaText();
        onPlayerTurnStart?.Invoke();
    }

    // Vytvorí text staminy iba raz na začiatku (nie v UpdateStaminaText - to sa volá aj pri zatváraní scény
    // cez OnDisable tlačidiel zbraní a vytváralo by nové objekty, ktoré Unity hlási ako neupratané)
    private void CreateStaminaTextIfMissing()
    {
        if (staminaText != null) return;

        var go = new GameObject("StaminaText");
        staminaText = go.AddComponent<TextMeshPro>();
        staminaText.alignment = TextAlignmentOptions.Center;
        staminaText.fontSize = 4;
        staminaText.color = Color.white;
        staminaText.sortingOrder = 100;
    }

    private void UpdateStaminaText()
    {
        if (playerGrid == null || staminaText == null) return;

        // Grid hráča sa môže zväčšiť (upgrady), preto pozíciu prepočítame vždy
        Vector3 pos = playerGrid.GetTopCenterWorld(staminaTextOffset);
        pos.z = 0f;
        staminaText.transform.position = pos;
        staminaText.text = $"Stamina {currentStamina}/{maxStamina}";
        if (previewStaminaCost > 0) staminaText.text += $" <color=#FF5555>-{previewStaminaCost}</color>";
    }

    // Zobrazí cenu zbrane pri stamine (napr. pri hoveri nad zbraňou)
    public void ShowStaminaCost(int cost)
    {
        previewStaminaCost = cost;
        UpdateStaminaText();
    }

    public void HideStaminaCost()
    {
        previewStaminaCost = 0;
        UpdateStaminaText();
    }

    private IEnumerator EnemyTurnRoutine()
    {
        State = TurnState.EnemyTurn;

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

            playerGrid.ShowEnemyAttackPreview(target, attack.pattern);
            yield return new WaitForSeconds(attackPreviewTime);

            playerGrid.ApplyEnemyAttack(target, attack.pattern);
            Debug.Log($"{enemy.enemyName} útočí: {attack.pattern.name}");

            // Po útoku vrátime obrys nepriateľa späť na bielo a skryjeme červenú guličku
            enemyGrid.SetAttackingEnemy(-1);
            RefreshUpperPartEnemies(-1);

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
            onDefeat?.Invoke();
            return true;
        }

        if (!enemyGrid.HasAnyAliveBlock())
        {
            State = TurnState.Victory;
            Debug.Log("Výhra - všetci nepriatelia sú porazení.");
            OnVictory();
            onVictory?.Invoke();
            return true;
        }

        return false;
    }

    // Po výhre si hráč v popupe vyberie odmenu: vyliečenie na max HP ALEBO zlato za nepriateľov
    private void OnVictory()
    {
        pendingGoldReward = 0;
        foreach (var enemy in enemyGrid.Enemies)
        {
            if (enemy != null) pendingGoldReward += enemy.goldReward;
        }

        if (PlayerStats.Instance != null) PlayerStats.Instance.encountersDefeated++;
        rewardChosen = false;

        if (victoryReward == null) victoryReward = FindAnyObjectByType<VictoryRewardUI>(FindObjectsInactive.Include); // Nájde ho aj vo vypnutom Canvase
        if (victoryReward != null)
        {
            victoryReward.Show(pendingGoldReward);
        }
        else
        {
            // Bez popupu v scéne (napr. testovanie) sa zachová pôvodné správanie: zlato automaticky, hráč ostane v scéne
            Debug.LogWarning("V scéne chýba VictoryRewardUI - zlato sa pridá automaticky.");
            SavePlayerHp();
            if (PlayerStats.Instance != null) PlayerStats.Instance.AddMoney(pendingGoldReward);
            rewardChosen = true;
        }
    }

    // Tlačidlo "Vyliečiť" v popupe - všetky políčka HP gridu sa vrátia na max, zlato sa nepridá
    public void ChooseHealReward()
    {
        if (State != TurnState.Victory || rewardChosen) return;
        rewardChosen = true;

        if (PlayerStats.Instance != null) PlayerStats.Instance.playerCurrentHp = null; // null = plné HP
        ReturnToMap();
    }

    // Tlačidlo "Zlato" v popupe - pridá zlato za nepriateľov, stratené HP sa prenesie ďalej
    public void ChooseGoldReward()
    {
        if (State != TurnState.Victory || rewardChosen) return;
        rewardChosen = true;

        SavePlayerHp();
        if (PlayerStats.Instance != null) PlayerStats.Instance.AddMoney(pendingGoldReward);
        ReturnToMap();
    }

    private void ReturnToMap()
    {
        if (LoadingScene.Instance != null) LoadingScene.Instance.SwitchToScene(mapSceneBuildIndex);
        else UnityEngine.SceneManagement.SceneManager.LoadScene(mapSceneBuildIndex);
    }

    // Stratené HP sa prenesie do ďalšieho encountru
    private void SavePlayerHp()
    {
        if (PlayerStats.Instance == null || playerGrid == null) return;
        PlayerStats.Instance.playerCurrentHp = playerGrid.GetHpSnapshot(PlayerStats.Instance.playerHealthPreset);
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
