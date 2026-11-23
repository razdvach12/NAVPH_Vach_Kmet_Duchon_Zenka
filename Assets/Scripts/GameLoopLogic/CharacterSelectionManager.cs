using System.Collections.Generic;
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

    private void Start()
    {
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayMusic("CharacterSelBackground");
        }

        SetupWeaponHoverPreviews();
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
