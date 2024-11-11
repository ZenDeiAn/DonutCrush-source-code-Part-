using System.Collections.Generic;
using System.Linq;
using Airpass.AudioManager;
using QFSW.QC;
using UnityEngine;
using Airpass.Utility;
using Airpass.XRSports;
using TMPro;
using UnityEngine.Serialization;
using UnityEngine.UI;

public interface IPlayer
{
    GameObject GameObject { get; }
    Transform Transform { get; }
    Rigidbody RigidBody { get; }
    Animator Animator { get; }
    Player Player { get; }
    int Number { get; set; }
    bool IsAI { get; }
    bool IsLocal { get; }
    bool Invincible { get; set; }
    bool Movable { get; set; }
    bool BeDamaged { get; set; }
    int HP { get; set; }
    int PerfectDodgeTime { get; set; }
    float MovingSpeed { get; set; }
    float RotateAngle { get; set; }
    float AliveTime { get; set; }
    void Initialize(int number);
    void TakeDamage();
    void GameOver();
    void Dodge(Vector3 position);
    void InitializeForGameStart();
}

[CommandPrefix("Player.")]
public static class PlayerUtility
{
    public const int PLAYER_HP_MAX = 3;
    public const int PLAYER_KNOCK_OFF_DIRECTION_RANGE = 5;
    [Command("InvincibleTime", "After player been hit then invincible for this time."), Savable]
    public static float invincibleTime = 2.0f;
    [Command("KnockOffVelocity", "While player die the strength of fly out of screen."), Savable]
    public static float knockOffVelocity = 150;
    public static float moveSpeed = 50.0f;
    public static float rotateSpeed = 10.0f;
    public static Vector2 speedLimit = new(-3.0f, 3.0f);
    [Command("Accelerate", "Value cause drift while it's low value."), Savable]
    public static float accelerate = 3.5f;
    [Command("Mass"), Savable]
    public static float mass = 0.125f;
    [Command("BeDamaged.Time"), Savable]
    public static float beDamagedTime = 1.0f;
    [Command("BeDamaged.Velocity"), Savable]
    public static Vector2 beDamagedVelocity = new(50, 0.75f);

    private static readonly int ANIMATION_SPEED = Animator.StringToHash("Speed");
    private static readonly int ANIMATION_GAMEOVER = Animator.StringToHash("GameOver");
    private static readonly int ANIMATION_HURT = Animator.StringToHash("Hurt");

    public static bool IsLocalAI(this IPlayer self)
    {
        return self.IsLocal && self.IsAI;
    }
    
    public static bool IsLocalPlayer(this IPlayer self)
    {
        return self.IsLocal && !self.IsAI;
    }

    public static int GetScore(this IPlayer self)
    {
        return self.AliveTime.FormatAsMillisecond() + self.PerfectDodgeTime * 50;
    }

    public static void DodgeLogic(this IPlayer self, Vector3 position)
    {
        if (GameManager.Instance.State <= GameState.gaming)
        {
            AudioManager.PlaySFX(AudioClipKey.SFX_Dodge);
        }
        
        ParticleSystem dodgeEffect = self.GameObject.GetComponent<Player>().dodgeEffect;
        dodgeEffect.transform.position = position;
        dodgeEffect.gameObject.SetActive(true);
        
        if (self.IsLocalAI() || self.IsLocal)
        {
            self.PerfectDodgeTime++;
        }
    }

    public static void TakeDamageLogic(this IPlayer self)
    {
        if (GameManager.Instance.State <= GameState.gaming)
        {
            AudioManager.PlaySFX(AudioClipKey.SFX_PlayerHurt);
        }
        
        self.Player.invincibleStartTime = Time.realtimeSinceStartup;
        
        if (!self.IsLocal)
            return;
        
        if (self.Invincible)
            return;
        
        // Damaged logic
        self.BeDamaged = true;
        self.GameObject.GetComponent<MonoBehaviour>().DelayToDo(beDamagedTime, () => self.BeDamaged = false);
        self.RigidBody.velocity = Vector3.zero;
        var bull = Bull.Instance;
        Vector3 direction = self.Transform.position - bull.transform.position;
        direction = Vector3.Cross(direction, bull.transform.forward);
        direction = Quaternion.Euler(0, direction.y > 0 ? -45 : 45, 0) * bull.transform.forward;
        direction.y = beDamagedVelocity.y;
        self.RigidBody.AddForce(direction * beDamagedVelocity.x);

        self.Invincible = true;

        int hp = self.HP;
        hp--;
        
        self.HP = hp;

        self.Animator.SetTrigger(ANIMATION_HURT);
    }

