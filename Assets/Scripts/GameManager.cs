using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum PetStage { Stage1, Stage2, Stage3 }

    [Header("狀態數值")]
    public int mood = 50;
    public int feed = 0;
    public int exp = 0;

    [Header("進化門檻")]
    public int stage2Threshold = 100;
    public int stage3Threshold = 600;

    [Header("當前階段")]
    public PetStage currentStage = PetStage.Stage1;

    [Header("數值變化單位")]
    [Tooltip("餵食時增加的飽食度")]
    public int feedIncreaseOnFeed = 5;
    [Tooltip("餵食時增加的心情")]
    public int moodIncreaseOnFeed = 5;
    [Tooltip("餵食時增加的經驗值")]
    public int expIncreaseOnFeed = 5;
    
    [Tooltip("撫摸時增加的心情")]
    public int moodIncreaseOnPet = 10;
    [Tooltip("撫摸時增加的經驗值")]
    public int expIncreaseOnPet = 10;
    
    [Tooltip("打擊時減少的心情")]
    public int moodDecreaseOnHit = 15;
    [Tooltip("打擊時增加的經驗值")]
    public int expIncreaseOnHit = 2;
    
    [Tooltip("嘔吐時減少的飽食度")]
    public int feedDecreaseOnVomit = 20;
    [Tooltip("嘔吐時減少的心情")]
    public int moodDecreaseOnVomit = 10;

    [Header("引用")]
    public PetVisualController petVisual;
    public SerialInputController serialController;

    private bool isVomiting = false;
    private int chokeCount = 0;
    private const int CHOKE_GOAL = 5;

    private int consecutiveFeedCount = 0;
    private const int FEED_LIMIT = 5;

    public int maxMood = 100;
    public int maxFeed = 100;
    public int maxExp = 600;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        CheckEvolution();
        CheckReset();

        // 更新 Visual 和 UI
        int currentLevel = (int)currentStage + 1;
        int levelMaxExp = GetCurrentLevelMaxExp();
        petVisual.UpdateUI(mood, feed, exp, maxMood, maxFeed, levelMaxExp, currentLevel);

        // 更新 Idle 狀態圖片（不播動畫且沒噎住時才會套用）
        if (!petVisual.IsChoking() && !petVisual.IsPlayingAnimation())
            petVisual.ShowState(currentStage, mood);
    }

    int GetCurrentLevelMaxExp()
    {
        switch (currentStage)
        {
            case PetStage.Stage1: return stage2Threshold;
            case PetStage.Stage2: return stage3Threshold;
            case PetStage.Stage3: return stage3Threshold;
            default: return stage2Threshold;
        }
    }

    // 移除 UpdateUI 方法，因為已經移到 PetVisualController


    public void OnFeed()
    {
        if (isVomiting) return; // 噎住時不能餵食

        feed += feedIncreaseOnFeed;
        mood += moodIncreaseOnFeed;
        exp += expIncreaseOnFeed;

        consecutiveFeedCount++;

        if (consecutiveFeedCount >= FEED_LIMIT)
        {
            TriggerVomit();
        }
        else
        {
            petVisual.PlayEat();
        }

        ClampStats();
    }

    public void OnPet()
    {
        if (isVomiting) return; // 噎住時不能撫摸

        consecutiveFeedCount = 0; // 重置連續餵食計數

        mood += moodIncreaseOnPet;
        exp += expIncreaseOnPet;
        petVisual.PlayPet();
        ClampStats();
    }

    public void OnHit()
    {
        if (isVomiting) return; // 噎住時不能打擊

        consecutiveFeedCount = 0; // 重置連續餵食計數

        mood -= moodDecreaseOnHit;
        exp += expIncreaseOnHit;
        petVisual.PlayHit();
        ClampStats();
    }

    public void OnTap()
    {
        if (isVomiting)
        {
            chokeCount++;
            petVisual.UpdateChokeProgress((float)chokeCount / CHOKE_GOAL, chokeCount, CHOKE_GOAL);

            if (chokeCount >= CHOKE_GOAL)
            {
                StopVomit();
            }
        }
    }

    void TriggerVomit()
    {
        isVomiting = true;
        feed = Mathf.Max(0, feed - feedDecreaseOnVomit);
        mood = Mathf.Max(0, mood - moodDecreaseOnVomit);
        consecutiveFeedCount = 0;

        chokeCount = 0;
        petVisual.StartChoke(chokeCount, CHOKE_GOAL);
        petVisual.UpdateChokeProgress(0, chokeCount, CHOKE_GOAL);
    }

    void StopVomit()
    {
        isVomiting = false;
        chokeCount = 0;
        petVisual.StopChoke(currentStage, mood);
    }

    void CheckEvolution()
    {
        if (currentStage == PetStage.Stage2 && exp >= stage3Threshold)
        {
            currentStage = PetStage.Stage3;
            exp = stage3Threshold;
            Debug.Log("進化到第三階段！");
            TriggerEndSceneTransition();
        }
        else if (currentStage == PetStage.Stage1 && exp >= stage2Threshold)
        {
            currentStage = PetStage.Stage2;
            exp = 0;
            Debug.Log("進化到第二階段！");
        }
    }

    void TriggerEndSceneTransition()
    {
        if (endTransitionTriggered) return;
        endTransitionTriggered = true;

        Debug.Log("TriggerEndSceneTransition: checking active scene before switching to End");
        string active = SceneManager.GetActiveScene().name;
        Debug.Log($"TriggerEndSceneTransition: active scene = {active}");

        if (active == "MainScene")
        {
            Debug.Log("TriggerEndSceneTransition: in MainScene -> switching to End");
            if (senceChange != null)
            {
                senceChange.LoadScene("End");
            }
            else
            {
                Debug.LogWarning("TriggerEndSceneTransition: senceChange not assigned, using SceneManager.LoadScene fallback");
                SceneManager.LoadScene("End");
            }
        }
        else
        {
            Debug.Log("TriggerEndSceneTransition: not in MainScene, skipping automatic End transition");
        }
    }

    void GameOver()
    {
        Debug.Log("Game Over - 達到最高等級！");
        Time.timeScale = 0f;
    }

    void CheckReset()
    {
        // 只有在還沒到 0 且被扣除到 0 以下時才重置（這裡的邏輯需要依需求調整）
        // 原本邏輯：mood <= 0 -> ResetToStage1
        // 新邏輯：保持 0，不重置？或是特定條件才重置？

        // 根據對話需求："Mood gets reseted when lower than 0. Keep it stays in 0."
        // 這意味著不應該因為 mood <= 0 就呼叫 ResetToStage1。
        // 所以我先註解掉這裡的重置邏輯，或者改成其他的失敗條件。

        /* 
        if (mood <= 0)
        {
            Debug.Log("心情太低！重置到第一階段");
            ResetToStage1();
        }
        */
    }

    void ResetToStage1()
    {
        currentStage = PetStage.Stage1;
        mood = 50;
        feed = 0;
        exp = 0;
        consecutiveFeedCount = 0;
    }

    void ClampStats()
    {
        mood = Mathf.Clamp(mood, 0, maxMood);
        feed = Mathf.Clamp(feed, 0, maxFeed);
        exp = Mathf.Clamp(exp, 0, GetCurrentLevelMaxExp());
    }

    public PetStage GetCurrentStage()
    {
        return currentStage;
    }

    [Header("場景轉換")]
    public SenceChange senceChange;

    private bool endTransitionTriggered = false;


    public bool IsChoking()
    {
        return isVomiting;
    }

    // void OnGUI()
    // {
    //     GUIStyle titleStyle = new GUIStyle(GUI.skin.box);
    //     titleStyle.fontSize = 24;
    //     titleStyle.alignment = TextAnchor.MiddleCenter;
    //     titleStyle.normal.textColor = Color.black;

    //     GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
    //     labelStyle.fontSize = 20;
    //     labelStyle.normal.textColor = Color.black;

    //     GUILayout.BeginArea(new Rect(10, 10, 400, 280));
    //     GUILayout.Box("操作說明", titleStyle);

    //     bool arduinoConnected = serialController != null && serialController.IsArduinoConnected();

    //     if (arduinoConnected)
    //     {
    //         GUILayout.Label("Arduino 已連接", labelStyle);
    //         GUILayout.Space(10);
    //         GUILayout.Label("CLICK: 餵食", labelStyle);
    //         GUILayout.Label("HOLD: 撫摸", labelStyle);
    //         GUILayout.Label("DOUBLE: 打擊", labelStyle);

    //         if (IsChoking())
    //         {
    //             GUIStyle rescueStyle = new GUIStyle(labelStyle);
    //             rescueStyle.normal.textColor = Color.red;
    //             GUILayout.Label("TAP: 拍背救援 ⚠️", rescueStyle);
    //         }
    //     }
    //     else
    //     {
    //         GUILayout.Label("鍵盤控制", labelStyle);
    //         GUILayout.Space(10);
    //         GUILayout.Label("A: 餵食", labelStyle);
    //         GUILayout.Label("S: 撫摸", labelStyle);
    //         GUILayout.Label("D: 打擊", labelStyle);

    //         if (IsChoking())
    //         {
    //             GUIStyle rescueStyle = new GUIStyle(labelStyle);
    //             rescueStyle.normal.textColor = Color.red;
    //             GUILayout.Label("F: 拍背救援 ⚠️", rescueStyle);
    //         }
    //     }

    //     GUILayout.EndArea();
    // }
}

