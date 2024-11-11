using UnityEngine;

public interface INetworkPlayerAI
{
    void Rpc_SetActive(bool active);
}

public class PlayerAI : MonoBehaviour
{
    // The time range for randomize the player's movement.
    private static readonly Vector2 randomMovementTime = new(3, 6);

    // Current target dodge direction that randomized. 
    private Vector3 _targetDodgeVector;
    private Bull _bull;
    private bool _needDodge;
    private float _rotateSpeed;
    private float _randomTimer;
    private float _randomTime;
    private IPlayer _iPlayer;

    private IPlayer IPlayer => _iPlayer ??= GetComponent<IPlayer>();

    private void GenerateRandomMovement()
    {
        // Randomize next random movement time.
        _randomTime = Random.Range(randomMovementTime.x, randomMovementTime.y);
        // Reset random timer.
        _randomTimer = Time.time;
        
        // Random the movement data.
        IPlayer.RotateAngle = Random.Range(-10f, 10f);
        IPlayer.MovingSpeed = Random.Range(0.1f, 3f);
    }

    private void GameStateEnableEvent(GameState state)
    {
        // While GameState change to gaming set the bull instance and add the delegate event to it.
        if (state == GameState.gaming)
        {
            _bull = Bull.Instance;
            _bull.StateEnableEvent += BullStateEnableEvent;
        }
    }

    private void BullStateEnableEvent(BullState state)
    {
        // Random the behaviour of AI Player while Bull State change.
        switch (state)
        {
            case BullState.aiming:
            case BullState.rush:
            case BullState.knockWall:
                // Randomize player dodge or not dodge the bull.
                _needDodge = Random.Range(0, 10) > 2;
                // Get the vector for dodge the bull.
                _targetDodgeVector = Vector3.Cross(_bull.IBull.AimingDirection, Vector3.up).normalized;

                /*
                RaycastHit[] hits = new RaycastHit[10];
                
                var size = Physics.RaycastNonAlloc(transform.position, _targetDodgeVector, hits, 50);
                for(int i = 0; i < size; ++i)
                {
                    if (hits[i].collider.CompareTag("Wall"))
                    {
                        break;
                    }
                }*/

                // Set the rotate direction by bull on this AI Player which side. 
                _rotateSpeed = Vector3.Distance(_bull.transform.position, transform.position);
                _rotateSpeed = Vector3.Dot(_targetDodgeVector, -transform.forward) >= 0 ? 
                    _rotateSpeed : 
                    -_rotateSpeed;
                break;
            
            //_needDodge = true;
            //break;
            
            default:
                _needDodge = false;
                break;
        }
    }

    void OnEnable()
    {
        GameManager.Instance.StateEnableEvent += GameStateEnableEvent;
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StateEnableEvent -= GameStateEnableEvent;
        }
        if (Bull.Instance != null)
        {
            Bull.Instance.StateEnableEvent -= BullStateEnableEvent;
        }
    }
    
    void FixedUpdate()
    {
        /*if (!IPlayer.IsLocalAI())
            return;*/
        if (GameManager.Instance.State < GameState.gaming)
            return;
        if (IPlayer.HP <= 0)
            return;

        // Movement logic.
        if (_needDodge)
        {
            if (Vector3.Angle(transform.forward, _targetDodgeVector) > 5)
            {
                IPlayer.RotateAngle = Random.Range(3f, 0.5f) * _rotateSpeed;
            }
            else
            {
                IPlayer.RotateAngle = 0;
            }

            IPlayer.MovingSpeed = 10;
        }
        else
        {
            //IPlayer.MovingSpeed = 0;
            if (Time.time - _randomTimer > _randomTime)
            {
                GenerateRandomMovement();
            }
        }

        // Process movement logic.
        IPlayer.MovementLogic(Time.fixedDeltaTime);
    }
}
