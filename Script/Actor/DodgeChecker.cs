using UnityEngine;

public class DodgeChecker : MonoBehaviour
{
    [SerializeField] private Transform iPlayerSource;

    private IPlayer _iPlayer;
    private bool _checking;

    private void Awake()
    {
        _iPlayer = iPlayerSource.GetComponent<IPlayer>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Bull") && !_iPlayer.Invincible)
        {
            //Debug.Log($"DodgeChecker : {other.gameObject.GetComponent<IBull>().BullState}");
            if (other.gameObject.GetComponent<Bull>().State == BullState.rush)
            {
                _checking = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (_checking)
        {
            if (other.CompareTag("Bull") && !_iPlayer.Invincible)
            {
                _iPlayer.Dodge(other.ClosestPoint(transform.position));
                _checking = false;
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (_checking)
        {
            if (other.CompareTag("Bull") && !_iPlayer.Invincible)
            {
                if (other.gameObject.GetComponent<Bull>().State != BullState.rush)
                {
                    _iPlayer.Dodge(other.ClosestPoint(transform.position));
                    _checking = false;
                }
            }
        }
    }
}