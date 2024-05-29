using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardTemplate : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNameText, killsText, deathsText, damageText;
    [SerializeField] private Color gold, silver, bronze, regular;
    [SerializeField] private Image background;

    public void SetTemplateData(string playerName, int kills, int deaths, int damage, int position)
    {
        playerNameText.text = playerName;
        killsText.text = kills.ToString();
        deathsText.text = deaths.ToString();
        damageText.text = damage.ToString();
        switch (position)
        {
            case 0:
                background.color = gold;
                break;

            case 1:
                background.color = silver;
                break;

            case 2:
                background.color = bronze;
                break;

            default:
                background.color = regular;
                break;
        }
    }
}
