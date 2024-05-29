using Photon.Realtime;
using ExitGames.Client.Photon;
using Photon.Pun;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

public class MatchManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public static MatchManager Instance { get; private set; }

    public GameState State { get; private set; }

    [SerializeField] private int killsToWin;

    private List<PlayerInfo> allPlayers = new List<PlayerInfo>();
    private int playerIndex;

    private void Awake()
    {
        Instance = this;
        SetGameState(GameState.Waiting);
    }

    private void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            SceneManager.LoadScene((int)SceneCode.MainMenu);
            return;
        }

        NewPlayerSend(PhotonNetwork.NickName);
        SetGameState(GameState.Playing);
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code < 200)
        {
            EventCodes theEvent = (EventCodes)photonEvent.Code;
            object[] data = (object[])photonEvent.CustomData;

            switch(theEvent)
            {
                case EventCodes.NewPlayer:
                    NewPlayerReceive(data);
                    break;

                case EventCodes.ListPlayer:
                    ListPlayerReceive(data); 
                    break;

                case EventCodes.UpdateStats:
                    UpdateStatsReceive(data);
                    break;
            }
        }
    }

    public override void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene((int)SceneCode.MainMenu);
    }

    private void NewPlayerSend(string nickName)
    {
        object[] package = new object[5];
        package[0] = nickName;
        package[1] = PhotonNetwork.LocalPlayer.ActorNumber;
        package[2] = 0;
        package[3] = 0;
        package[4] = 0;

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.NewPlayer,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
            new SendOptions { Reliability = true }
        );
    }
    
    private void NewPlayerReceive(object[] dataReceived)
    {
        PlayerInfo newPlayer = new PlayerInfo(dataReceived);

        allPlayers.Add(newPlayer);

        ListPlayerSend();
    }

    private void ListPlayerSend()
    {
        object[] package = new object[allPlayers.Count];

        for (int i = 0; i < allPlayers.Count; i++)
        {
            object[] piece = new object[5];
            piece = allPlayers[i].GetData();
            package[i] = piece;
        }

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.ListPlayer,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
        );
    }

    private void ListPlayerReceive(object[] dataReceived)
    {
        allPlayers.Clear();

        for(int i = 0; i < dataReceived.Length; i++)
        {
            object[] piece = (object[])dataReceived[i];

            PlayerInfo newPlayer = new PlayerInfo(piece);
            allPlayers.Add(newPlayer);

            if (newPlayer.Actor == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                playerIndex = i;
            }
        }

        UIController.Instance.CreateLeaderboard(allPlayers);
    }

    public void UpdateStatsSend(int senderActor, int statToUpdate, int statIncrement)
    {
        object[] package = new object[] { senderActor, statToUpdate, statIncrement };

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.UpdateStats,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
        );
    }

    private void UpdateStatsReceive(object[] dataReceived)
    {
        int actor = (int)dataReceived[0];
        int statIndex = (int)dataReceived[1];
        int amount = (int)dataReceived[2];

        for(int i = 0; i < allPlayers.Count; i++)
        {
            if (allPlayers[i].Actor == actor)
            {
                allPlayers[i].UpdateStat(statIndex, amount);
                UIController.Instance.SetKillDeathCount();
                break;
            }
        }

        if(statIndex == 0)
        {
            UIController.Instance.UpdateLeaderboard(allPlayers);
        }
    }

    public PlayerInfo GetLocalPlayerInfo()
    {
        return allPlayers[playerIndex];
    }

    public void CheckScore(PlayerInfo topPlayer)
    {
        if(topPlayer?.Kills >= killsToWin && !(State == GameState.Ending))
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.DestroyAll();
            }

            EndGame();
        }
    }

    private void EndGame()
    {
        SetGameState(GameState.Ending);

        UIController.Instance.EndGame();
    }

    public void SetGameState(GameState gameState)
    {
        State = gameState;
    }
}
