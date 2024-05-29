using UnityEngine;
using TMPro;
using Photon.Realtime;

public class RoomTemplate : MonoBehaviour
{
    [SerializeField] private TMP_Text roomNameText, playerJoinedCount;

    public RoomInfo roomInfo;

    public void SetTemplateData(RoomInfo info)
    {
        roomInfo = info;

        roomNameText.text = roomInfo.Name;
        playerJoinedCount.text = roomInfo.PlayerCount.ToString() + " / " + roomInfo.MaxPlayers.ToString();
    }

    public void JoinRoom()
    {
        Launcher.Instance.JoinRoom(roomInfo);
    }
}