    public static void GameOverLogic(this IPlayer self)
    {
        if (self.IsLocalPlayer())
        {
            AudioManager.PlaySFX(AudioClipKey.SFX_PlayerKnockOutWee);
            CameraFollower.Instance.SetGameOverVirtualCamera(self.Transform);
            
            if (!XRSportsNetwork.IsRunning)
            {
                Player.gameOverPlayerCount = 1;
            }
            self.Player.direction.SetActive(false);
            self.Player.mark.SetActive(false);
            
            AudioManager.StopBGM(AudioClipKey.BGM_Gaming, 2.0f);
            AudioManager.StopBGM(AudioClipKey.BGM_Title, 2.0f);
        }

        self.RigidBody.velocity = Vector3.zero;
        Vector3 velocity = self.RigidBody.transform.position;
            
        velocity = Vector3.Distance(velocity, Vector3.zero) <= PLAYER_KNOCK_OFF_DIRECTION_RANGE ?
            (velocity - Bull.Instance.transform.position).normalized :
            (Vector3.zero - velocity).normalized;
        velocity.y = .9f;
        velocity *= knockOffVelocity;
        self.RigidBody.AddForce(velocity);
        self.GameObject.GetComponent<MonoBehaviour>().DelayToDo(5.0f, () =>
        {
            if (self.HP <= 0)
            {
                self.RigidBody.useGravity = false;
                self.RigidBody.velocity = Vector3.zero;
            }
        });

        if (self.IsLocal)
        {
            self.Animator.SetBool("GameOver", true);
        }
    }

    public static void NetworkGameOverLogic(this IPlayer self)
    {
        if (!self.Player.alive)
            return;

        switch (self.IsLocal)
        {
            case true when self.Player.alive:
                self.GameOverLogic();
                goto TRY_ADD_GAME_OVER_PLAYER_COUNT;
            
            case false when self.Player.alive:
                TRY_ADD_GAME_OVER_PLAYER_COUNT:
                if (!self.IsAI)
                {
                    Player.gameOverPlayerCount++;
                }
                break;
        }
        
        self.Player.alive = false;

        GameManager gm = GameManager.Instance;
        UIManager.Instance.UpdateUIInformation();
        if (self.IsLocalPlayer())
        {
            if (gm.State < GameState.gameOver)
            {
                gm.State = GameState.gameOver;
            }
        }
        else if (Player.gameOverPlayerCount >= gm.PlayerList.Count(p => !p.IsAI))
        {
            if (gm.State == GameState.settlement)
            {
                gm.State = GameState.result;
            }
        }
    }

    public static void MovementLogic(this IPlayer self, float deltaTime)
    {
        if (!self.Movable || self.BeDamaged)
            return;
        
        // Set moving
        Vector3 rotation = Vector3.zero;
        rotation.y = self.RotateAngle * rotateSpeed * deltaTime;
        self.RigidBody.MoveRotation(self.RigidBody.rotation * Quaternion.Euler(rotation));
        self.MovingSpeed = Mathf.Clamp(self.MovingSpeed * moveSpeed * deltaTime, speedLimit.x, speedLimit.y);
        Vector3 vector = self.RigidBody.velocity;
        Vector3 heading = self.RigidBody.transform.forward * self.MovingSpeed;
        vector.x = heading.x;
        vector.z = heading.z;
        self.RigidBody.velocity = Vector3.Lerp(self.RigidBody.velocity, vector, Time.deltaTime * accelerate);

        if (self.IsLocal)
        {
            self.Animator.SetFloat(ANIMATION_SPEED, self.MovingSpeed / 1);
        }
    }

