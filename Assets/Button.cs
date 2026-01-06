using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonAnimationController : MonoBehaviour
{
    public Animator animator;
    public string nextSceneName;
    public SenceChange senceChange;

    private bool hasPressed = false;

    void Update()
    {
        // 這裡換成你的「外部按鈕條件」
        if (Input.GetKeyDown(KeyCode.Space) && !hasPressed)
        {
            hasPressed = true;
            Debug.Log("ButtonAnimationController: Space pressed -> trigger Press animator");
            if (animator != null)
                animator.SetTrigger("Press");
            else
                Debug.LogWarning("ButtonAnimationController: animator is null");
            // 臨時直接呼叫以測試場景切換流程是否正常
            LoadNextScene();
        }
    }

    // 給 Animation Event 用
    public void LoadNextScene()
    {
        Debug.Log($"ButtonAnimationController: LoadNextScene called for '{nextSceneName}' (has SenceChange: {senceChange != null})");
        if (senceChange != null)
        {
            senceChange.LoadScene(nextSceneName);
        }
        else
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
