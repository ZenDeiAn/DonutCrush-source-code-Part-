using System.Linq;
using Airpass.Utility;
using Airpass.XRSports;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class PlayerIPC : IPCPlayer, IPlayer
{
    private static int _gameOverPlayerCount;
    
    private readonly SyncVar<int> _number = new();
    private readonly SyncVar<int> _hp = new();
    private readonly SyncVar<int> _perfectDodgeTime = new();
    private readonly SyncVar<bool> _invincible = new();
    private readonly SyncVar<float> _aliveTime = new();

    private float _aliveTimer;
    private bool _alive;
    private Rigidbody _rigidBody;
    private Player _player;
        
    public Transform Transform => transform;
    public Rigidbody RigidBody => _rigidBody ??= GetComponent<Rigidbody>();
    public Player Player => _player ??= GetComponent<Player>();
    public Animator Animator => Player.animator;
    public int Number { get => _number.Value; [ServerRpc] set =>_number.Value = value; }
    public virtual bool IsAI => false;
    public bool Invincible { get => _invincible.Value; [ServerRpc] set => _invincible.Value = value; }
    public bool Movable { get; set; }
    public bool BeDamaged { get; set; }
    public int HP { get => _hp.Value; [ServerRpc] set => _hp.Value = value; }
    public int PerfectDodgeTime { get => _perfectDodgeTime.Value; [ServerRpc] set => _perfectDodgeTime.Value = value; }
    public float MovingSpeed { get; set; }
    public float RotateAngle { get; set; }
    public float AliveTime {  get => _aliveTime.Value; [ServerRpc] set => _aliveTime.Value = value; }
    
    public void Initialize(int number)
    {
        Rpc_Initialize(number);
    }
    
    public void TakeDamage()
    {
        //Rpc_TakeDamage(Owner);
        this.TakeDamageLogic();
    }

    public void GameOver()
    {
        this.NetworkGameOverLogic();
    }

    public void Dodge(Vector3 position)
    {
        this.DodgeLogic(position);
    }

    public void InitializeForGameStart()
    {
        _aliveTimer = (float)TimeManager.TicksToTime();
    }

    /*[TargetRpc]
    private void Rpc_TakeDamage(NetworkConnection target)
    {
        this.TakeDamageLogic();
    }*/

    [ObserversRpc]
    private void Rpc_Initialize(int number)
    {
        _alive = true;
        this.InitializeLogic(number);
        _gameOverPlayerCount = 0;
        if (this.IsLocalPlayer())
        {
            GameManager.Instance.LocalPlayer = this;
        }
    }

    private void OnHPChange(int prev, int next, bool server)
    {
        if (next <= 0)
        {
            GameOver();
        }
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();

        if (GameManager.Instance.PlayerList.Contains(this))
        {
            GameManager.Instance.PlayerList.Remove(this);
        }
    }

    void Update()
    {
        if (IsLocal && _alive)
        {
            AliveTime = (float)TimeManager.TicksToTime() - _aliveTimer;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        _hp.OnChange += OnHPChange;
    }
}
