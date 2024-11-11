using System;
using System.Linq;
using System.Collections.Generic;
using Airpass.AudioManager;
using Airpass.DesignPattern;
using Airpass.XRSports;
using Cinemachine;
using MoreMountains.Feedbacks;
using QFSW.QC;
using UnityEngine;

public class GameManager : ProcessorEternal<GameManager, GameState>
{
    // Game maximum player count.
    public const int MAX_PLAYER = 4;

    #region QC Console Config Values.
    [Command("Option.Bull.AimingTimeList", "Time of bull aiming."), Savable]
    public List<float> option_bull_aiming_time_list = new() { 1.5f, 1.25f, 1.0f };
    
    [Command("Option.Bull.SpeedList", "Bull rush speed list by difficulty."), Savable]
    public List<float> option_bull_speed_list = new() { 2.0f, 3.0f, 5.0f };

    [Command("Option.Bull.AccelerateList", "Increase speed of bull rushing list by difficulty."), Savable]
    public List<float> option_bull_accelerate_list = new() { 0.2f, 0.3f, 0.5f };
    
    [Command("Option.Player.MovementSpeedList", "Player movement speed list by difficulty."), Savable]
    public List<float> option_player_speed_movement_list = new() { 100.0f, 75.0f, 50.0f };

    [Command("Option.Player.RotateSpeedList", "Player rotate speed list by difficulty."), Savable]
    public List<float> option_player_speed_rotate_list = new() { 20.0f, 15.0f, 10.0f };
    
    [Command("Option.Player.MovementSpeedLimitList", "Player movement speed limit list by difficulty."), Savable]
    public List<Vector2> option_player_speed_limit_list = new() { new(-7.0f, 7.0f), new(-5.0f, 5.0f), new(-3.0f, 3.0f) };

    [Command("Bull.BreakTime", "Time of bull take a break after knock the wall."), Savable]
    public float bullBreakTime = 1.0f;

    [Command("SizeOfBull", "The model size of Bull."), Savable]
    public float sizeOfBull = 2.0f;

    [Command("SizeOfPlayer", "The model size of Player"), Savable]
    public float sizeOfPlayer = 1.5f;

    [Command("SizeOfWall", "The model size of Donut wall surrounding player."), Savable]
    public float sizeOfWall = 10;

    [Command("CameraPosition", "The main camera position."), Savable]
    public Vector3 cameraPosition = new(0, 27.5f, -20f);

    [Command("CameraRotation", "The main camera rotation."), Savable]
    public Vector3 cameraRotation = new(58, 0, 0);
    #endregion

    // For limit Bull's movement range. (Used with 'sizeOfWall' variable.)
    public float rangeOfWall = 1.9f;

    // Player initialize position.
    public List<Transform> spawnAnchors;
    // Player data like color or profile sprite by Index number.
    public List<PlayerNumberData> playerNumberData;
    // MoreMountains Feedback Player for while bull knock player.
    public MMF_Player mmfp_shake;
    // MoreMountains Feedback Player for while bull knock wall.
    public MMF_Player mmfp_crush;
    [SerializeField] private GameObject prefabPlayerXRRK;
    [SerializeField] private GameObject prefabPlayerAIXRRK;
    [SerializeField] private GameObject prefabPlayerAIIPC;
    [SerializeField] private GameObject prefabPlayerAIOB;
    [SerializeField] private GameObject prefabBullXRRK;
    [SerializeField] private GameObject prefabBullIPC;
    [SerializeField] private GameObject prefabBullOB;
    // Donut wall's transform.
    [SerializeField] private Transform wall;
    // The bull instance actor(Fake Bull) in scene for timeline.
    [SerializeField] private Transform bullCinematic;

    public GameOption gameOption;

    // Local Player's rank after game result for OnlineBattle Ranking Upload.
    [HideInInspector] public int localPlayerRank;

    // List of AI Player IPlayer interface.
    public List<IPlayer> aiPlayerList = new();

    // XRSports Loading UI Guid.
    private Guid _loadingLock;

    // Current in gaming players(contains AI Player) IPlayer interface List.
    public List<IPlayer> PlayerList { get; set; } = new();
    // Current in gaming Local Player.
    public IPlayer LocalPlayer { get; set; }

