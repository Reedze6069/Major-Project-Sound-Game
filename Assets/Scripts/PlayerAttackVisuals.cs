using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class PlayerAttackVisuals : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public Animator animator;
    public string runStateName = "Stickman_Run";
    public float upwardShotThreshold = 0.15f;
    public float frameDuration = 0.06f;
    public Sprite[] straightPunchFrames;
    public Sprite[] upwardKickFrames;

    Coroutine activeRoutine;
    SpriteRenderer overlayRenderer;
    bool baseSpriteWasEnabled = true;

    void Reset()
    {
        PopulateDefaultsIfMissing();
    }

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (animator == null)
            animator = GetComponent<Animator>();

        EnsureOverlayRenderer();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        PopulateDefaultsIfMissing();
        EditorUtility.SetDirty(this);
    }
#endif

    [ContextMenu("Auto Assign Attack Visuals")]
    void AutoAssignAttackVisuals()
    {
        PopulateDefaultsIfMissing();

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    public void PlayShoot(Vector2 direction)
    {
        Sprite[] frames = direction.y > upwardShotThreshold
            ? upwardKickFrames
            : straightPunchFrames;

        if (frames == null || frames.Length == 0 || spriteRenderer == null)
            return;

        EnsureOverlayRenderer();
        if (overlayRenderer == null)
            return;

        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        activeRoutine = StartCoroutine(PlayFrames(frames));
    }

    IEnumerator PlayFrames(Sprite[] frames)
    {
        float duration = Mathf.Max(0.01f, frameDuration);
        WaitForSeconds wait = new WaitForSeconds(duration);
        baseSpriteWasEnabled = spriteRenderer.enabled;
        spriteRenderer.enabled = false;
        overlayRenderer.enabled = true;
        overlayRenderer.flipX = spriteRenderer.flipX;

        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] != null)
                overlayRenderer.sprite = frames[i];

            yield return wait;
        }

        RestoreRunState();
        activeRoutine = null;
    }

    void OnDisable()
    {
        activeRoutine = null;
        RestoreRunState();
    }

    void RestoreRunState()
    {
        SetAnimatorEnabled(true);
        HideOverlay();

        if (animator != null && !string.IsNullOrWhiteSpace(runStateName))
        {
            animator.Rebind();
            animator.Play(runStateName, 0, 0f);
            animator.Update(0f);
        }
    }

    void SetAnimatorEnabled(bool enabled)
    {
        if (animator != null)
            animator.enabled = enabled;
    }

    void PopulateDefaultsIfMissing()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (animator == null)
            animator = GetComponent<Animator>();

#if UNITY_EDITOR
        if (straightPunchFrames == null || straightPunchFrames.Length == 0)
        {
            straightPunchFrames = new[]
            {
                LoadSprite("Assets/Prefabs/Sprites/fighter_combo_0064.png"),
                LoadSprite("Assets/Prefabs/Sprites/fighter_combo_0065.png"),
                LoadSprite("Assets/Prefabs/Sprites/fighter_combo_0066.png"),
                LoadSprite("Assets/Prefabs/Sprites/fighter_combo_0067.png"),
                LoadSprite("Assets/Prefabs/Sprites/fighter_combo_0068.png")
            };
        }

        if (upwardKickFrames == null || upwardKickFrames.Length == 0)
        {
            upwardKickFrames = new[]
            {
                LoadSprite("Assets/Prefabs/Sprites/fighter_air_attack_0062.png"),
                LoadSprite("Assets/Prefabs/Sprites/fighter_air_attack_0063.png")
            };
        }
#endif
    }

    void EnsureOverlayRenderer()
    {
        if (overlayRenderer != null || spriteRenderer == null)
            return;

        Transform child = transform.Find("AttackVisualOverlay");
        if (child == null)
        {
            GameObject overlayObject = new GameObject("AttackVisualOverlay");
            overlayObject.transform.SetParent(transform, false);
            child = overlayObject.transform;
        }

        overlayRenderer = child.GetComponent<SpriteRenderer>();
        if (overlayRenderer == null)
            overlayRenderer = child.gameObject.AddComponent<SpriteRenderer>();

        overlayRenderer.enabled = false;
        overlayRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        overlayRenderer.sortingOrder = spriteRenderer.sortingOrder;
        overlayRenderer.color = spriteRenderer.color;
        overlayRenderer.sharedMaterial = spriteRenderer.sharedMaterial;
        overlayRenderer.maskInteraction = spriteRenderer.maskInteraction;
    }

    void HideOverlay()
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = baseSpriteWasEnabled;

        if (overlayRenderer != null)
        {
            overlayRenderer.sprite = null;
            overlayRenderer.enabled = false;
        }
    }

#if UNITY_EDITOR
    static Sprite LoadSprite(string assetPath)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite != null)
            return sprite;

        Object[] assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite nestedSprite)
                return nestedSprite;
        }

        return null;
    }
#endif
}
