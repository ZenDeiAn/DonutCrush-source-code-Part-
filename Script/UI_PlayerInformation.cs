using Airpass.Utility;
using LeTai.TrueShadow;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_PlayerInformation : MonoBehaviour
{
    [SerializeField] private Image img_profile;
    [SerializeField] private Image img_profile_dead;
    [SerializeField] private Image img_hpBar;
    [SerializeField] private TextMeshProUGUI txt_timer;
    [SerializeField] private GameObject dead;
    [SerializeField] private GameObject selfMark;
    [SerializeField] private TrueShadow selfMarkShadow;

    public IPlayer player;

    public void Initialize(IPlayer iPlayer)
    {
        var data = GameManager.Instance.playerNumberData[iPlayer.Number];
        player = iPlayer;
        img_profile.sprite = data.profile;
        img_profile_dead.sprite = data.profile_dead;
        selfMarkShadow.Color = data.color;
        selfMark.gameObject.SetActive(iPlayer.IsLocalPlayer());
        dead.gameObject.SetActive(false);
    }

    void Update()
    {
        try
        {
            if (player == null)
                return;
        
            img_hpBar.fillAmount = player.HP / (float)PlayerUtility.PLAYER_HP_MAX;
            txt_timer.SetText(player.AliveTime.FormatAsTimeString());
            if (player.HP <= 0 && !dead.activeSelf)
            {
                dead.gameObject.SetActive(true);
            }
        }
        catch
        {
            player = null;
        }
    }
}
