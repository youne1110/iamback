using UnityEngine;
using System.IO.Ports;
using UnityEngine.UI;

public class ArduinoInput : MonoBehaviour
{
    SerialPort port;

    // === 狀態資料 ===
    public int mood = 50;      // 心情
    public int feed = 0;       // 餵食狀態（你可以自訂最大值）
    public int exp = 0;        // 經驗值（累積打/摸/餵食）

    // === UI ===
    public Slider moodSlider;
    public Slider feedSlider;
    public Slider expSlider;

    public Image petImage;         // 顯示寵物圖片用
    public Sprite normalSprite;    // 心情普通
    public Sprite happySprite;     // 心情好
    public Sprite angrySprite;     // 心情差

    public Sprite evolveSprite;    // 進化後的圖片（可選）

    public int evolveExp = 200;    // 達到這個經驗值就進化

    void Start()
    {
        port = new SerialPort("COM3", 9600);
        port.ReadTimeout = 50;
        port.Open();

        // 初始化 Slider
        moodSlider.value = mood;
        feedSlider.value = feed;
        expSlider.value = exp;
    }

    void Update()
{
    if (!port.IsOpen) return;

    try
    {
        // ================================
        // 1. 讀取 Arduino 指令
        // ================================
        string input = port.ReadLine().Trim();

        // 長按：撫摸
        if (input == "HOLD")
        {
            Debug.Log("撫摸 (HOLD)");
            mood += 10;
            exp += 10;
        }
        // 點一下：餵食
        else if (input == "CLICK")
        {
            Debug.Log("餵食 (CLICK)");
            feed += 5;
            mood += 5;
            exp += 5;
        }
        // 快速連點：打一下
        else if (input == "DOUBLE")
        {
            Debug.Log("打擊 (DOUBLE)");
            mood -= 15;
            exp += 2;     // 打擊也算少量經驗
        }

        // ================================
        // 2. 限制所有遊戲數值範圍
        // ================================
        mood = Mathf.Clamp(mood, 0, 100);
        feed = Mathf.Clamp(feed, 0, 100);
        exp = Mathf.Clamp(exp, 0, 1000);

        // ================================
        // 3. 更新 UI Slider 數值
        // ================================
        moodSlider.value = mood;
        feedSlider.value = feed;
        expSlider.value = exp;

        // ================================
        // 4. 根據心情／經驗更新寵物圖片
        // ================================
        if (exp >= evolveExp)
        {
            // 進化圖片
            petImage.sprite = evolveSprite;
        }
        else
        {
            if (mood >= 70)
                petImage.sprite = happySprite;
            else if (mood >= 40)
                petImage.sprite = normalSprite;
            else
                petImage.sprite = angrySprite;
        }

        // ================================
        // 5. 回傳最新 mood 給 Arduino → 控制 LED
        // ================================
        port.WriteLine("MOOD:" + mood);
    }
    catch
    {
        // 忽略 Serial Timeout 避免卡住
    }
}
}