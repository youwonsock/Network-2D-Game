using UnityEngine;

public class Managers : MonoBehaviour
{
    static Managers instance; // 유일성이 보장된다
    public static Managers Instance { get { Init(); return instance; } } // 유일한 매니저를 갖고온다

    #region Contents
    InventoryManager inven = new InventoryManager();
    MapManager map = new MapManager();
    ObjectManager obj = new ObjectManager();
    NetworkManager network = new NetworkManager();
    WebManager web = new WebManager();

    public static InventoryManager Inven { get { return Instance.inven; } }
    public static MapManager Map { get { return Instance.map; } }
    public static ObjectManager Object { get { return Instance.obj; } }
    public static NetworkManager Network { get { return Instance.network; } }
    public static WebManager Web { get { return Instance.web; } }
	#endregion

	#region Core
	DataManager data = new DataManager();
    ResourceManager resource = new ResourceManager();
    UIManager ui = new UIManager();

    public static DataManager Data { get { return Instance.data; } }
    public static ResourceManager Resource { get { return Instance.resource; } }
    public static UIManager UI { get { return Instance.ui; } }
    #endregion

    void Start()
    {
        Init();
	}

    void Update()
    {
        network.Update();
    }

    static void Init()
    {
        if (instance == null)
        {
			GameObject go = GameObject.Find("@Managers");
            if (go == null)
            {
                go = new GameObject { name = "@Managers" };
                go.AddComponent<Managers>();
            }

            DontDestroyOnLoad(go);
            instance = go.GetComponent<Managers>();

            instance.data.Init();
        }		
	}
}
