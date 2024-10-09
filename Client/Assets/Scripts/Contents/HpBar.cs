using UnityEngine;

public class HpBar : MonoBehaviour
{
    [SerializeField]
    Transform hpBar = null;

    public void SetHpBar(float ratio)
	{
        ratio = Mathf.Clamp(ratio, 0, 1);
        hpBar.localScale = new Vector3(ratio, 0.1f, 1);
	}
}
