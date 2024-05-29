using UnityEngine;
using System;
using System.IO;

[Serializable]
public class PlayerData
{
    public string nickName;
    public float sensitivity;
    public Character_SO selectedCharacter;
}

public class LocalDataSaver : MonoBehaviour
{
    public static LocalDataSaver Instance {  get; private set; }

    public PlayerData playerData;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        playerData = new PlayerData();
    }

    public void SaveData()
    {
        string savedJson = JsonUtility.ToJson(playerData);
        string savePath = Application.persistentDataPath + "/PlayerData.json";

        File.WriteAllText(savePath, savedJson);
    }

    public void LoadData()
    {
        string savePath = Application.persistentDataPath + "/PlayerData.json";
        if (File.Exists(savePath))
        {
            string loadData = File.ReadAllText(savePath);
            playerData = JsonUtility.FromJson<PlayerData>(loadData);
        }
    }
}
