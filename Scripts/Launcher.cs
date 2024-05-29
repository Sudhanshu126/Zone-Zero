using UnityEngine;
using Photon.Pun;
using TMPro;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.TextCore.Text;

public class Launcher : MonoBehaviourPunCallbacks
{
    //Public variables
    public static Launcher Instance { get; private set; }

    //Private variables
    [SerializeField] private GameObject loadingScreen, menuButtons, createRoomScreen, roomScreen, roomBrowserScreen, startButton, disabledStartButton, optionsMenu;
    [SerializeField] private TMP_Text loadingText, roomName, roomCreateErrorText, nicknameErrorText;
    [SerializeField] private TMP_InputField roomNameField, nicknameField;
    [SerializeField] private int maxBrowserRoomCount, matchPlayerLimit;
    [SerializeField] private RoomTemplate roomTemplate;
    [SerializeField] private PlayerTemplate playerTemplate;
    [SerializeField] private string levelToPlay;
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private List<CharacterTemplate> characterTemplates;
    [SerializeField] private Color selectedCharacterColor;

    private List<RoomTemplate> allRoomTemplates = new List<RoomTemplate>();
    private List<PlayerTemplate> allPlayerTemplates = new List<PlayerTemplate>();
    private Dictionary<string, RoomInfo> cachedRoomsList = new Dictionary<string, RoomInfo>();
    private Character_SO selectedCharacterSO;

    /*---------- Standard Methods ----------*/

    //Awake method
    private void Awake()
    {
        Instance = this;
    }

    //Start method
    private void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            DisableAllMenus();

            loadingText.text = "Connecting to network...";
            loadingScreen.SetActive(true);

            PhotonNetwork.ConnectUsingSettings();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetPlayerData();

