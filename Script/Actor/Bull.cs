using System;
using System.Collections;
using System.Collections.Generic;
using Airpass.AudioManager;
using Airpass.DesignPattern;
using Airpass.Utility;
using UnityEngine;
using Random = UnityEngine.Random;

// Interface defined for Bull in different XRSportsType.
public interface IBull
{
    Vector3 AimingDirection { get; set; }
    Vector3 PositionAdjust { get; set; }
    bool IsLocal { get; }
}

public class Bull : Processor<Bull, BullState>
{
    // Time for bull to aiming
    public float aimingTime = 1.0f;
    public float speed = 5.0f;
    public float accelerate = 0.5f;
    
    [SerializeField] private ParticleSystem pts_rush; 
    [SerializeField] private ParticleSystem pts_knock;
    [SerializeField] private Rigidbody _rigidBody;
    [SerializeField] private Animator _animator;

    private float _currentSpeed = 5.0f;
    private GameManager _gm;
    private Coroutine _coroutine;
    private IBull _iBull;
    private static readonly int ANIMATION_STATE = Animator.StringToHash("State");

    public IBull IBull => _iBull ??= GetComponent<IBull>();
    
    // Initialize state and transform for each game start.
    public void ResetTransform()
    {
        State = BullState.none;
        transform.position = Vector3.zero;
        _rigidBody.MovePosition(Vector3.zero);
        transform.rotation = Quaternion.LookRotation(Vector3.back);
        _rigidBody.MoveRotation(Quaternion.LookRotation(Vector3.back));
        transform.localScale = Vector3.one * GameManager.Instance.sizeOfBull;
    }

    // Initialize for bull game start.
    public void Initialize()
    {
        _gm = GameManager.Instance;
        ResetTransform();
        State = BullState.aiming;
        gameObject.SetActive(true);
        pts_rush.Clear();
    }

    // Let always one state coroutine been process.
    private void StartCoroutineLogic(IEnumerator enumerator)
    {
        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
        }
        _coroutine = StartCoroutine(enumerator);
    }

    private IEnumerator IE_Aiming()
    {
        Quaternion originalRotation = _rigidBody.rotation;
        float timer = 0;
        while (timer <= aimingTime)
        {
            if (IBull.AimingDirection != Vector3.zero)
            {
                Quaternion targetDirection = Quaternion.LookRotation(IBull.AimingDirection, Vector3.up);
                _rigidBody.MoveRotation(Quaternion.Lerp(originalRotation, targetDirection, timer / aimingTime));
            }
            timer += Time.deltaTime;
            yield return null;
        }

        State = BullState.rush;
    }

    private IEnumerator IE_KnockWall()
    {
        yield return Utility.GetWaitForSecond(_gm.bullBreakTime);
        State = BullState.aiming;
    }

    void Enable_Aiming()
    {        
        if (PreState == BullState.aiming)
            return;

        List<IPlayer> playerList = new();

        foreach (var player in _gm.PlayerList)
        {
            try
            {
                if (player.HP > 0)
                {
                    playerList.Add(player);
                }
            }
            catch
            {
                // ignored
            }
        }
        
        // Check is there any player.
        if (playerList.Count > 0)
        {
            // Only local client has authentication to change aiming direction in multi-player mode.
            if (IBull.IsLocal)
            {
                Vector3 direction = (playerList[Random.Range(0, playerList.Count)]
                        .Transform.position - _rigidBody.position)
                    .normalized;
                direction.y = 0;
                
                IBull.AimingDirection = direction;
            }

            StartCoroutineLogic(IE_Aiming());
        }
        else
        {
            State = BullState.none;
        }
        
        // Sync position while aiming in multi-player mode.
        if (!IBull.IsLocal)
        {
            _rigidBody.position = IBull.PositionAdjust;
        }
    }

    void Enable_Rush()
    {
        _currentSpeed = speed;
    }

    void Enable_KnockWall()
    {
        if (PreState == BullState.knockWall)
            return;

        if (GameManager.Instance.State < GameState.settlement)
        {
            AudioManager.PlaySFX(AudioClipKey.SFX_BullKnockWall);
        }

        StartCoroutineLogic(IE_KnockWall());
    }
    
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Wall") && State == BullState.rush)
        {
            _gm.mmfp_crush.PlayFeedbacks();
        
            if (State == BullState.aiming)
                return;
            
            // Play the knock particle
            pts_knock.transform.position = other.ClosestPointOnBounds(transform.position);
            pts_knock.Play();
        
            State = BullState.knockWall;
        }
        else if (other.CompareTag("Player") && State == BullState.rush)
        {
            if (!other.TryGetComponent(out IPlayer player))
                return;
            if (player.Invincible)
                return;

            if (player.IsLocalPlayer())
            {
                _gm.mmfp_shake.PlayFeedbacks();
            }

            pts_knock.Play();
        
            player.TakeDamage();
        }
    }

    /*private void OnCollisionEnter(Collision other)
    {
        if (!other.gameObject.CompareTag("Wall"))
            return;
        
        _gm.mmfp_shake.PlayFeedbacks();
        
        if (State == BullState.aiming)
            return;
        
        // Play the knock particle
        pts_knock.transform.position = other.collider.ClosestPointOnBounds(transform.position);
        pts_knock.Play();
        
        State = BullState.knockWall;
    }*/

    void FixedUpdate()
    {
        if (IBull.IsLocal)
        {
            IBull.PositionAdjust = _rigidBody.position;
        }
        
        switch (State)
        {
            case BullState.rush:
                _currentSpeed += accelerate;
                _rigidBody.MovePosition(_rigidBody.position += transform.forward * (Time.fixedDeltaTime * _currentSpeed));
                goto MOVEMENT_LIMIT;
            
            case BullState.knockWall:
                _rigidBody.MovePosition(_rigidBody.position -= transform.forward * (Time.fixedDeltaTime * 4));
                
                MOVEMENT_LIMIT:
                
                float range = _gm.rangeOfWall * _gm.sizeOfWall;
                if (Vector3.Distance(Vector3.zero, _rigidBody.position) > range)
                {
                    _rigidBody.position = _rigidBody.position.normalized * range;
                }
                break;
            
            case BullState.aiming:
                if (!IBull.IsLocal)
                {
                    _rigidBody.position = IBull.PositionAdjust;
                }
                break;
        }
    }

    protected override void Initialization()
    {
        base.Initialization();

        StateEnableEvent += _ =>
        {
            if (PreState != State)
            {
                _animator.SetInteger(ANIMATION_STATE, (int)State);
            }
        };
    }
}

[Serializable]
public enum BullState
{
    none,
    aiming,
    rush,
    knockWall
}