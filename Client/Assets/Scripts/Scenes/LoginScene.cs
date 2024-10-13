using UnityEngine;
using UnityEngine.EventSystems;

public class LoginScene : MonoBehaviour
{
    UI_LoginScene sceneUI;

    public void Awake()
    {
        Object obj = GameObject.FindObjectOfType(typeof(EventSystem));
        if (obj == null)
            Managers.Resource.Instantiate("UI/EventSystem").name = "@EventSystem";

        Managers.Web.BaseUrl = "https://localhost:5001/api";


        Screen.SetResolution(480, 320, false);

        sceneUI = Managers.UI.ShowSceneUI<UI_LoginScene>();
    }
}