        if(selectedCharacterSO == null)
        {
            SelectCharacter(characterTemplates[0]);
        }
        else
        {
            SetSelectedCharacter(selectedCharacterSO);
        }
    }

    /*---------- UI Methods ----------*/

    //Disable all menus
    private void DisableAllMenus()
    {
        menuButtons.SetActive(false);
        loadingScreen.SetActive(false);
        createRoomScreen.SetActive(false);
        roomScreen.SetActive(false);
        roomBrowserScreen.SetActive(false);
        optionsMenu.SetActive(false);
    }

    //Open create room panel
    public void OpenCreateRoom()
    {
        if (CheckNickname())
        {
            roomCreateErrorText.gameObject.SetActive(false);
            LoadUI(createRoomScreen);
        }
    }

    //Creates the room (Create room button)
    public void CreateRoom()
    {
        if (!string.IsNullOrEmpty(roomNameField.text))
        {
            loadingText.text = "Creating Room...";
            LoadUI(loadingScreen);

            RoomOptions options = new RoomOptions();
            options.MaxPlayers = matchPlayerLimit;

            PhotonNetwork.CreateRoom(roomNameField.text, options);
        }
    }

    //Open room browser panel
    public void OpenRoomBrowser()
    {
        if (CheckNickname())
        {
            LoadUI(roomBrowserScreen);
        }
    }

    //Join a room
    public void JoinRoom(RoomInfo joinRoomInfo)
    {
        PhotonNetwork.NickName = nicknameField.text;
        PhotonNetwork.JoinRoom(joinRoomInfo.Name);

        DisableAllMenus();
        loadingText.text = "Joining room...";
        loadingScreen.SetActive(true);
    }

    //Leave the room
    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();

        loadingText.text = "Leaving room...";
        LoadUI(loadingScreen);
    }

    //Loading UIs
    public void LoadUI(GameObject loadingUI)
    {
        DisableAllMenus();
        LoadUIOnTop(loadingUI);
    }

    //Loading UIs on top
    public void LoadUIOnTop(GameObject loadingUI)
    {
        loadingUI.SetActive(true);
    }

    //Start Game
    public void StartGame()
    {
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.LoadLevel(levelToPlay);
    }

    //Quit application
    public void Exit()
    {
        SavePlayerData();
        Application.Quit();
    }

    /*---------- PUN Server Methods ----------*/

    //Connection to master
    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    //On joined the lobby
    public override void OnJoinedLobby()
    {
        menuButtons.SetActive(true);
        loadingScreen.SetActive(false);
    }

    //On joined a room
    public override void OnJoinedRoom()
    {
        LoadUI(roomScreen);

        roomName.text = PhotonNetwork.CurrentRoom.Name;
        PhotonNetwork.NickName = nicknameField.text;

        UpdatePlayersInRoom();

        SetScenePersistData();
        SavePlayerData();
    }

    //Player left the room
    public override void OnLeftRoom()
    {
        LoadUI(menuButtons);
    }

    //New room created or removed
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        UpdateCachedRoomList(roomList);
    }

    //Another player joined in room
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        PlayerTemplate newPlayerTemplate = Instantiate(playerTemplate, playerTemplate.transform.parent);
        newPlayerTemplate.SetTemplateData(newPlayer.NickName);
        newPlayerTemplate.gameObject.SetActive(true);

        allPlayerTemplates.Add(newPlayerTemplate);
    }

    //Another player left the room
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayersInRoom();
    }

    //Unable to create a room
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        LoadUI(createRoomScreen);

        switch(returnCode)
        {
            case (int)RoomErrorCodes.SameName:
                roomCreateErrorText.text = "A room with same name already exists.";
                break;
            case (int)RoomErrorCodes.GameFull:
                roomCreateErrorText.text = "The room you tried to join is full.";
                break;
            case (int)RoomErrorCodes.GameEnded:
                roomCreateErrorText.text = "The room you tried to join is closed.";
                break;
            case (int)RoomErrorCodes.GameDoesNotExist:
                roomCreateErrorText.text = "The room you tried to does not exist.";
                break;
        }

        roomCreateErrorText.gameObject.SetActive(true);
    }

    /*---------- Internal Methods ----------*/

    //Checks if nickname is added or not
    private bool CheckNickname()
    {
        if (string.IsNullOrEmpty(nicknameField.text))
        {
            nicknameErrorText.text = "Add a nickname before starting a game.";
            nicknameErrorText.gameObject.SetActive(true);
            return false;
        }
        nicknameErrorText.gameObject.SetActive(false);
        return true;
    }

    //Update players in a  room
    private void UpdatePlayersInRoom()
    {
        foreach (PlayerTemplate playerTemplateInList in allPlayerTemplates)
        {
            Destroy(playerTemplateInList.gameObject);
        }

        allPlayerTemplates.Clear();

        Player[] allPlayers = PhotonNetwork.PlayerList;
        foreach (Player player in allPlayers)
        {
            PlayerTemplate newPlayerTemplate = Instantiate(playerTemplate, playerTemplate.transform.parent);
            newPlayerTemplate.SetTemplateData(player.NickName);
            newPlayerTemplate.gameObject.SetActive(true);

            allPlayerTemplates.Add(newPlayerTemplate);
        }

        if(PhotonNetwork.IsMasterClient)
        {
            startButton.SetActive(true);
            disabledStartButton.SetActive(false);
        }
        else
        {
            startButton.SetActive(false);
            disabledStartButton.SetActive(true);
        }
    }

    //Update cached rooms list
    private void UpdateCachedRoomList(List<RoomInfo> roomList)
    {
        for (int i = 0; i < roomList.Count; i++)
        {
            RoomInfo roomInfo = roomList[i];
            if (roomInfo.RemovedFromList || roomInfo.PlayerCount == matchPlayerLimit || roomInfo.IsOpen == false)
            {
                cachedRoomsList.Remove(roomInfo.Name);
            }
            else
            {
                cachedRoomsList[roomInfo.Name] = roomInfo;
            }
        }
        RoomListTemplateUpdate();
    }

    //Update rooms template list
    private void RoomListTemplateUpdate()
    {
        foreach (RoomTemplate roomTemplateInList in allRoomTemplates)
        {
            Destroy(roomTemplateInList.gameObject);
        }

        allRoomTemplates.Clear();

        foreach (KeyValuePair<string, RoomInfo> roomInfo in cachedRoomsList)
        {
            RoomTemplate newButton = Instantiate(roomTemplate, roomTemplate.transform.parent);
            newButton.SetTemplateData(roomInfo.Value);
            newButton.gameObject.SetActive(true);
            allRoomTemplates.Add(newButton);
        }

    }

    private void SavePlayerData()
    {
        LocalDataSaver dataSaver = LocalDataSaver.Instance;

        dataSaver.playerData.sensitivity = sensitivitySlider.value;
        dataSaver.playerData.nickName = PhotonNetwork.NickName;
        dataSaver.playerData.selectedCharacter = selectedCharacterSO;
        dataSaver.SaveData();
    }

    private void SetPlayerData()
    {
        LocalDataSaver dataSaver = LocalDataSaver.Instance;
        dataSaver.LoadData();

        nicknameField.text = dataSaver.playerData.nickName;
        sensitivitySlider.value = dataSaver.playerData.sensitivity;
        selectedCharacterSO = dataSaver.playerData.selectedCharacter;
    }

    private void SetScenePersistData()
    {
        SceneDataPersistence dataPersistence = SceneDataPersistence.Instance;
        dataPersistence.Sensitivity = sensitivitySlider.value;
        dataPersistence.SelectedCharacterMaterial = selectedCharacterSO.material;
    }

    public void SelectCharacter(CharacterTemplate character)
    {
        foreach(var characterTemplate in characterTemplates)
        {
            characterTemplate.namePlate.color = Color.black;
        }
        character.namePlate.color = selectedCharacterColor;
        selectedCharacterSO = character.characterSO;
    }

    private void SetSelectedCharacter(Character_SO characterSO)
    {
        foreach (var characterTemplate in characterTemplates)
        {
            characterTemplate.namePlate.color = Color.black;
            if(characterTemplate.characterSO == characterSO)
            {
                characterTemplate.namePlate.color = selectedCharacterColor;
            }
        }
    }
}
