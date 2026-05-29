using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class PlayerAttackVisuals : MonoBehaviour
{
    // Base player sprite renderer that normally shows the run animation.
    public SpriteRenderer spriteRenderer;
    // Animator that controls the player's normal movement animation.
    public Animator animator;
    // Animation state to return to after the attack frames finish.
    public string runStateName = "Stickman_Run";
    // Y direction above this value uses the upward kick frames.
    public float upwardShotThreshold = 0.15f;
    // Time each attack sprite frame stays on screen.
    public float frameDuration = 0.06f;
    // Frame sequence used for straight shots.
    public Sprite[] straightPunchFrames;
    // Frame sequence used for upward shots.
    public Sprite[] upwardKickFrames;

    // Currently running frame playback coroutine.
    Coroutine activeRoutine;
    // Temporary overlay renderer used to show attack frames.
    SpriteRenderer overlayRenderer;
    // Remembers whether the base sprite was visible before an attack started.
    bool baseSpriteWasEnabled = true;

    void Reset()
    {
        // Auto-fill references when the component is first added in Unity.
        PopulateDefaultsIfMissing();
    }

    void Awake()
    {
        // Use components on the same GameObject if references were not assigned.
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (animator == null)
            animator = GetComponent<Animator>();

        // Prepare the overlay renderer before any attack animation plays.
        EnsureOverlayRenderer();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Keep default sprite references filled while editing in the Inspector.
        PopulateDefaultsIfMissing();
        EditorUtility.SetDirty(this);
    }
#endif

    [ContextMenu("Auto Assign Attack Visuals")]
    void AutoAssignAttackVisuals()
    {
        // Context menu helper for manually refreshing default attack frames.
        PopulateDefaultsIfMissing();

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    public void PlayShoot(Vector2 direction)
    {
        // Pick the animation set based on whether the player is aiming upward.
        Sprite[] frames = direction.y > upwardShotThreshold
            ? upwardKickFrames
            : straightPunchFrames;

        // Do nothing if required sprites or renderers are missing.
        if (frames == null || frames.Length == 0 || spriteRenderer == null)
            return;

        // Create the overlay on demand if it was not ready yet.
        EnsureOverlayRenderer();
        if (overlayRenderer == null)
            return;

        // Stop the previous attack animation before starting a new one.
        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        activeRoutine = StartCoroutine(PlayFrames(frames));
    }

    IEnumerator PlayFrames(Sprite[] frames)
    {
        // Clamp frame time so the coroutine always yields a real duration.
        float duration = Mathf.Max(0.01f, frameDuration);
        WaitForSeconds wait = new WaitForSeconds(duration);
        // Hide the base sprite so only the attack frame is visible.
        baseSpriteWasEnabled = spriteRenderer.enabled;
        spriteRenderer.enabled = false;
        overlayRenderer.enabled = true;
        overlayRenderer.flipX = spriteRenderer.flipX;

        // Display each attack sprite in order.
        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] != null)
                overlayRenderer.sprite = frames[i];

            yield return wait;
        }

        // Return to the normal run animation after the attack finishes.
        RestoreRunState();
        activeRoutine = null;
    }

    void OnDisable()
    {
        // Clean up visual state if the player or component is disabled mid-attack.
        activeRoutine = null;
        RestoreRunState();
    }

    void RestoreRunState()
    {
        // Re-enable normal animation and hide the temporary overlay sprite.
        SetAnimatorEnabled(true);
        HideOverlay();

        // Restart the configured run state so the animation resumes cleanly.
        if (animator != null && !string.IsNullOrWhiteSpace(runStateName))
        {
            animator.Rebind();
            animator.Play(runStateName, 0, 0f);
            animator.Update(0f);
        }
    }

    void SetAnimatorEnabled(bool enabled)
    {
        // Safely toggle the Animator if one exists.
        if (animator != null)
            animator.enabled = enabled;
    }

    void PopulateDefaultsIfMissing()
    {
        // Fill missing component references from the current GameObject.
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (animator == null)
            animator = GetComponent<Animator>();

#if UNITY_EDITOR
        // In the editor, load default straight punch sprites if none were assigned.
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

        // In the editor, load default upward kick sprites if none were assigned.
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
        // Only create the overlay when a base sprite renderer exists.
        if (overlayRenderer != null || spriteRenderer == null)
            return;

        // Reuse the named child if it already exists.
        Transform child = transform.Find("AttackVisualOverlay");
        if (child == null)
        {
            // Create a child object to draw attack frames over the base player.
            GameObject overlayObject = new GameObject("AttackVisualOverlay");
            overlayObject.transform.SetParent(transform, false);
            child = overlayObject.transform;
        }

        // Add a SpriteRenderer to the overlay child if needed.
        overlayRenderer = child.GetComponent<SpriteRenderer>();
        if (overlayRenderer == null)
            overlayRenderer = child.gameObject.AddComponent<SpriteRenderer>();

        // Copy rendering settings so the overlay lines up with the base sprite.
        overlayRenderer.enabled = false;
        overlayRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        overlayRenderer.sortingOrder = spriteRenderer.sortingOrder;
        overlayRenderer.color = spriteRenderer.color;
        overlayRenderer.sharedMaterial = spriteRenderer.sharedMaterial;
        overlayRenderer.maskInteraction = spriteRenderer.maskInteraction;
    }

    void HideOverlay()
    {
        // Restore the base sprite to whatever visibility it had before attacking.
        if (spriteRenderer != null)
            spriteRenderer.enabled = baseSpriteWasEnabled;

        // Clear and hide the overlay renderer.
        if (overlayRenderer != null)
        {
            overlayRenderer.sprite = null;
            overlayRenderer.enabled = false;
        }
    }

#if UNITY_EDITOR
    static Sprite LoadSprite(string assetPath)
    {
        // Try to load a sprite directly from the asset path first.
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite != null)
            return sprite;

        // Some sprite sheets store sprites as sub-assets, so check those too.
        Object[] assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite nestedSprite)
                return nestedSprite;
        }

        // Returning null leaves the frame slot empty if the asset was not found.
        return null;
    }
#endif
}
