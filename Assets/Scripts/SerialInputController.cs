using UnityEngine;
using System.IO.Ports;
using System.Threading;
using System.Collections.Generic;
using UnityEngine.UI;

public class SerialInputController : MonoBehaviour
{
    private SerialPort port;
    private Thread readThread;
    private bool isRunning = false;

    private Queue<string> commandQueue = new Queue<string>();
    private object queueLock = new object();

    // === 狀態 ===
    public int mood = 50;
    public int feed = 0;
    public int exp = 0;
    public int evolveExp = 200;

    // UI
    public Slider moodSlider;
    public Slider feedSlider;
    public Slider expSlider;

    // 寵物圖片控制
    public PetVisualController petVisual;

    void Start()
    {
        port = new SerialPort("COM3", 9600);
        port.ReadTimeout = 100;

        try { port.Open(); }
        catch { Debug.LogError("❌ 無法開啟序列埠 COM3"); }

        isRunning = true;
        readThread = new Thread(ReadSerialLoop);
        readThread.Start();

        moodSlider.value = mood;
        feedSlider.value = feed;
        expSlider.value = exp;
    }

    void OnDestroy()
    {
        isRunning = false;
        if (readThread != null) readThread.Join();
        if (port != null && port.IsOpen) port.Close();
    }

    void ReadSerialLoop()
    {
        while (isRunning)
        {
            if (port != null && port.IsOpen)
            {
                try
                {
                    string line = port.ReadLine().Trim();
                    lock (queueLock) commandQueue.Enqueue(line);
                }
                catch { }
            }
        }
    }

    void Update()
    {
        // 讀取 Arduino 指令並處理
        while (true)
        {
            string cmd = null;
            lock (queueLock)
            {
                if (commandQueue.Count > 0)
                    cmd = commandQueue.Dequeue();
                else break;
            }

            if (cmd != null)
                ProcessCommand(cmd);
        }

        // 更新 UI
        moodSlider.value = mood;
        feedSlider.value = feed;
        expSlider.value = exp;

        // 更新 Idle 狀態圖片（不播動畫時才會套用）
        if (!petVisual.IsChoking())   // 噎住時不切 Idle
            petVisual.ShowState(mood, feed, exp, evolveExp);

        // 回傳 mood 到 Arduino
        if (port.IsOpen)
            port.WriteLine("MOOD:" + mood);
    }

    // =====================================
    // 接收到 Arduino 指令後 → 處理狀態＋播動畫
    // =====================================
    void ProcessCommand(string input)
    {
        Debug.Log("收到 Arduino 指令: " + input);

        if (input == "HOLD")  // 撫摸
        {
            mood += 10;
            exp += 10;
            petVisual.PlayPet();
        }
        else if (input == "CLICK")    // 餵食
        {
            feed += 5;
            mood += 5;
            exp += 5;
            petVisual.PlayEat();

            // 模擬噎住（如果你要使用）
            if (feed >= 90)
                petVisual.StartChoke();
        }
        else if (input == "DOUBLE")  // 打擊
        {
            mood -= 15;
            exp += 2;
            petVisual.PlayHit();

            // 被打時也可能噎住
            if (Random.value < 0.1f)
                petVisual.StartChoke();
        }
        else if (input == "TAP")   // 假設 Arduino 傳這個來代表拍背
        {
            petVisual.AddChokeProgress();
        }

        // 限制數值
        mood = Mathf.Clamp(mood, 0, 100);
        feed = Mathf.Clamp(feed, 0, 100);
        exp = Mathf.Clamp(exp, 0, 1000);
    }
}
