using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIController : MonoBehaviour
{
    public static UIController Instance { get; private set; }

    public bool ShowCursor { get; set; }

    public int ammoCount;
    public TMP_Text deathScreenText;

    [SerializeField] private TMP_Text ammoCountText, respawnTimeText, healthText, killsCount, deathsCount, endScreenText, pingText;
    [SerializeField] private GameObject deathScreen, aliveScreen, endScreen, pauseScreen;
    [SerializeField] private Transform leaderboard;
    [SerializeField] private LeaderboardTemplate leaderboardTemplate;
    [SerializeField] private Transform healthBar;
    [SerializeField] private int respawnTime, endScreenTime;
    [SerializeField] private Sprite lowPingSprite, mediumPingSprite, highPingSprite;
    [SerializeField] private Image pingImage;

    private List<LeaderboardTemplate> allLeaderboards = new List<LeaderboardTemplate>();
    private float pingUpdateTime = 0.5f, pingUpdateCounter;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        aliveScreen.SetActive(true);
        deathScreen.SetActive(false);
        endScreen.SetActive(false);
    }

    private void Update()
    {
        pingUpdateCounter += Time.deltaTime;
        if(pingUpdateCounter >= pingUpdateTime)
        {
            pingUpdateTime = 0f;
            SetPing();
        }
    }

    //Changing cursor visibility
    public void ChangeCursorVisibility(bool visible)
    {
        if (visible)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
    public void SetAmmoCount()
    {
        ammoCountText.text = ammoCount.ToString();
    }

    public void PlayerKilled(bool killed)
    {
        if(MatchManager.Instance.State == GameState.Playing)
        {
            deathScreen.SetActive(killed);
            aliveScreen.SetActive(!killed);

            if (killed)
            {
                string respawnText = "Respawn in ";
                StartCoroutine(TextCountdown(respawnTime, respawnTimeText, respawnText));
            }
        }
    }

    private IEnumerator TextCountdown(int countdownTime, TMP_Text textObject, string countdownText)
    {
        while(countdownTime >= 0)
        {
            textObject.text = countdownText + countdownTime.ToString();
            yield return new WaitForSeconds(1f);
            countdownTime--;
        }

        if(MatchManager.Instance.State == GameState.Ending)
        {
            PhotonNetwork.AutomaticallySyncScene = false;
            PhotonNetwork.LeaveRoom();
        }
    }

    public void SetHealthUI(int currentHealth, int maxHealth)
    {
        Vector3 healthBarSCale = new Vector3 ((float)currentHealth/maxHealth, 1f, 1f);
        healthBar.localScale = healthBarSCale;
        healthText.text = currentHealth.ToString() + "/" + maxHealth.ToString();
    }

    public void SetKillDeathCount()
    {
        killsCount.text = MatchManager.Instance.GetLocalPlayerInfo().Kills.ToString();
        deathsCount.text = MatchManager.Instance.GetLocalPlayerInfo().Deaths.ToString();
    }

    public void CreateLeaderboard(List<PlayerInfo> allPlayers)
    {
        for(int i = 0; i < allPlayers.Count; i++)
        {
            LeaderboardTemplate newLeaderBoard =  Instantiate(leaderboardTemplate, leaderboardTemplate.transform.parent);
            PlayerInfo player = allPlayers[i];
            newLeaderBoard.SetTemplateData(player.NickName, player.Kills, player.Deaths, player.Damage, i);
            newLeaderBoard.gameObject.SetActive(true);
            allLeaderboards.Add(newLeaderBoard);
        }
    }

    public void UpdateLeaderboard(List<PlayerInfo> allPlayers)
    {
        foreach(var leaderboard in allLeaderboards)
        {
            Destroy(leaderboard.gameObject);
        }
        allLeaderboards.Clear();

        List<PlayerInfo> sortedPlayers = SortPlayers(allPlayers);
        CreateLeaderboard(sortedPlayers);
        MatchManager.Instance.CheckScore(sortedPlayers[0]);
    }

    private List<PlayerInfo> SortPlayers(List<PlayerInfo> players)
    {
        List<PlayerInfo> sortedPlayers = new List<PlayerInfo>();

        while(sortedPlayers.Count < players.Count)
        {
            int highestKill = -1;
            PlayerInfo selectedPlayer = players[0];

            foreach(PlayerInfo player in players)
            {
                if(player.Kills > highestKill && !sortedPlayers.Contains(player))
                {
                    highestKill = player.Kills;
                    selectedPlayer = player;
                }
            }

            sortedPlayers.Add(selectedPlayer);
        }

        return sortedPlayers;
    }

    public void EndGame()
    {
        leaderboard.SetParent(endScreen.transform);

        aliveScreen.SetActive(false);
        deathScreen.SetActive(false);
        endScreen.SetActive(true);

        ChangeCursorVisibility(true);

        string endText = "Exiting game in ";
        StartCoroutine(TextCountdown(endScreenTime, endScreenText, endText));
    }

    private void SetPing()
    {
        int ping = PhotonNetwork.GetPing();
        string pingValue = ping <= 999 ? ping.ToString() : "999+";
        pingText.text = pingValue;



        if(ping < 200)
        {
            pingImage.sprite = lowPingSprite;
            pingText.color = Color.green;
        }
        else if(ping < 500)
        {
            pingImage.sprite = mediumPingSprite;
            pingText.color = Color.yellow;
        }
        else if(ping > 500)
        {
            pingImage.sprite = highPingSprite;
            pingText.color = Color.red;
        }
    }

    public void PauseGame()
    {
        ShowCursor = true;
        ChangeCursorVisibility(true);
        pauseScreen.SetActive(true);

        MatchManager.Instance.SetGameState(GameState.Paused);
    }

    public void ResumeGame()
    {
        ShowCursor = false;
        ChangeCursorVisibility(false);
        pauseScreen.SetActive(false);

        MatchManager.Instance.SetGameState(GameState.Playing);
    }

    public void GoToMainMenu()
    {
        PhotonNetwork.AutomaticallySyncScene = false;
        SceneManager.LoadScene((int)SceneCode.MainMenu);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
