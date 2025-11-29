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
            string input = port.ReadLine().Trim();

            // --------------------
            // 1. 判斷 Arduino 回傳的指令
            // --------------------
            if (input == "HOLD")
            {
                Debug.Log("撫摸");
                mood += 10;
                exp += 10;
            }
            else if (input == "CLICK")
            {
                Debug.Log("餵食");
                feed += 5;
                mood += 5;
                exp += 5;
            }
            else if (input == "DOUBLE")
            {
                Debug.Log("打擊");
                mood -= 15;
                exp += 2;   // 打擊也算經驗，但比較少
            }
            else if (input.StartsWith("MOOD:"))
            {
                // Arduino 傳來的心情值
                mood = int.Parse(input.Substring(5));
            }

            // 限制範圍
            mood = Mathf.Clamp(mood, 0, 100);
            feed = Mathf.Clamp(feed, 0, 100);
            exp = Mathf.Clamp(exp, 0, 1000);

            // --------------------
            // 2. 更新 Slider UI
            // --------------------
            moodSlider.value = mood;
            feedSlider.value = feed;
            expSlider.value = exp;

            // --------------------
            // 3. 根據心情切換圖片
            // --------------------
            if (exp >= evolveExp)
            {
                // 進化後圖片
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
        }
        catch {}
    }
}