    public void ChangeState(string state)
    {
        if (Enum.TryParse(state, true, out GameState parsed))
        {
            State = parsed;
        }
    }

    public void GameOver()
    {
        // Check is all Actual Player(!AI Player) finished the game. If not then just get into spectator mode.(For OB, IPC)
        State = Player.gameOverPlayerCount >= PlayerList.Count(p => !p.IsAI) ? 
            GameState.result : 
            GameState.settlement;
    }

    public void XRSportsStartGameEvent()
    {
        // Close the XRSportsUI.
        XRSportsUI.Instance.SwitchUIPanel(XRSportsUIType.none);

        // Change the wall's size by config.(QC)
        wall.localScale = Vector3.one * sizeOfWall;

        // Set gaming virtual camera's position by config.(QC)
        CinemachineVirtualCamera virtualCamera = CameraFollower.Instance.GetVirtualCamera(GameState.gaming);
        virtualCamera.transform.position = cameraPosition;
        virtualCamera.transform.eulerAngles = cameraRotation;
        
        switch (XRSports.XRSportsType)
        {
            case XRSportsRK.TYPE:
                // Get Best Record first in Ranking mode for result page new record.
                XRSports.RequestBestRecord(XRSports.Option, XRSportsRK.TYPE, record => XRSports.UserInfo.Record = record);
                goto case XRSportsXR.TYPE;

            case XRSportsXR.TYPE:
                // Spawn Bull Instance while there's no any of it.
                if (Bull.Instance == null)
                {
                    Instantiate(prefabBullXRRK, Vector3.zero, Quaternion.LookRotation(Vector3.back))
                        .GetComponent<IBull>();
                }
                
                // Spawn Local Player.
                if (PlayerList.Count < 1)
                {
                    PlayerList.Add(Instantiate(prefabPlayerXRRK).GetComponent<IPlayer>());
                }

                // Set scale by config.(QC)
                PlayerList[0].Transform.localScale = Vector3.one * sizeOfPlayer;

                // Spawn AI Player.
                for (int i = 1; i < MAX_PLAYER; ++i)
                {
                    if (PlayerList.Count < MAX_PLAYER)
                    {
                        PlayerList.Add(Instantiate(prefabPlayerAIXRRK).GetComponent<IPlayer>());
                        aiPlayerList.Add(PlayerList[i]);
                    }

                    PlayerList[i].Transform.localScale = Vector3.one * sizeOfPlayer;
                }

                // Change State to prepare for pre-gaming scenario(Timeline).
                State = GameState.prepare;
                break;

            case XRSportsIPC.TYPE:
            case XRSportsOB.TYPE:
                // Set Network Time Syncer Complete Event for Network Game Start logic delegate.
                XRSportsNetwork.NetworkTimeSyncer.SetCompleteEvent(NetworkStartGame);

                // RoomMaster Start the Network Time Syncer.
                if (XRSportsNetwork.IsRoomMaster)
                {
                    for (int i = 0; i < aiPlayerList.Count; ++i)
                    {
                        aiPlayerList[i].GameObject.GetComponent<INetworkPlayerAI>().Rpc_SetActive(
                            i < MAX_PLAYER - XRSportsNetwork.PlayerList.Count);
                    }
                    
                    XRSportsNetwork.NetworkTimeSyncer.StartSyncTimer(0.25f);
                }
                break;
        }

        // Initialize the bull's transform.
        Bull.Instance.ResetTransform();
        // Change the Scenario Bull's scale by config.(QC)
        bullCinematic.localScale = Vector3.one * sizeOfBull;
    }

