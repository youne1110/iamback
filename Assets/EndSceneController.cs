using UnityEngine;
using UnityEngine.SceneManagement;

public class EndSceneController : MonoBehaviour
{
    [Tooltip("(可選) 指向場景轉換管理器以播放過場動畫。若不設定，會直接用 SceneManager.LoadScene。")]
    public SenceChange senceChange;

    [Tooltip("Open 場景名稱（與 Build Settings 中的名稱一致）")]
    public string openSceneName = "Open";

    private bool triggered = false;

    void Update()
    {
        if (triggered) return;
        if (Input.GetKeyDown(KeyCode.Space))
        {
            triggered = true;
            Debug.Log($"EndSceneController: Space pressed -> loading '{openSceneName}' (use SenceChange: {senceChange != null})");
            if (senceChange != null)
            {
                senceChange.LoadScene(openSceneName);
            }
            else
            {
                SceneManager.LoadScene(openSceneName);
            }
        }
    }
}
