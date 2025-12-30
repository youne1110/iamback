using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Playables;
using UnityEngine.VFX;

[System.Serializable]
public class PetStageVisuals
{
    public Sprite normalSprite;
    public Sprite happySprite;
    public Sprite angrySprite;
}

public class PetVisualController : MonoBehaviour
{
    [Header("主圖片顯示")]
    public Image petImage;

    [Header("各階段狀態圖片")]
    public PetStageVisuals stage1Visuals;
    public PetStageVisuals stage2Visuals;
    public PetStageVisuals stage3Visuals;

    [Header("")]
    public Sprite eatSprite;      // 吃飯咀嚼動畫
    public Sprite hitSprite;      // 被打
    public Sprite petSprite;      // 撫摸
    public Sprite chokeSprite;    // 噎住

    [Header("Visual Effects")]
    public Transform visualEffectsParent; // optional: drag the VisualEffects parent here
    public GameObject chokeVFXPrefab; // fullscreen choke prefab (UI or canvas child)
    private GameObject activeChokeVFX;

    // Pre-placed VFX instances (place under VisualEffects parent in scene and disable them)
    public GameObject eatVFX;  // shows when PlayEat is triggered
    public GameObject hitVFX;  // shows when PlayHit is triggered
    public GameObject petVFX;  // shows when PlayPet is triggered
    public GameObject chokeVFXInstance; // optional pre-placed choke VFX (preferred)

    [Header("Pet Action Image")]
    public Image petActionImage; // optional UI Image to show when petting
    public Sprite petActionSprite; // optional sprite to assign to the above image

    [Header("噎住 UI")]
    public GameObject chokeProgressBar;
    public Image chokeBar;
    public TMP_Text chokeText;

    [Header("UI 進度條")]
    public Image moodBar;
    public TMP_Text moodText;
    public Image feedBar;
    public TMP_Text feedText;
    public Image expBar;
    public TMP_Text expText;
    public TMP_Text levelText;

    private bool isChoking = false;

    private Coroutine currentAction = null;
    // map from prefab asset -> instantiated runtime instance
    private Dictionary<GameObject, GameObject> runtimeVFXInstances = new Dictionary<GameObject, GameObject>();

    void Awake()
    {
        // Ensure pre-placed VFX are hidden at start
        if (eatVFX != null) eatVFX.SetActive(false);
        if (hitVFX != null) hitVFX.SetActive(false);
        if (petVFX != null) petVFX.SetActive(false);
        if (chokeVFXInstance != null) chokeVFXInstance.SetActive(false);

        // Hide pet action image initially
        if (petActionImage != null)
        {
            petActionImage.gameObject.SetActive(false);
            if (petActionSprite != null)
                petActionImage.sprite = petActionSprite;
        }

        // Try to auto-find VisualEffects parent if not assigned
        if (visualEffectsParent == null)
        {
            GameObject found = GameObject.Find("VisualEffects");
            if (found != null) visualEffectsParent = found.transform;
        }
    }

    // ========================
    // 顯示靜態狀態圖（回到 Idle）
    // ========================
    public void ShowState(GameManager.PetStage stage, int mood)
    {
        if (petImage == null) return;

        PetStageVisuals visuals = GetVisualsForStage(stage);
        if (visuals == null) return;

        Sprite targetSprite = null;
        if (mood >= 70)
            targetSprite = visuals.happySprite;
        else if (mood >= 40)
            targetSprite = visuals.normalSprite;
        else
            targetSprite = visuals.angrySprite;

        if (targetSprite != null)
            petImage.sprite = targetSprite;

        // 播放階段切換音效
        if (AudioController.Instance != null)
            AudioController.Instance.PlayStateChange();
    }

    PetStageVisuals GetVisualsForStage(GameManager.PetStage stage)
    {
        switch (stage)
        {
            case GameManager.PetStage.Stage1: return stage1Visuals;
            case GameManager.PetStage.Stage2: return stage2Visuals;
            case GameManager.PetStage.Stage3: return stage3Visuals;
            default: return stage1Visuals;
        }
    }

    // ========================
    // 1. 播放吃飯動畫（1 秒）
    // ========================
    public void PlayEat()
    {
        if (currentAction != null) StopCoroutine(currentAction);
        if (AudioController.Instance != null) AudioController.Instance.PlayEat();
        ShowVFX(eatVFX);
        currentAction = StartCoroutine(EatAnimation());
    }

    IEnumerator EatAnimation()
    {
        if (eatSprite != null)
            petImage.sprite = eatSprite;
        yield return new WaitForSeconds(1f);
        // hide eat VFX when animation ends
        HideVFX(eatVFX);
        currentAction = null;
    }

    // ========================
    // 2. 被打動畫（0.3 秒）
    // ========================
    public void PlayHit()
    {
        if (currentAction != null) StopCoroutine(currentAction);
        if (AudioController.Instance != null) AudioController.Instance.PlayHit();
        ShowVFX(hitVFX);
        currentAction = StartCoroutine(HitAnimation());
    }

