using Airpass.Utility;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class Pnl_RankingBoard : MonoBehaviour
{
    //[SerializeField] Image img_rank;
    [SerializeField] TextMeshProUGUI txt_rankNumber;
    //[SerializeField] Image img_profileIcon;
    [SerializeField] GameObject selfMark;
    [SerializeField] TextMeshProUGUI txt_groupName;
    [SerializeField] TextMeshProUGUI txt_name;
    [SerializeField] TextMeshProUGUI txt_nickName;
    [SerializeField] TextMeshProUGUI txt_record;
    [SerializeField] TextMeshProUGUI txt_highScore;
    [SerializeField] GameObject newRecord;
    [SerializeField] GameObject highScoreObject;
    [SerializeField] GameObject outOfRankLine;
    [SerializeField] GameObject mainBody;
    [SerializeField] CanvasGroup canvasGroup;

    //[SerializeField] List<Sprite> rankNumberIcons;
    //[SerializeField] List<Sprite> profileIcons;

    RectTransform rectTransform;
    Vector2 originalSizeDelta;

    public void Initialize(RankingBoardData data, bool isOutOfLine = false)
    {
        if (isOutOfLine)
        {
            mainBody.SetActive(false);
            outOfRankLine.SetActive(true);
            transform.SetSiblingIndex(data.rank);
        }
        else
        {
            transform.SetSiblingIndex(data.rank < 10 ? data.rank : data.rank + 1);
            int index = transform.GetSiblingIndex();
            //img_profileIcon.sprite = profileIcons[Mathf.Clamp(data.number, 0, profileIcons.Count - 1)];
            //goldRank.SetActive(index == 0);
            //img_rank.color = Color.clear;
            txt_rankNumber.text = (data.rank + 1).ToString();
            /*if (index < 3)
            {
                img_rank.color = Color.white;
                img_rank.sprite = rankNumberIcons[index];
                txt_rankNumber.text = string.Empty;
            }*/
            mainBody.SetActive(true);
            outOfRankLine.SetActive(false);
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
                originalSizeDelta = rectTransform.sizeDelta;
            }
            selfMark.SetActive(data.isSelf);
            //rectTransform.sizeDelta = data.isSelf ? new Vector2(originalSizeDelta.x * 1.1f, originalSizeDelta.y * 1.1f) : originalSizeDelta;
            txt_groupName.text = data.groupName;
            txt_name.text = data.name;
            txt_nickName.text = data.nickName;
            txt_record.text = data.record == -1 ? "..." : data.record.ToString();
            
            txt_highScore.text = data.highScore.ToString();
            highScoreObject.SetActive(data.highScore != -1);
            
            newRecord.SetActive(data.isSelf && (!highScoreObject.activeSelf || data.record > data.highScore));
        }
        
        canvasGroup.alpha = 0;
        this.DelayToDo(transform.GetSiblingIndex() * 0.25f, () =>
        {
            canvasGroup.DOFade(1, 1);
        });
    }
}

public struct RankingBoardData
{
    public int rank;
    public int number;
    public bool isSelf;
    public string groupName;
    public string name;
    public string nickName;
    public int record;
    public int highScore;
}