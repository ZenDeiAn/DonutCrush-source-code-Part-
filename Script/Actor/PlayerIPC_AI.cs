using FishNet.Object;

public class PlayerIPC_AI : PlayerIPC, INetworkPlayerAI
{
    public override bool IsAI => true;

    public override void OnStartClient()
    {
        GameManager.Instance.aiPlayerList.Add(this);
    }

    [ObserversRpc]
    public void Rpc_SetActive(bool active)
    {
        gameObject.SetActive(active);
    }
}