    IEnumerator HitAnimation()
    {
        if (hitSprite != null)
            petImage.sprite = hitSprite;
        yield return new WaitForSeconds(0.3f);
        // hide hit VFX when animation ends
        HideVFX(hitVFX);
        currentAction = null;
    }

    // ========================
    // 3. 撫摸動畫（0.5 秒）
    // ========================
    public void PlayPet()
    {
        if (currentAction != null) StopCoroutine(currentAction);
        if (AudioController.Instance != null) AudioController.Instance.PlayPet();
        ShowVFX(petVFX);
        currentAction = StartCoroutine(PetAnimation());
    }

    IEnumerator PetAnimation()
    {
        if (petSprite != null)
            petImage.sprite = petSprite;
        // Show pet action image (if assigned)
        if (petActionImage != null)
        {
            if (petActionSprite != null) petActionImage.sprite = petActionSprite;
            petActionImage.gameObject.SetActive(true);
        }
        yield return new WaitForSeconds(0.5f);
        // Hide pet action image after animation
        if (petActionImage != null)
            petActionImage.gameObject.SetActive(false);
        // hide pet VFX when animation ends
        HideVFX(petVFX);

        currentAction = null;
    }

    // ========================
    // 噎住狀態
    // ========================
    public void StartChoke(int current, int goal)
    {
        isChoking = true;
        chokeProgressBar.SetActive(true);
        if (chokeBar != null) chokeBar.fillAmount = 0;
        if (chokeText != null) chokeText.text = $"{current}/{goal}";

        if (currentAction != null) StopCoroutine(currentAction);

        if (chokeSprite != null)
            petImage.sprite = chokeSprite;

        if (AudioController.Instance != null) AudioController.Instance.PlayChoke();

        // Use pre-placed choke VFX if assigned; otherwise instantiate fallback prefab
        if (chokeVFXInstance != null)
        {
            activeChokeVFX = chokeVFXInstance;
            ShowVFX(activeChokeVFX);
        }
        else if (chokeVFXPrefab != null)
        {
            Transform parent = visualEffectsParent;
            if (parent == null)
            {
                GameObject found = GameObject.Find("VisualEffects");
                if (found != null) parent = found.transform;
            }

            if (parent != null)
            {
                activeChokeVFX = Instantiate(chokeVFXPrefab, parent);
                if (activeChokeVFX != null)
                {
                    // Ensure the instantiated VFX is active and on top
                    activeChokeVFX.SetActive(true);
                    activeChokeVFX.transform.SetAsLastSibling();
                }

                // If the prefab is a UI element, stretch it to full screen
                var rt = activeChokeVFX.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = Vector2.zero;
                }
                else
                {
                    activeChokeVFX.transform.localPosition = Vector3.zero;
                    activeChokeVFX.transform.localScale = Vector3.one;
                }
            }
        }
    }

    public void UpdateChokeProgress(float progress, int current, int goal)
    {
        if (!isChoking) return;
        if (chokeBar != null) chokeBar.fillAmount = progress;
        if (chokeText != null) chokeText.text = $"{current}/{goal}";
    }

    public void StopChoke(GameManager.PetStage stage, int mood)
    {
        isChoking = false;
        chokeProgressBar.SetActive(false);
        // Hide or destroy active choke VFX if exists
        if (activeChokeVFX != null)
        {
            // If it is the pre-placed instance, just deactivate; otherwise it was instantiated so destroy
            if (activeChokeVFX == chokeVFXInstance)
            {
                HideVFX(activeChokeVFX);
            }
            else
            {
                Destroy(activeChokeVFX);
            }
            activeChokeVFX = null;
        }

        ShowState(stage, mood);
    }

    public bool IsChoking()
    {
        return isChoking;
    }

    public bool IsPlayingAnimation()
    {
        return currentAction != null;
    }

    // Helper: activate a pre-placed VFX (or reparent to VisualEffects if needed) and make it fullscreen
    void ShowVFX(GameObject vfx)
    {
        if (vfx == null)
        {
            Debug.LogWarning("ShowVFX called with null VFX");
            return;
        }

        // If the provided GameObject is a prefab asset (not part of a scene), instantiate it under the VisualEffects parent.
        GameObject instance = null;
        bool providedIsSceneObject = vfx.scene.IsValid();
        Debug.Log($"ShowVFX called for '{vfx.name}' (sceneObject={providedIsSceneObject})");
        if (!providedIsSceneObject)
        {
            Transform parent = visualEffectsParent;
            if (parent == null)
            {
                GameObject found = GameObject.Find("VisualEffects");
                if (found != null) parent = found.transform;
            }

            instance = Instantiate(vfx, parent);
            // track mapping so HideVFX can destroy the instance later
            runtimeVFXInstances[vfx] = instance;
            Debug.Log($"Instantiated VFX prefab '{vfx.name}' -> instance '{instance.name}' under '{parent?.name}'");
        }
        else
        {
            instance = vfx;
        }

        if (instance == null) return;

        // Activate instance without modifying its transform or scale
        instance.SetActive(true);
        Debug.Log($"Activated VFX instance '{instance.name}', parent='{instance.transform.parent?.name}'");

        // Try to (re)start particle systems
        var particleSystems = instance.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particleSystems)
        {
            ps.Clear(true);
            ps.Play(true);
        }
        Debug.Log($"VFX '{instance.name}' has {particleSystems.Length} ParticleSystem(s)");

        // Try to (re)start animators
        var animators = instance.GetComponentsInChildren<Animator>(true);
        foreach (var anim in animators)
        {
            anim.enabled = true;
            anim.Rebind();
            anim.Update(0f);
            anim.Play(0, -1, 0f);
        }
        Debug.Log($"VFX '{instance.name}' has {animators.Length} Animator(s)");

        // Try to play Visual Effect Graph (VFX Graph) components
        var vfxs = instance.GetComponentsInChildren<VisualEffect>(true);
        foreach (var ve in vfxs)
        {
            try { ve.Stop(); } catch { }
            try { ve.Play(); } catch { }
        }
        Debug.Log($"VFX '{instance.name}' has {vfxs.Length} VisualEffect(s)");

        // Try to play PlayableDirector (Timeline) components
        var directors = instance.GetComponentsInChildren<PlayableDirector>(true);
        foreach (var d in directors)
        {
            try { d.time = 0; d.Play(); } catch { }
        }
        Debug.Log($"VFX '{instance.name}' has {directors.Length} PlayableDirector(s)");

        // Respect the prefab/instance's existing transform and scale — do not modify RectTransform or localScale.
    }

    void HideVFX(GameObject vfx)
    {
        if (vfx == null)
        {
            Debug.LogWarning("HideVFX called with null VFX");
            return;
        }

        // If the provided object is a prefab asset that we instantiated, its runtime clone is stored in the dictionary.
        // If HideVFX is called with the original prefab (field), destroy the runtime clone; if called with the runtime clone, destroy it as well.
        // First check if vfx is a key (prefab) in the mapping
        if (runtimeVFXInstances.TryGetValue(vfx, out var inst))
        {
            // stop particle systems on instance then destroy
            var psList = inst.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in psList) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Debug.Log($"Destroying runtime VFX instance '{inst.name}' for prefab '{vfx.name}'");
            Destroy(inst);
            runtimeVFXInstances.Remove(vfx);
            return;
        }

        // If vfx is an instantiated clone (value), find and remove its key
        foreach (var kv in new List<KeyValuePair<GameObject, GameObject>>(runtimeVFXInstances))
        {
            if (kv.Value == vfx)
            {
                var psList = vfx.GetComponentsInChildren<ParticleSystem>(true);
                foreach (var ps in psList) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                Debug.Log($"Destroying runtime VFX instance '{vfx.name}' (matched value)");
                Destroy(vfx);
                runtimeVFXInstances.Remove(kv.Key);
                return;
            }
        }

        // Otherwise treat it as a pre-placed scene object: stop systems and deactivate
        var particleSystems = vfx.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particleSystems)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        var animators = vfx.GetComponentsInChildren<Animator>(true);
        foreach (var anim in animators)
        {
            anim.Rebind();
            anim.Update(0f);
            anim.enabled = false;
        }

        var vfxs = vfx.GetComponentsInChildren<VisualEffect>(true);
        foreach (var ve in vfxs)
        {
            try { ve.Stop(); } catch { }
        }

        var directors = vfx.GetComponentsInChildren<PlayableDirector>(true);
        foreach (var d in directors)
        {
            try { d.Stop(); } catch { }
        }

        Debug.Log($"Deactivating pre-placed VFX '{vfx.name}' (ParticleSystems={particleSystems.Length}, Animators={animators.Length}, VisualEffects={vfxs.Length}, Directors={directors.Length})");
        vfx.SetActive(false);
    }

    // ========================
    // 更新 UI 顯示
    // ========================
    public void UpdateUI(int mood, int feed, int exp, int maxMood, int maxFeed, int maxExp, int level)
    {
        if (moodBar != null) moodBar.fillAmount = (float)mood / maxMood;
        if (feedBar != null) feedBar.fillAmount = (float)feed / maxFeed;
        if (expBar != null) expBar.fillAmount = (float)exp / maxExp;

        if (moodText != null) moodText.text = $"{mood}/{maxMood}";
        if (feedText != null) feedText.text = $"{feed}/{maxFeed}";
        if (expText != null) expText.text = $"{exp}/{maxExp}";
        if (levelText != null) levelText.text = $"Level {level}";
    }
}
