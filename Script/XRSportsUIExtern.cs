using UnityEngine;

public class XRSportsUIExtern : MonoBehaviour
{
    public void OnTitleEnable()
    {
        GameManager.Instance.State = GameState.none;
    }
}
