using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

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

    [Header("動作圖片")]
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

    void Awake()
    {
        // Ensure pre-placed VFX are hidden at start
        if (eatVFX != null) eatVFX.SetActive(false);
        if (hitVFX != null) hitVFX.SetActive(false);
        if (petVFX != null) petVFX.SetActive(false);
        if (chokeVFXInstance != null) chokeVFXInstance.SetActive(false);

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
        yield return new WaitForSeconds(0.5f);
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
        if (vfx == null) return;

        // If the VFX is not under the visualEffectsParent, reparent it for organization
        if (visualEffectsParent != null && vfx.transform.parent != visualEffectsParent)
            vfx.transform.SetParent(visualEffectsParent, false);

        vfx.SetActive(true);
        vfx.transform.SetAsLastSibling();

        var rt = vfx.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }
        else
        {
            vfx.transform.localPosition = Vector3.zero;
            vfx.transform.localScale = Vector3.one;
        }
    }

    void HideVFX(GameObject vfx)
    {
        if (vfx == null) return;
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
