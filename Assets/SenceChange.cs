using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SenceChange : MonoBehaviour
{
    [SerializeField]
    private string[] scenes = new string[3];

    [SerializeField]
    private Animator transitionAnimator;

    [SerializeField]
    private float transitionDuration = 1f;

    private void Awake()
    {
        if (scenes == null || scenes.Length == 0)
            Debug.LogWarning("SenceChange: scenes array is empty. Assign scene names in Inspector.");
        if (transitionAnimator == null)
            Debug.LogWarning("SenceChange: transitionAnimator not assigned. Assign an Animator for transitions.");
    }

    public void LoadSceneByIndex(int index)
    {
        if (scenes == null)
        {
            Debug.LogError("SenceChange: scenes array is null.");
            return;
        }
        if (index < 0 || index >= scenes.Length)
        {
            Debug.LogError($"SenceChange: scene index {index} out of range.");
            return;
        }
        LoadScene(scenes[index]);
    }

    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("SenceChange: sceneName is null or empty.");
            return;
        }
        Debug.Log($"SenceChange: LoadScene requested -> '{sceneName}'");
        StartCoroutine(DoSceneTransition(sceneName));
    }

    private IEnumerator DoSceneTransition(string sceneName)
    {
        if (transitionAnimator != null)
        {
            Debug.Log("SenceChange: triggering transition animator 'Start'");
            transitionAnimator.SetTrigger("Start");
        }
        else
        {
            Debug.Log("SenceChange: no transitionAnimator assigned, skipping trigger");
        }

        Debug.Log($"SenceChange: waiting {transitionDuration} seconds before loading '{sceneName}'");
        yield return new WaitForSeconds(transitionDuration);

        Debug.Log($"SenceChange: calling LoadSceneAsync('{sceneName}')");
        AsyncOperation async = SceneManager.LoadSceneAsync(sceneName);
        while (!async.isDone)
        {
            yield return null;
        }
        Debug.Log($"SenceChange: finished loading '{sceneName}'");
    }

    public void ReloadCurrentScene()
    {
        StartCoroutine(DoSceneTransition(SceneManager.GetActiveScene().name));
    }
}
