using UnityEngine;

public class BullXRRK : MonoBehaviour, IBull
{
    [SerializeField] private Bull _bull;
    public Vector3 AimingDirection { get; set; }
    public Vector3 PositionAdjust { get; set; }
    public bool IsLocal => true;
}
