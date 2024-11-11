using System.Linq;
using Airpass.XRSports;
using Fusion;
using UnityEngine;

public class PlayerOB : OBPlayer, IPlayer
{
    private static int _gameOverPlayerCount;
    
    private float _aliveTimer;
    private bool _alive;
    private Rigidbody _rigidBody;
    private Player _player;
    
    public Transform Transform => transform;
    public Rigidbody RigidBody => _rigidBody ??= GetComponent<Rigidbody>();
    public Player Player => _player ??= GetComponent<Player>();
    public Animator Animator => Player.animator;
    [Networked] public int Number { get; set; }
    public virtual bool IsAI => false;
    [Networked] public bool Invincible { get; set; }
    public bool Movable { get; set; }
    public bool BeDamaged { get; set; }
    [Networked(OnChanged = nameof(OnHPChanged))] public int HP { get; set; }
    [Networked] public int PerfectDodgeTime { get; set; }
    public float MovingSpeed { get; set; }
    public float RotateAngle { get; set; }
    [Networked] public float AliveTime { get; set; }
    
    private static void OnHPChanged(Changed<PlayerOB> changed)
    {
        if (changed.Behaviour.HP <= 0)
        {
            changed.Behaviour.GameOver();
        }
    }
    
    public void Initialize(int number)
    {
        Rpc_Initialize(number);
    }

    public void TakeDamage()
    {
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
        _aliveTimer = Time.realtimeSinceStartup;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
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
    
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);
        
        if (GameManager.Instance.PlayerList.Contains(this))
        {
            GameManager.Instance.PlayerList.Remove(this);
        }
    }

    void Update()
    {
        if (IsLocal && _alive)
        {
            AliveTime = Time.realtimeSinceStartup - _aliveTimer;
        }
    }
}
