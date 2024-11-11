using Fusion;

public class PlayerOB_AI : PlayerOB, INetworkPlayerAI
{
    public override bool IsAI => true;

    public override void Spawned()
    {
       // base.Spawned();

        if (HasStateAuthority)
        {
            // Set PlayerRef
            PlayerRef = Object.StateAuthority;
        }

        GameManager.Instance.aiPlayerList.Add(this);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void Rpc_SetActive(bool active)
    {
        gameObject.SetActive(active);
    }
}