    private void NetworkStartGame()
    {
        // Change Player State in XRSports Network to gaming state.
        XRSportsNetwork.LocalPlayer.State = PlayerState.gaming;
        // Set current room not join-able for room list.
        XRSportsNetwork.SetRoomAvailable(false);

        // UpdatePlayer list. 
        PlayerList.Clear();
        LocalPlayer = XRSportsNetwork.LocalPlayer.GameObject.GetComponent<IPlayer>();
        // Add other players to the list.
        foreach (var player in XRSportsNetwork.PlayerList)
        {
            player.GameObject.transform.localScale = sizeOfPlayer * Vector3.one;
            PlayerList.Add(player.GameObject.GetComponent<IPlayer>());
        }
        // Add AI Player to the list.
        for (int i = 0; i < aiPlayerList.Count; ++i)
        {
            var player = aiPlayerList[i];

            // Active AI Player by left needed ai player amount.
            if (player.GameObject.activeSelf)
            {
                player.Transform.localScale = sizeOfPlayer * Vector3.one;
                PlayerList.Add(player);
            }
        }

        // Change State to prepare for pre-gaming scenario(Timeline).
        State = GameState.prepare;
    }

    private void OnNetworkHostRoom(bool successful)
    {
        // Only process while room host succeed.
        if (!successful)
            return;

        // Spawn Bull AI and AI Player by IPC mode and OB mode.
        switch (XRSports.XRSportsType)
        {
            case XRSportsIPC.TYPE:
                // Wait for room master joined the room that host by self(weird fishnet...)
                var nc = XRSportsIPC.NetworkManager.ClientManager.Connection;
                // Spawn Bull gameObject.
                if (Bull.Instance == null)
                {
                    Instantiate(prefabBullIPC).GetComponent<IBull>();
                    XRSportsIPC.NetworkManager.ServerManager.Spawn(Bull.Instance.gameObject, nc);
                }

                // Spawn AI Players.
                for (int i = 0; i < MAX_PLAYER - XRSportsNetwork.PLAYER_MIN; ++i)
                {
                    var aiPlayer = Instantiate(prefabPlayerAIIPC);
                    XRSportsIPC.NetworkManager.ServerManager.Spawn(aiPlayer, nc);
                }
                break;

            case XRSportsOB.TYPE:
                // Spawn Bull gameObject.
                if (Bull.Instance == null)
                {
                    XRSportsOB.NetworkRunner.Spawn(prefabBullOB).GetComponent<IBull>();
                }

                // Spawn AI Players.
                for (int i = 0; i < MAX_PLAYER - XRSportsNetwork.PLAYER_MIN; ++i)
                {
                    XRSportsOB.NetworkRunner.Spawn(prefabPlayerAIOB);
                }

                break;
        }
    }

    private void ClearGameData()
    {
        PlayerList.Clear();
        aiPlayerList.Clear();
        foreach (var info in UIManager.Instance.ui_playerInformations)
        {
            info.player = null;
        }
    }

    private void OnNetworkQuitRoom(bool isNormal)
    {
        State = GameState.none;
        UIManager.Instance.SwitchUIPanel(0);
        ClearGameData();
    }

    // Airpass Processor's State Enable event.
    void Enable_None()
    {
        if (XRSportsNetwork.IsRunning)
            return;

        // Clear GameObjects only local mode (XR, RK)
        foreach (var player in PlayerList)
        {
            try
            {
                if (player != null && player.GameObject != null)
                {
                    DestroyImmediate(player.GameObject);
                }
            }
            catch
            {
                // ignored
            }
        }

        try
        {
            if (Bull.Instance != null)
            {
                DestroyImmediate(Bull.Instance.gameObject);
            }
        }
        catch
        {
            // ignored
        }

        ClearGameData();
        
        // Play title bgm.
        AudioManager.StopBGM(AudioClipKey.BGM_Result);
        AudioManager.StopBGM(AudioClipKey.BGM_Gaming);
        AudioManager.PlayBGM(AudioClipKey.BGM_Title);
    }

