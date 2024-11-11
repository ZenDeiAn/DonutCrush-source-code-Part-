using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Airpass.DesignPattern;
using Airpass.Language;
using Airpass.Utility;
using Airpass.XRSports;
using MoreMountains.Feedbacks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class UIManager : SingletonUnity<UIManager>
{
    public List<UI_PlayerInformation> ui_playerInformations;

    [SerializeField, Range(1, 10)] private float numberAnimationDuriation = 3;
    [SerializeField] private List<GameObject> uiPanels;
    [SerializeField] private GameObject pnl_pause;
    [SerializeField] private TextMeshProUGUI txt_aliveTime;
    [SerializeField] private TextMeshProUGUI txt_dodgeTime;
    [SerializeField] private TextMeshProUGUI txt_rank;
    [SerializeField] private TextMeshProUGUI txt_result;
    [SerializeField] private Button btn_restart;
    [SerializeField] private Button btn_room;
    [SerializeField] private Button btn_userselection;
    [SerializeField] private ObjectPool obp_ranking;
    [SerializeField] private MMF_Player mmf_rankingboard;
    [SerializeField] private RectTransform rct_rankingboard;
    [SerializeField] private EventSystemSelectedObjectUpdater essou_result;
    
    private IPlayer _localPlayer;
    private Guid _loadingLock;
    private Rect _rankingBoardOriginalAnchor;

    // Initialize In Game UI.
    public void InitializeGame()
    {
        var players = GameManager.Instance.PlayerList;
        for (int i = 0; i < ui_playerInformations.Count; ++i)
        {
            var ui = ui_playerInformations[i];
            ui.gameObject.SetActive(i < players.Count);
            if (i < players.Count)
            {
                ui.Initialize(players[i]);
            }
        }
    }

    // Initialize Result UI.
    public void InitializeResult()
    {
        // Reset ranking board for while no need to show it.
        rct_rankingboard.anchorMin = new Vector2(_rankingBoardOriginalAnchor.x, _rankingBoardOriginalAnchor.y);
        rct_rankingboard.anchorMax = new Vector2(_rankingBoardOriginalAnchor.width, _rankingBoardOriginalAnchor.height);
        btn_userselection.gameObject.SetActive(XRSports.PrimaryUserInfo.Type == XRSportsUserType.group);
        
        // Sorting player by alive time for get local player rank.
        var playerList = GameManager.Instance.PlayerList;
        playerList = playerList.OrderByDescending(p => p.HP).ThenByDescending(p => p.AliveTime).ToList();
        int rank = 1;
        for (int i = 0; i < playerList.Count; ++i)
        {
            if (playerList[i].IsLocal && !playerList[i].IsLocalAI())
            {
                _localPlayer = playerList[i];
                rank = i + 1;
            }
        }

        // Set text by language.
        GameManager.Instance.localPlayerRank = rank;
        string rankSuffix = rank switch
        {
            1 => "st",
            2 => "nd",
            3 => "rd",
            _ => "th"
        };
            
        txt_rank.SetText($"<b>{rank}</b><size=50%>{rankSuffix}");

        txt_aliveTime.SetText("00:00:000" + 
                              LanguageManager.GetLanguageData("GameUI", "Result_AliveTime_Suffix"));
        
        txt_dodgeTime.SetText("0" + 
                              LanguageManager.GetLanguageData("GameUI", "Result_DodgeTime_Suffix"));
        
        txt_result.SetText("0");
        
        // Update rank board by sorted 'playerList'.
        obp_ranking.RecycleAll();
        List<RankingBoardData> rankList = new List<RankingBoardData>();
        switch (XRSports.XRSportsType)
        {
            case XRSportsRK.TYPE:
                _loadingLock = XRSportsUI.Instance.LoadingBegin();
                int highscore = -1;
                if (int.TryParse(XRSports.UserInfo.Record, out int record))
                {
                    highscore = record;
                }
                int currentscore = GameManager.Instance.LocalPlayer.GetScore();
                // Post result to XRSports database.
                XRSports.RequestPostResultForRanking(XRSports.Option,
                    currentscore,
                    rank =>
                    {
                        XRSportsUI.Instance.LoadingOver(_loadingLock);
                        for (int i = 0; i < XRSports.RankingList.Count; ++i)
                        {
                            UserInfo info = XRSports.RankingList[i];
                            bool isSelf = i == rank - 1;
                            rankList.Add(new RankingBoardData
                            {
                                name = info.Name,
                                nickName = info.NickName,
                                groupName = info.GroupName,
                                isSelf = isSelf,
                                highScore = isSelf ? highscore : -1,
                                rank = i,
                                number = i,
                                record = int.Parse(info.Record)
                            });
                        }
                        if (rank > 10)
                        {
                            rankList.Add(new RankingBoardData
                            {
                                name = XRSports.UserInfo.Name,
                                nickName = XRSports.UserInfo.NickName,
                                groupName = XRSports.UserInfo.GroupName,
                                highScore = highscore,
                                isSelf = true,
                                rank = rank - 1,
                                number = Random.Range(0, 10),
                                record = currentscore
                            });
                        }
                        
                        mmf_rankingboard.PlayFeedbacks();
                        this.DelayToDo(mmf_rankingboard.TotalDuration, 
                            () => UpdateRankingBoard(rankList));
                    },
                    issue =>
                    {
                        XRSportsUI.Instance.PopupActivate(issue, () => 
                            { XRSportsUI.Instance.PopupActivate(false); });
                    });
                break;
        }

        UpdateUIInformation();
    }

    public void UpdateUIInformation()
    {
        if (XRSportsNetwork.IsRunning)
        {
            var playerList = GameManager.Instance.PlayerList.Where(p => !p.IsAI).ToList();
            int playerCount = playerList.Count;
            int gameOverPlayerCount = 0;
            foreach (var player in playerList)
            {
                try
                {
                    if (player.HP <= 0)
                    {
                        gameOverPlayerCount++;
                    }
                }
                catch
                {
                    //ignore
                }
            }
            
            btn_restart.interactable = XRSportsNetwork.IsRoomMaster && gameOverPlayerCount >= playerCount;
            btn_room.gameObject.SetActive(true);
        }
        else
        {
            btn_restart.interactable = true;
            btn_room.gameObject.SetActive(false);
        }
    }

    public void SwitchUIPanel(int index)
    {
        for (int i = 0; i < uiPanels.Count; ++i)
        {
            uiPanels[i].SetActive(i == index);
        }
    }

    public void StartResulting()
    {
        if (_localPlayer != null)
        {
            StartCoroutine(NumberAnimation());
        }
    }

    public void Btn_Home()
    {
        CinemachineManager.Instance.StopAllTimeLine();

        XRSportsUI.Instance.SwitchUIPanel(XRSportsUIType.title);
        GameManager.Instance.State = GameState.none;
        SwitchUIPanel(0);
        
        switch (XRSports.XRSportsType)
        {
            case XRSportsIPC.TYPE:
            case XRSportsOB.TYPE:
                XRSportsNetwork.ShutDownNetwork();
                break;
        }
    }

    public void Btn_Restart()
    {
        CinemachineManager.Instance.StopAllTimeLine();
        if (XRSportsNetwork.IsRunning && XRSportsNetwork.IsRoomMaster)
        {
            XRSportsNetwork.RestartGame();
        }
        else
        {
            XRSports.Instance.startGameEvent?.Invoke();
        }
    }

    public void Btn_UserSelection()
    {
        XRSportsUI.Instance.SetUserSelectionConfirmEvent(() =>
        {
            XRSportsUI.Instance.SwitchUIPanel(XRSportsUIType.none);
            essou_result.SetSelected();
        });
        XRSportsUI.Instance.SwitchUIPanel(XRSportsUIType.userSelection, false);
    }

    public void Btn_Pause()
    {
        if (!pnl_pause.activeSelf)
        {
            pnl_pause.SetActive(true);
            GameManager.Instance.LocalPlayer.Movable = false;
        }
    }

    public void Btn_Resume()
    {
        if (pnl_pause.activeSelf)
        {
            pnl_pause.SetActive(false);
            GameManager.Instance.LocalPlayer.Movable = true;
        }
    }

    public void Btn_Room()
    {
        XRSportsUI.Instance.SwitchUIPanel(XRSportsUIType.room);
        GameManager.Instance.State = GameState.none;
        SwitchUIPanel(0);
        
        switch (XRSports.XRSportsType)
        {
            case XRSportsIPC.TYPE:
            case XRSportsOB.TYPE:
                XRSportsNetwork.LocalPlayer.State = PlayerState.room;
                XRSportsNetwork.SetRoomAvailable(true);
                break;
        }
    }

    private void UpdateRankingBoard(List<RankingBoardData> rankList)
    {
        obp_ranking.RecycleAll();
        for (int i = 0; i < rankList.Count; ++i)
        {
            if (i >= 10)
            {
                obp_ranking.GetObject().GetComponent<Pnl_RankingBoard>().Initialize(rankList[i], true);
            }
            obp_ranking.GetObject().GetComponent<Pnl_RankingBoard>().Initialize(rankList[i]);
        }

        UpdateUIInformation();
    }

    private IEnumerator NumberAnimation()
    {
        float time = 0;
        float timer = 0;
        int dodge = 0;
        float lerp = 0;
        int score = 0;
        int targetScore = _localPlayer.GetScore();
        while (lerp < 1)
        {
            lerp = Mathf.Clamp(lerp, 0, 1);
            time = Mathf.Lerp(time, _localPlayer.AliveTime, lerp);
            txt_aliveTime.SetText(time.FormatAsTimeString() +
                            LanguageManager.GetLanguageData("GameUI", "Result_AliveTime_Suffix"));

            dodge = Mathf.RoundToInt(Mathf.Lerp(dodge, _localPlayer.PerfectDodgeTime, lerp));
            txt_dodgeTime.SetText($"{dodge} " + 
                            LanguageManager.GetLanguageData("GameUI", "Result_DodgeTime_Suffix"));
            
            timer += Time.deltaTime;
            lerp = timer / numberAnimationDuriation;
            yield return null;
        }
        
        txt_aliveTime.SetText(_localPlayer.AliveTime.FormatAsTimeString() +
                              LanguageManager.GetLanguageData("GameUI", "Result_AliveTime_Suffix"));
        txt_dodgeTime.SetText($"{_localPlayer.PerfectDodgeTime} " +
                              LanguageManager.GetLanguageData("GameUI", "Result_DodgeTime_Suffix"));

        lerp = timer = 0;
        while (lerp < 1)
        {
            lerp = Mathf.Clamp(lerp, 0, 1);

            score = Mathf.RoundToInt(Mathf.Lerp(score, targetScore, lerp));
            txt_result.SetText(score.ToString());
            
            timer += Time.deltaTime;
            lerp = timer / numberAnimationDuriation;
            yield return null;
        }
        
        txt_result.SetText(targetScore.ToString());
    }

    void Start()
    {
        _rankingBoardOriginalAnchor = new Rect(
            rct_rankingboard.anchorMin.x,
            rct_rankingboard.anchorMin.y,
            rct_rankingboard.anchorMax.x,
            rct_rankingboard.anchorMax.y);
    }
}
