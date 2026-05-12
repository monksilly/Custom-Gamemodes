using UnityEngine;
using UnityEngine.UI;

namespace CustomGameModes;

public class TextLoading: MonoBehaviour
{
    public static GameObject LoadingInstance;

    void Awake()
    {
        if (LoadingInstance) return;
        GameObject canvas = GameObject.Find("Canvas/Loading");
        if (canvas)
        {
            LoadingInstance = Instantiate(canvas);
            LoadingInstance.name = "LoadingPrefab";
            LoadingInstance.SetActive(false);
            LoadingInstance.GetComponent<Image>().color += new Color(0f, 0f, 0f, 0.4f);
            DontDestroyOnLoad(LoadingInstance);
        }
    }
}