    // Airpass Processor's State Enable event.
    void Enable_Prepare()
    {
        // Disable Actual Bull and initialize it for scenario.
        if (Bull.Instance != null)
        {
            Bull.Instance.gameObject.SetActive(false);

            // Only init by RoomMaster while is IPC, OB mode.
            if (XRSportsNetwork.IsRunning && XRSportsNetwork.IsRoomMaster)
            {
                Bull.Instance.transform.position = Vector3.zero;
                Bull.Instance.ResetTransform();
                for (int i = 0; i < PlayerList.Count; i++)
                {
                    PlayerList[i].Initialize(i);
                }
            }
            // If is XR, RK mode.
            else if (!XRSportsNetwork.IsRunning)
            {
                // Generate a random number list.
                List<int> numberList = new() { 2, 3, 1, 0 };
                int n = numberList.Count;
                System.Random random = new System.Random();
                while (n > 1)
                {
                    n--;
                    int k = random.Next(n + 1);
                    (numberList[k], numberList[n]) = (numberList[n], numberList[k]);
                }
                for (int i = 0; i < PlayerList.Count; i++)
                {
                    PlayerList[i].Initialize(numberList[i]);
                }
            }
        }
        
        // Stop all bgm with fade.
        AudioManager.StopAllBGM(0.5f);
    }
    
    // Airpass Processor's State Enable event.
    void Enable_Gaming()
    {
        // Apply option.
        if (Enum.TryParse(XRSports.Option, out GameOption option))
        {
            gameOption = option;
        }

        // Set bull's speed properties by option.
        Bull bull = Bull.Instance;
        if (bull != null)
        {
            int optionIndex = (int)gameOption;
            if (option_bull_aiming_time_list.Count > optionIndex)
            {
                bull.aimingTime = option_bull_aiming_time_list[optionIndex];
            }
            if (option_bull_speed_list.Count > optionIndex)
            {
                bull.speed = option_bull_speed_list[optionIndex];
            }
            if (option_bull_accelerate_list.Count > optionIndex)
            {
                bull.accelerate = option_bull_accelerate_list[optionIndex];
            }
            if (option_player_speed_movement_list.Count > optionIndex)
            {
                PlayerUtility.moveSpeed = option_player_speed_movement_list[optionIndex];
            }
            if (option_player_speed_rotate_list.Count > optionIndex)
            {
                PlayerUtility.rotateSpeed = option_player_speed_rotate_list[optionIndex];
            }
            if (option_player_speed_limit_list.Count > optionIndex)
            {
                PlayerUtility.speedLimit = option_player_speed_limit_list[optionIndex];
            }

            // Spawn bull.
            bull.gameObject.SetActive(true);

            bull.Initialize();
        }

        // Initialize all player for gameStart.
        foreach (var player in PlayerList)
        {
            player.InitializeForGameStart();
        }
    }

    // Airpass Processor's State Update event.
    void Update_Gaming()
    {
        // Escape function key while gaming for pause ui.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UIManager.Instance.Btn_Pause();
        }
    }

    // Airpass Processor's State enable event.
    void Enable_Result()
    {
        // Initialize UI result page.
        UIManager.Instance.InitializeResult();
        
        // Post result to XRSports DB. 
        if (XRSports.XRSportsType != XRSportsRK.TYPE)
        {
            // Async request request post result.
            XRSports.RequestPostResultForNotRanking(XRSports.Option, localPlayerRank, LocalPlayer.GetScore(), XRSports.CurrentTypeInstance.Type, null);
        }
        
        AudioManager.PlayBGM(AudioClipKey.BGM_Result);
    }

    protected override void Initialization()
    {
        base.Initialization();

        // Bind events to StateEnableEvent let Scenario play and VirtualCamera setting changed by Game state.
        StateEnableEvent += state =>
        {
            CameraFollower.Instance.SetCameraByGameState(state);
            CinemachineManager.Instance.PlayTimeLine((int)state);
        };
        
        // Bind network events.
        XRSportsNetwork.OnHostRoomCallbackEvent += OnNetworkHostRoom;
        XRSportsNetwork.OnQuitRoomCallbackEvent += OnNetworkQuitRoom;
    }
}

[Serializable]
public enum GameState
{
    none,           // Do nth state.
    prepare,        // State before gaming for scenario.
    pause,          // Not using.
    gaming,
    gameOver,       // State before result page or spectator state. 
    settlement,     // Spectator state foe multi-player mode.
    result          // State for result page and all player game over.
}

public enum GameOption
{
    easy,
    normal,
    hard
}

[Serializable]
public struct PlayerNumberData
{
    public Color color;
    public Sprite profile;
    public Sprite profile_dead;
    public Texture texture;
}