    public static void InitializeLogic(this IPlayer self, int number)
    {
        if (self.IsLocal)
        {
            self.HP = 3;
            self.MovingSpeed = 
                self.RotateAngle = 
                    self.AliveTime = 0;
            self.PerfectDodgeTime = 0;
            self.Number = number;
            self.Invincible = false;
            // Init position
            Vector3 vector3 = GameManager.Instance.spawnAnchors[number].position;
            self.RigidBody.position = vector3;
            // Init rotation
            vector3 = Vector3.zero - vector3;
            vector3.y = 0;
            vector3.Normalize();
            self.RigidBody.rotation = Quaternion.LookRotation(vector3);
        
            self.Animator.ResetTrigger(ANIMATION_HURT);
            self.Animator.SetBool(ANIMATION_GAMEOVER, false);
            self.BeDamaged = false;
        }

        self.Movable = true;
        self.RigidBody.velocity = Vector3.zero;
        self.RigidBody.useGravity = true;
        self.RigidBody.mass = mass;

        self.Player.Initialize(number);
    }
}

public class Player : MonoBehaviour
{
    public static int gameOverPlayerCount;
    
    public Animator animator;
    public GameObject direction;
    public GameObject mark;
    [SerializeField] private Renderer mainMeshRenderer;
    [SerializeField] private List<SpriteRenderer> directionRenderer;
    [SerializeField] private TextMeshProUGUI txt_name;
    [SerializeField] private Image img_markArrow;
    public ParticleSystem dodgeEffect;
    
    [HideInInspector] public float invincibleStartTime;
    [HideInInspector] public bool alive;
    
    private IPlayer _iPlayer;
    private float _invincibleTimer;
    private Camera _mainCamera;
    private GameManager _gm;

    public IPlayer IPlayer => _iPlayer ??= GetComponent<IPlayer>();
    
    public void Initialize(int number)
    {
        alive = true;
        gameOverPlayerCount = 0;
        _gm = GameManager.Instance;
        var data = _gm.playerNumberData[number];
        mainMeshRenderer.material.mainTexture = data.texture;
        direction.SetActive(false);
        if (IPlayer.IsLocalPlayer())
        {
            foreach (var renderer in directionRenderer)
            {
                renderer.color = data.color;
            }
        }
        _mainCamera = Camera.main;
        mark.SetActive(false);
        if (!IPlayer.IsAI)
        {
            if (IPlayer.IsLocalPlayer())
            {
                CameraFollower.Instance.SetGamingVirtualCamera(number);
                txt_name.SetText("ME");
            }
            else if (XRSportsNetwork.IsRunning)
            {
                txt_name.SetText(GetComponent<IXRSportsNetworkPlayer>().UserName);
            }
            this.WaitUntilToDo(() => _gm.State == GameState.gaming, () =>
            {
                if (IPlayer.IsLocalPlayer())
                {
                    direction.SetActive(true);
                }
                mark.SetActive(true);
            });
            txt_name.color = data.color;
            img_markArrow.color = data.color;
            img_markArrow.gameObject.SetActive(IPlayer.IsLocal);
        }
    }

    void Update()
    {
        switch (IPlayer.Invincible)
        {
            case true :
                if (Time.realtimeSinceStartup - invincibleStartTime >= PlayerUtility.invincibleTime && IPlayer.IsLocal)
                {
                    mainMeshRenderer.enabled = true;
                    IPlayer.Invincible = false;
                }

                if (Time.time - _invincibleTimer > 0.25f)
                {
                    mainMeshRenderer.enabled = !mainMeshRenderer.enabled;
                    _invincibleTimer = Time.time;
                }
                break;
            
            case false when !mainMeshRenderer.enabled:
                mainMeshRenderer.enabled = true;
                break;
        }
    }

    private void LateUpdate()
    {
        if (!IPlayer.IsAI)
        {
            if (_mainCamera)
            {
                mark.transform.LookAt(transform.position + _mainCamera.transform.rotation * Vector3.forward,
                    _mainCamera.transform.rotation * Vector3.up);
            }
        }
    }
}
