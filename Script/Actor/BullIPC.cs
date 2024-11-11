using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class BullIPC : NetworkBehaviour, IBull
{
    [SerializeField] private Bull _bull;
    public Vector3 AimingDirection { get => _aimingDirection.Value; [ServerRpc] set => _aimingDirection.Value = value; }
    public Vector3 PositionAdjust { get => _positionAdjust.Value; [ServerRpc] set => _positionAdjust.Value = value; }
    public bool IsLocal => IsOwner;

    private readonly SyncVar<Vector3> _aimingDirection = new();
    private readonly SyncVar<Vector3> _positionAdjust = new();
}
