using Fusion;
using UnityEngine;

public class BullOB : NetworkBehaviour, IBull
{
    [SerializeField] private Bull _bull;
    [Networked] public Vector3 AimingDirection { get; set; }
    [Networked] public Vector3 PositionAdjust { get; set; }
    public bool IsLocal => Object.HasStateAuthority;
}
