using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Súhrn runu (FinalScene): prvá stránka (You won! + PROCEED), po kliknutí stránka so štatistikami,
// HP gridom a zbraňami hráča. Rozloženie je prevzaté z DeathSceneNOTDONE (AllStatsSummary).
public class RunSummaryUI : MonoBehaviour
{
    [Tooltip("Stránka so štatistikami (AllStatsSummary) - na začiatku sa skryje")]
    [SerializeField] private GameObject statsPage;

    [Tooltip("Názvy objektov pod Canvasom, ktoré ostávajú zobrazené na oboch stránkach")]
    [SerializeField] private List<string> alwaysVisible = new List<string> { "Background" };

    [Tooltip("HP grid v náhľade bol navrhnutý pre 4x4 - väčší grid sa zmenší, aby sa zmestil")]
    [SerializeField] private int designedGridSize = 4;

    private Transform canvasRoot;

    private void Awake()
    {
        if (statsPage == null)
        {
            Debug.LogWarning("RunSummaryUI: nie je priradená stránka so štatistikami.");
            enabled = false;
            return;
        }

        canvasRoot = statsPage.transform.parent;
        statsPage.SetActive(false);

        // Na prvej stránke nahradí tlačidlo MENU tlačidlo PROCEED (MENU je až pri štatistikách)
        Transform firstPageMenu = canvasRoot.Find("BackToMenuButton");
        if (firstPageMenu != null) firstPageMenu.gameObject.SetActive(false);

        HookButton(canvasRoot.Find("ProceedToStatsButton"), ShowStats);
        HookButton(statsPage.transform.Find("BackToMenuButton"), BackToMenu);
    }

    public void ShowStats()
    {
        FillStats();

        foreach (Transform child in canvasRoot)
        {
            if (child.gameObject == statsPage || alwaysVisible.Contains(child.name)) continue;
            child.gameObject.SetActive(false);
        }
        statsPage.SetActive(true);
    }

    public void BackToMenu()
    {
        if (LoadingScene.IsBusy) return;
        LoadingScene.Load(SceneIds.Menu);
    }

    // Tlačidlá z návrhu majú v OnClick iba prepínanie objektov (SetActive) - vypneme ho a dáme vlastnú akciu
    private static void HookButton(Transform buttonRoot, UnityAction action)
    {
        if (buttonRoot == null) return;
        Button button = buttonRoot.GetComponentInChildren<Button>(true);
        if (button == null) return;

        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            if (button.onClick.GetPersistentMethodName(i) == "SetActive")
            {
                button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
            }
        }
        button.onClick.AddListener(action);
    }

    private bool filled;

    private void FillStats()
    {
        if (filled) return; // Grid sa nesmie zmenšiť dvakrát
        filled = true;

        PlayerStats stats = PlayerStats.Instance;

        TMP_Text statsText = FindText("Stats");
        if (statsText != null)
        {
            statsText.text =
                $"Encounters visited:\t{(stats != null ? stats.encountersVisited : 0)}\n" +
                $"Shops visited:\t{(stats != null ? stats.shopsVisited : 0)}\n" +
                $"Meadows used:\t{(stats != null ? stats.meadowsUsed : 0)}\n" +
                $"Unique potions:\t{(stats != null ? stats.UniquePotionsCount : 0)}\n" +
                $"Unique relics:\t{(stats != null ? stats.UniqueRelicsCount : 0)}\n" +
                $"Damage done:\t{(stats != null ? stats.damageDone : 0)}\n" +
                $"Damage taken:\t{(stats != null ? stats.damageTaken : 0)}";
        }

        FillWeapons(stats);
        FillAvatar(stats);
        FitHealthGrid(stats);
    }

    // Text v objekte s daným názvom (alebo v jeho deťoch)
    private TMP_Text FindText(string objectName)
    {
        foreach (TMP_Text text in statsPage.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.name == objectName || (text.transform.parent != null && text.transform.parent.name == objectName)) return text;
        }
        return null;
    }

    // Sloty Weapon1..WeaponN - ikony zbraní hráča, prázdne sloty sa skryjú
    private void FillWeapons(PlayerStats stats)
    {
        List<GridPreset> weapons = stats != null ? stats.unlockedWeapons : new List<GridPreset>();

        foreach (Transform t in statsPage.GetComponentsInChildren<Transform>(true))
        {
            if (!t.name.StartsWith("Weapon") || !int.TryParse(t.name.Substring("Weapon".Length), out int number)) continue;

            int index = number - 1;
            GridPreset weapon = index >= 0 && index < weapons.Count ? weapons[index] : null;
            Image image = t.GetComponent<Image>();

            bool show = weapon != null && weapon.weaponIcon != null && image != null;
            t.gameObject.SetActive(show);
            if (!show) continue;

            image.sprite = weapon.weaponIcon;
            image.preserveAspect = true;
            image.enabled = true;
        }
    }

    private void FillAvatar(PlayerStats stats)
    {
        if (stats == null || stats.playerSprite == null) return;

        Transform avatar = FindChild(statsPage.transform, "PlayerAvatar");
        Image image = avatar != null ? avatar.GetComponent<Image>() : null;
        if (image == null) return;

        image.sprite = stats.playerSprite;
        image.preserveAspect = true;
    }

    // HP grid sa vygeneruje sám z PlayerStats (GridManager.Start); väčší grid zmenšíme, aby sa zmestil do rámika
    private void FitHealthGrid(PlayerStats stats)
    {
        if (stats == null || stats.playerHealthPreset == null) return;

        Transform hpSection = FindChild(statsPage.transform, "HPSection");
        GridManager grid = (hpSection != null ? hpSection : statsPage.transform).GetComponentInChildren<GridManager>(true);
        if (grid == null) return;

        int maxDim = Mathf.Max(stats.playerHealthPreset.width, stats.playerHealthPreset.height);
        if (maxDim > designedGridSize)
        {
            grid.transform.localScale *= (float)designedGridSize / maxDim;
        }
    }

    private static Transform FindChild(Transform root, string childName)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == childName) return t;
        }
        return null;
    }
}
