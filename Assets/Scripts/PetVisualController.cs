using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PetVisualController : MonoBehaviour
{
    [Header("主圖片顯示")]
    public Image petImage;

    [Header("狀態圖片")]
    public Sprite normalSprite;
    public Sprite happySprite;
    public Sprite angrySprite;
    public Sprite evolveSprite;

    [Header("動作圖片")]
    public Sprite eatSprite;      // 吃飯咀嚼動畫
    public Sprite hitSprite;      // 被打
    public Sprite petSprite;      // 撫摸
    public Sprite chokeSprite;    // 噎住

    [Header("噎住 UI")]
    public GameObject chokeProgressBar;
    public Slider chokeSlider;

    private bool isChoking = false;
    private int chokeCount = 0;

    private Coroutine currentAction = null;

    // ========================
    // 顯示靜態狀態圖（回到 Idle）
    // ========================
    public void ShowState(int mood, int feed, int exp, int evolveExp)
    {
        if (exp >= evolveExp)
        {
            petImage.sprite = evolveSprite;
            return;
        }

        // 你可以改成根據 feed 顯示不同臉
        if (feed >= 80)
            petImage.sprite = happySprite;
        else if (mood >= 70)
            petImage.sprite = happySprite;
        else if (mood >= 40)
            petImage.sprite = normalSprite;
        else
            petImage.sprite = angrySprite;
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
        petImage.sprite = petSprite;
        yield return new WaitForSeconds(0.5f);
        currentAction = null;
    }

    // ========================
    // 4. 噎住狀態
    // ========================
    public void StartChoke()
    {
        isChoking = true;
        chokeCount = 0;
        chokeProgressBar.SetActive(true);
        chokeSlider.value = 0;

        if (currentAction != null) StopCoroutine(currentAction);

        petImage.sprite = chokeSprite;
    }

    public void AddChokeProgress()
    {
        if (!isChoking) return;

        chokeCount++;
        chokeSlider.value = (float)chokeCount / 20f;

        if (chokeCount >= 20)
            StopChoke();
    }

    public void StopChoke()
    {
        isChoking = false;
        chokeProgressBar.SetActive(false);
    }

    public bool IsChoking()
    {
        return isChoking;
    }
}
