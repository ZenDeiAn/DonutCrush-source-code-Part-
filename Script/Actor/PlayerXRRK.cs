
using UnityEngine;

public class PlayerXRRK : MonoBehaviour, IPlayer
{
    private float _aliveTimer;
    protected GameManager gm;
    
    private int _hp;
    private Rigidbody _rigidBody;
    private Player _player;

    public GameObject GameObject => gameObject;
    public Transform Transform => transform;
    public Rigidbody RigidBody => _rigidBody ??= GetComponent<Rigidbody>();
    public Player Player => _player ??= GetComponent<Player>();
    public Animator Animator => Player.animator;
    public int Number { get; set; }
    public virtual bool IsAI => false;
    public bool IsLocal => true;
    public bool Invincible { get; set; }
    public bool Movable { get; set; }
    public bool BeDamaged { get; set; }

    public int HP { get => _hp;
        set
        {
            _hp = value;
            if (_hp == 0)
                GameOver();
        }
        
    }
    public int PerfectDodgeTime { get; set; }
    public float MovingSpeed { get; set; }
    public float RotateAngle { get; set; }
    public float AliveTime { get; set; }


    public virtual void Initialize(int number)
    {
        this.InitializeLogic(number);
    }

    public void TakeDamage()
    {
        this.TakeDamageLogic();
    }

    public void GameOver()
    {
        this.GameOverLogic();

        if (this.IsLocalPlayer())
        {
            GameManager.Instance.State = GameState.gameOver;
        }
    }

    public void Dodge(Vector3 position)
    {
        this.DodgeLogic(position);
    }

    public void InitializeForGameStart()
    {
        _aliveTimer = Time.realtimeSinceStartup;
    }

    protected virtual void Update()
    {
        if (GameManager.Instance.State != GameState.gaming)
            return;
        
        if (HP <= 0)
            return;
        
        AliveTime = Time.realtimeSinceStartup - _aliveTimer;
    }

    void Awake()
    {
        gm = GameManager.Instance;
    }
}