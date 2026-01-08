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

    // GameManager 引用
    public GameManager gameManager;

    public bool IsArduinoConnected()
    {
        return port != null && port.IsOpen;
    }

    void Start()
    {
        port = new SerialPort("COM3", 9600);
        port.ReadTimeout = 100;

        try { port.Open(); }
        catch { Debug.LogError("❌ 無法開啟序列埠 COM3"); }

        isRunning = true;
        readThread = new Thread(ReadSerialLoop);
        readThread.Start();

        if (gameManager == null)
            gameManager = GameManager.Instance;
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
        // 鍵盤測試輸入
        ProcessKeyboardInput();

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

        // 更新 Idle 狀態圖片的邏輯現在由 GameManager 統一呼叫
        // 這裡只需要處理輸入和回傳

        // 回傳 mood 到 Arduino
        if (port != null && port.IsOpen)
            port.WriteLine("MOOD:" + gameManager.mood);
    }

    // =====================================
    // 鍵盤測試輸入 (編輯器專用)
    // =====================================
    void ProcessKeyboardInput()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.A))       // 餵食 (CLICK)
        {
            gameManager.OnFeed();
        }
        else if (Input.GetKeyDown(KeyCode.S))      // 撫摸 (HOLD)
        {
            gameManager.OnPet();
        }
        else if (Input.GetKeyDown(KeyCode.D))      // 打擊 (DOUBLE)
        {
            gameManager.OnHit();
        }
        else if (Input.GetKeyDown(KeyCode.F))      // 拍背 (TAP) - 只在噎住時有效
        {
            if (gameManager.IsChoking())
                gameManager.OnTap();
        }
#endif
    }

    // =====================================
    // 接收到 Arduino 指令後 → 委派給 GameManager
    // =====================================
    void ProcessCommand(string input)
    {
        Debug.Log("收到 Arduino 指令: " + input);

        if (input == "HOLD")
        {
            gameManager.OnPet();
        }
        else if (input == "CLICK")
        {
            // 如果正在噎住（進入拍背狀態），使用 Arduino 的按鈕改為當作打擊 (DOUBLE)
            if (gameManager.IsChoking())
                gameManager.OnHit();
            else
                gameManager.OnFeed();
        }
        else if (input == "DOUBLE")
        {
            // If choking, treat DOUBLE as TAP (rescue). Otherwise treat as Hit.
            if (gameManager.IsChoking())
                gameManager.OnTap();
            else
                gameManager.OnHit();
        }
        else if (input == "TAP")
        {
            if (gameManager.IsChoking())
                gameManager.OnTap();
        }
    }
}
