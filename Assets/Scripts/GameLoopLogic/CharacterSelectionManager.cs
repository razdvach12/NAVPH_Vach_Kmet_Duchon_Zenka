using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterSelectionManager : MonoBehaviour
{
    [Header("Aktuálne zvolená postava v menu")]
    public CharacterData selectedCharacter;

    [Header("Mapa")]
    [Tooltip("Z tohto presetu sa po stlačení Start vygeneruje mapa pre celú hru")]
    public MapPreset mapPreset;

    private class WeaponHoverEntry
    {
        public SpriteRenderer iconRenderer;
        public RectTransform parentRect;
        public Vector2 localCenter;
        public GridManager weaponGrid;
    }

    private readonly List<WeaponHoverEntry> weaponHoverEntries = new List<WeaponHoverEntry>();

    [Header("Text so štartovým zlatom a staminou (pod HP gridom postavy)")]
    [Tooltip("Font textu (prázdne = predvolený TextMeshPro font)")]
    [SerializeField] private TMP_FontAsset statsFont;
    [SerializeField] private float statsFontSize = 36f;
    [SerializeField] private Color statsColor = Color.white;
    [Tooltip("Vzdialenosť textu pod spodným okrajom HP gridu (v jednotkách Canvasu)")]
    [SerializeField] private float statsOffsetBelowGrid = 40f;

    private IEnumerator Start()
    {
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayMusic("CharacterSelBackground");
        }

        SetupWeaponHoverPreviews();

        // Počkáme jeden frame, aby si HP gridy postáv stihli vygenerovať políčka (kvôli ich výške)
        yield return null;
        SetupCharacterStatsTexts();
    }

    // Do každého panelu postavy (IceDescription, LavaDescription, ...) pridá text so štartovým zlatom a staminou
    private void SetupCharacterStatsTexts()
    {
        CharacterData[] allCharacters = Resources.FindObjectsOfTypeAll<CharacterData>();
        RectTransform[] allRects = FindObjectsByType<RectTransform>(FindObjectsInactive.Include);

        foreach (RectTransform panel in allRects)
        {
            if (panel == null || !panel.name.EndsWith("Description") || panel.name == "Description") continue;

            string prefix = panel.name.Substring(0, panel.name.Length - "Description".Length);
            CharacterData character = FindCharacterByPrefix(prefix, allCharacters);
            if (character == null) continue;

            GridManager hpGrid = null;
            foreach (GridManager gm in panel.GetComponentsInChildren<GridManager>(true))
            {
                if (gm != null && gm.gameObject.name != "WeaponGrid")
                {
                    hpGrid = gm;
                    break;
                }
            }
            if (hpGrid == null) continue;

            // Pod stred HP gridu
            Vector3 gridCenterLocal = panel.InverseTransformPoint(hpGrid.transform.position);
            float gridBottomY = GetGridBottomLocalY(panel, hpGrid);
            CreateStatsText(panel, "StartingStatsText",
                $"Gold: {character.startingGold}\nStamina: {character.startingStamina}",
                new Vector3(gridCenterLocal.x, gridBottomY - statsOffsetBelowGrid, gridCenterLocal.z));
        }
    }

    // Spodný okraj gridu v súradniciach panelu (grid je vycentrovaný na svojej pozícii, spodok = stred - (vrch - stred))
    private static float GetGridBottomLocalY(Transform panel, GridManager grid)
    {
        Vector3 centerLocal = panel.InverseTransformPoint(grid.transform.position);
        Vector3 topLocal = panel.InverseTransformPoint(grid.GetTopCenterWorld());
        return centerLocal.y - (topLocal.y - centerLocal.y);
    }

    // Vytvorí text v paneli postavy; localTopCenter = stred horného okraja textu v súradniciach panelu
    private void CreateStatsText(Transform panel, string objectName, string content, Vector3 localTopCenter)
    {
        var textGo = new GameObject(objectName, typeof(RectTransform));
        textGo.transform.SetParent(panel, false);

        // 3D TextMeshPro (nie UI) - aby sa správne vykreslil nad spritmi postavy a pozadia
        var text = textGo.AddComponent<TextMeshPro>();
        if (statsFont != null) text.font = statsFont;
        text.text = content;
        text.alignment = TextAlignmentOptions.Top;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.color = statsColor;
        text.sortingOrder = 10;

        // V 3D TextMeshPro je fontSize 10 ≈ 1 jednotka, preto zväčšíme objekt 10×, aby fontSize zodpovedal jednotkám Canvasu
        text.fontSize = statsFontSize;
        textGo.transform.localScale = Vector3.one * 10f;
        text.rectTransform.sizeDelta = new Vector2(80f, 20f);
        text.rectTransform.pivot = new Vector2(0.5f, 1f);

        textGo.transform.localPosition = localTopCenter;
    }

    private static CharacterData FindCharacterByPrefix(string prefix, CharacterData[] allCharacters)
    {
        foreach (CharacterData ch in allCharacters)
        {
            if (ch == null) continue;
            if (ch.name.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase)
                || (ch.characterName != null && ch.characterName.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase)))
            {
                return ch;
            }
        }
        return null;
    }

    private void Update()
    {
        if (Mouse.current == null || weaponHoverEntries.Count == 0) return;

        Vector2 screenPos = Mouse.current.position.ReadValue();
        Camera cam = Camera.main;

        foreach (WeaponHoverEntry entry in weaponHoverEntries)
        {
            if (entry.iconRenderer == null || entry.weaponGrid == null || entry.parentRect == null) continue;

            if (!entry.parentRect.gameObject.activeInHierarchy)
            {
                if (!entry.iconRenderer.enabled) entry.iconRenderer.enabled = true;
                if (entry.weaponGrid.gameObject.activeSelf) entry.weaponGrid.gameObject.SetActive(false);
                continue;
            }

            bool isHovered = false;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(entry.parentRect, screenPos, cam, out Vector2 localPoint))
            {
                const float halfSize = 125f;
                isHovered = Mathf.Abs(localPoint.x - entry.localCenter.x) <= halfSize
                         && Mathf.Abs(localPoint.y - entry.localCenter.y) <= halfSize;
            }

            if (entry.iconRenderer.enabled == isHovered)
            {
                entry.iconRenderer.enabled = !isHovered;
            }
            if (entry.weaponGrid.gameObject.activeSelf != isHovered)
            {
                entry.weaponGrid.gameObject.SetActive(isHovered);
            }
        }
    }

    private void SetupWeaponHoverPreviews()
    {
        weaponHoverEntries.Clear();

        GridPreset[] allPresets = Resources.FindObjectsOfTypeAll<GridPreset>();
        CharacterData[] allCharacters = Resources.FindObjectsOfTypeAll<CharacterData>();
        SpriteRenderer[] allRenderers = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include);

        foreach (SpriteRenderer sr in allRenderers)
        {
            if (sr == null || sr.transform.parent == null) continue;
            if (!sr.name.EndsWith("Weapon1") && !sr.name.EndsWith("Weapon2")) continue;

            RectTransform parentRect = sr.transform.parent as RectTransform;
            if (parentRect == null) continue;

            GridManager hpGrid = null;
            foreach (GridManager gm in sr.transform.parent.GetComponentsInChildren<GridManager>(true))
            {
                if (gm != null && gm.gameObject.name != "WeaponGrid")
                {
                    hpGrid = gm;
                    break;
                }
            }
            if (hpGrid == null) continue;

            GridPreset weaponPreset = FindWeaponPresetForRenderer(sr, allPresets, allCharacters);
            if (weaponPreset == null) continue;

            GameObject gridObj = new GameObject("WeaponGrid");
            gridObj.transform.SetParent(sr.transform.parent, false);
            gridObj.transform.localPosition = new Vector3(
                sr.transform.localPosition.x,
                hpGrid.transform.localPosition.y,
                hpGrid.transform.localPosition.z
            );
            gridObj.transform.localRotation = Quaternion.identity;

            float baseScale = hpGrid.transform.localScale.x > 0f ? hpGrid.transform.localScale.x : 90f;
            int maxDim = Mathf.Max(weaponPreset.width, weaponPreset.height, 4);
            float gridScale = baseScale * (4f / maxDim);
            gridObj.transform.localScale = new Vector3(gridScale, gridScale, gridScale);

            GridManager weaponGrid = gridObj.AddComponent<GridManager>();
            weaponGrid.isPlayerGrid = true;
            weaponGrid.CopyPrefabsFrom(hpGrid);
            weaponGrid.SetShowMaxHpOutline(false);
            weaponGrid.SetSinglePreset(weaponPreset);
            weaponGrid.GenerateSingleGrid(weaponPreset);

            // Cena zbrane v stamine - pod ikonou aj pod náhľadom gridu (podľa toho, čo siaha nižšie)
            Transform panel = sr.transform.parent;
            float bottomY = GetGridBottomLocalY(panel, weaponGrid);
            if (sr.sprite != null)
            {
                float iconBottomY = panel.InverseTransformPoint(sr.transform.TransformPoint(new Vector3(0f, sr.sprite.bounds.min.y, 0f))).y;
                bottomY = Mathf.Min(bottomY, iconBottomY);
            }
            Vector3 iconLocal = sr.transform.localPosition;
            CreateStatsText(panel, "WeaponStaminaText", $"Stamina: {weaponPreset.staminaCost}",
                new Vector3(iconLocal.x, bottomY - statsOffsetBelowGrid, hpGrid.transform.localPosition.z));

            gridObj.SetActive(false);

            weaponHoverEntries.Add(new WeaponHoverEntry
            {
                iconRenderer = sr,
                parentRect = parentRect,
                localCenter = new Vector2(sr.transform.localPosition.x, sr.transform.localPosition.y),
                weaponGrid = weaponGrid
            });
        }
    }

    private static GridPreset FindWeaponPresetForRenderer(SpriteRenderer sr, GridPreset[] allPresets, CharacterData[] allCharacters)
    {
        if (sr.sprite != null)
        {
            foreach (CharacterData ch in allCharacters)
            {
                if (ch == null || ch.startingWeapons == null) continue;
                foreach (GridPreset wp in ch.startingWeapons)
                {
                    if (wp != null && wp.weaponIcon == sr.sprite) return wp;
                }
            }

            foreach (GridPreset p in allPresets)
            {
                if (p != null && p.weaponIcon == sr.sprite) return p;
            }
        }

        int weaponIndex = sr.name.EndsWith("Weapon1") ? 0 : 1;
        string prefix = sr.name.Replace("Weapon1", "").Replace("Weapon2", "");
        foreach (CharacterData ch in allCharacters)
        {
            if (ch == null || ch.startingWeapons == null || weaponIndex >= ch.startingWeapons.Count) continue;
            if (ch.name.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase)
                || (ch.characterName != null && ch.characterName.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase)))
            {
                return ch.startingWeapons[weaponIndex];
            }
        }

        return null;
    }

    // Univerzálna metóda pre hociktorú postavu
    public void SelectCharacter(CharacterData character)
    {
        selectedCharacter = character;
        Debug.Log($"Zvolený hrdina: {character.characterName}");
    }


    public void OnClickStartButton()
    {
        if (selectedCharacter == null)
        {
            Debug.LogWarning("Nie je vybraná žiadna postava!");
            return;
        }

        PlayerStats.EnsureInstance().InitializeCharacter(selectedCharacter);

        if (LoadingScene.IsBusy) return; // Dvojklik počas loadingu by vygeneroval mapu znova

        // Nová hra = nová mapa, ostane rovnaká až do konca hry
        MapRunState.StartNewRun(mapPreset);

        LoadingScene.Load(SceneIds.Map);
    }

    public void OnClickBackToMenuButton()
    {
        MapRunState.Clear();
        LoadingScene.Load(SceneIds.Menu);
    }
}
