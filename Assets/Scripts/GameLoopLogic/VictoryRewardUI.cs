using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class VictoryRewardUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Panel popupu (VictoryPopup) - na začiatku vypnutý, zapne sa pri výhre")]
    [SerializeField] private GameObject popup;

    [Tooltip("Voliteľné - text, do ktorého sa vypíše, koľko zlata hráč získal")]
    [SerializeField] private TextMeshProUGUI goldText;

    [Tooltip("Ak nie je priradený, nájde sa automaticky")]
    [SerializeField] private TurnManager turnManager;

    [Header("Odmena po boji (hráč si vyberie jednu)")]
    [Tooltip("Koľko Max HP políčok pridá odmena Max HP")]
    [SerializeField] private int maxHpCellsReward = 3;
    [SerializeField] private Vector2 rewardRowPosition = new Vector2(0f, -55f); // Stred riadku tlačidiel (od stredu obrazovky)
    [SerializeField] private Vector2 rewardButtonSize = new Vector2(230f, 60f);
    [SerializeField] private float rewardButtonSpacing = 20f;
    [Tooltip("Font tlačidiel odmien (prázdne = font textu zlata)")]
    [SerializeField] private TMP_FontAsset rewardFont;
    [SerializeField] private float rewardFontSize = 22f;

    private Canvas canvas;
    private RectTransform rewardRow;
    private bool rewardClaimed;
    private bool isTargeting;     // Hráč práve umiestňuje odmenu na grid (popup je skrytý)
    private int maxHpCellsLeft;   // Zvyšné Max HP políčka (ak hráč zruší umiestňovanie v polovici)

    private void Awake()
    {
        if (turnManager == null) turnManager = FindAnyObjectByType<TurnManager>();
        if (popup != null) popup.SetActive(false);
        canvas = GetComponent<Canvas>();
    }


    public void Show(int goldReward)
    {
        gameObject.SetActive(true);

        if (goldText != null) goldText.text = $"+{goldReward} gold pieces";
        if (popup != null) popup.SetActive(true);

        rewardClaimed = false;
        isTargeting = false;
        maxHpCellsLeft = maxHpCellsReward;
        RewardTargetingController.EnsureInstance();
        ShowRewardChoices();
    }

    private void Update()
    {
        if (!isTargeting) return;

        // Hráč zrušil umiestňovanie (Escape / pravý klik) - vrátime popup s výberom odmien
        RewardTargetingController targeting = RewardTargetingController.Instance;
        if (targeting == null || !targeting.IsTargeting)
        {
            isTargeting = false;
            SetPopupVisible(true);
            if (maxHpCellsLeft < maxHpCellsReward) ShowMaxHpOnly(); // Časť Max HP už je pridaná - ostáva iba dokončiť ju
            else ShowRewardChoices();
        }
    }

    // --- VÝBER ODMENY ---

    private void ShowRewardChoices()
    {
        BuildRow(new List<(string, Color, Action)>
        {
            ($"+{maxHpCellsReward} Max HP", new Color(0.8f, 0.25f, 0.25f), StartMaxHpReward),
            ("Element cell", new Color(0.35f, 0.55f, 0.85f), ShowElementChoices),
            ("+1 Weapon cell", new Color(0.75f, 0.6f, 0.3f), StartWeaponReward)
        });
    }

    // Druhý krok odmeny "Element" - hráč si vyberie živel
    private void ShowElementChoices()
    {
        BuildRow(new List<(string, Color, Action)>
        {
            ("Lava", new Color(0.9f, 0.35f, 0.1f), () => StartElementReward(BlockElement.Lava)),
            ("Ice", new Color(0.4f, 0.75f, 0.95f), () => StartElementReward(BlockElement.Ice)),
            ("Nature", new Color(0.3f, 0.7f, 0.3f), () => StartElementReward(BlockElement.Nature)),
            ("Rock", new Color(0.55f, 0.5f, 0.45f), () => StartElementReward(BlockElement.Rock)),
            ("Back", new Color(0.4f, 0.4f, 0.4f), ShowRewardChoices)
        });
    }

    private void ShowMaxHpOnly()
    {
        BuildRow(new List<(string, Color, Action)>
        {
            ($"+{maxHpCellsLeft} Max HP", new Color(0.8f, 0.25f, 0.25f), StartMaxHpReward)
        });
    }

    private void StartMaxHpReward()
    {
        BeginTargeting();
        RewardTargetingController.Instance.StartAddMaxHpTargeting(OnMaxHpCellPlaced);
    }

    private void OnMaxHpCellPlaced()
    {
        maxHpCellsLeft--;
        if (maxHpCellsLeft > 0)
        {
            // Ďalšie políčko - targeting pokračuje, popup ostáva skrytý
            RewardTargetingController.Instance.StartAddMaxHpTargeting(OnMaxHpCellPlaced);
            return;
        }
        FinishReward();
    }

    private void StartElementReward(BlockElement element)
    {
        BeginTargeting();
        RewardTargetingController.Instance.StartChangeElementTargeting(element, FinishReward);
    }

    private void StartWeaponReward()
    {
        BeginTargeting();
        RewardTargetingController.Instance.StartAddWeaponCellTargeting(FinishReward);
    }

    // Skryje popup, aby hráč videl svoj grid a zbrane
    private void BeginTargeting()
    {
        isTargeting = true;
        SetPopupVisible(false);
    }

    private void FinishReward()
    {
        isTargeting = false;
        rewardClaimed = true;
        if (rewardRow != null) rewardRow.gameObject.SetActive(false);
        SetPopupVisible(true);
    }

    private void SetPopupVisible(bool visible)
    {
        if (canvas != null) canvas.enabled = visible;
        else if (popup != null) popup.SetActive(visible);
    }

    // --- TLAČIDLÁ (vytvárajú sa za behu, v scéne netreba nič pridávať) ---

    private void BuildRow(List<(string label, Color color, Action onClick)> buttons)
    {
        if (rewardClaimed) return;

        if (rewardRow == null)
        {
            var rowGo = new GameObject("RewardChoices", typeof(RectTransform));
            rewardRow = rowGo.GetComponent<RectTransform>();
            rewardRow.SetParent(transform, false);
            rewardRow.anchorMin = rewardRow.anchorMax = new Vector2(0.5f, 0.5f);
            rewardRow.pivot = new Vector2(0.5f, 0.5f);
        }

        rewardRow.gameObject.SetActive(true);
        rewardRow.anchoredPosition = rewardRowPosition;
        for (int i = rewardRow.childCount - 1; i >= 0; i--) Destroy(rewardRow.GetChild(i).gameObject);

        float totalWidth = buttons.Count * rewardButtonSize.x + (buttons.Count - 1) * rewardButtonSpacing;
        float startX = -totalWidth * 0.5f + rewardButtonSize.x * 0.5f;

        for (int i = 0; i < buttons.Count; i++)
        {
            var (label, color, onClick) = buttons[i];
            CreateButton(label, color, onClick, new Vector2(startX + i * (rewardButtonSize.x + rewardButtonSpacing), 0f));
        }
    }

    private void CreateButton(string label, Color color, Action onClick, Vector2 position)
    {
        var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(rewardRow, false);
        rt.sizeDelta = rewardButtonSize;
        rt.anchoredPosition = position;

        go.GetComponent<Image>().color = color;
        go.GetComponent<Button>().onClick.AddListener(() => onClick());

        var textGo = new GameObject("Text", typeof(RectTransform));
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.SetParent(rt, false);
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = textRt.offsetMax = Vector2.zero;

        var text = textGo.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset font = rewardFont != null ? rewardFont : (goldText != null ? goldText.font : null);
        if (font != null) text.font = font;
        text.text = label;
        text.fontSize = rewardFontSize;
        text.enableAutoSizing = true;
        text.fontSizeMin = 10f;
        text.fontSizeMax = rewardFontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
    }

    // Napojiť na On Click tlačidla "Continue" - vráti hráča na mapu
    public void OnClickContinue()
    {
        if (isTargeting) return;
        if (RewardTargetingController.Instance != null) RewardTargetingController.Instance.CancelTargeting();
        if (ResolveTurnManager()) turnManager.ContinueAfterVictory();
    }

    private bool ResolveTurnManager()
    {
        if (turnManager == null) turnManager = FindAnyObjectByType<TurnManager>();
        return turnManager != null;
    }
}
