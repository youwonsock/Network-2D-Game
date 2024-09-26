using UnityEngine;
using UnityEngine.EventSystems;

public class GameScene : MonoBehaviour
{
    public void Awake()
    {
        Object obj = GameObject.FindObjectOfType(typeof(EventSystem));
        if (obj == null)
            Managers.Resource.Instantiate("UI/EventSystem").name = "@EventSystem";

        Managers.Map.LoadMap(1);

        Screen.SetResolution(640, 480, false);
    }
}
