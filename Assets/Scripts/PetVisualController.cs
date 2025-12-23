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
