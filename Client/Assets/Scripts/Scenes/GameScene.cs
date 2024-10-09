using UnityEngine;
using UnityEngine.EventSystems;

public class GameScene : MonoBehaviour
{
    UI_GameScene sceneUI;

    public void Awake()
    {
        Object obj = GameObject.FindObjectOfType(typeof(EventSystem));
        if (obj == null)
            Managers.Resource.Instantiate("UI/EventSystem").name = "@EventSystem";

        Managers.Map.LoadMap(1);

        Screen.SetResolution(480, 320, false);

        sceneUI = Managers.UI.ShowSceneUI<UI_GameScene>();
    }
}
