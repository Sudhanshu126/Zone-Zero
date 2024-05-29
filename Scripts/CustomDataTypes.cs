using System;

//Struct for keeping inventory guns data
public struct InventoryGunData
{
    public int ammoCount;
}

//Event codes for matching players
public enum EventCodes : byte
{
    NewPlayer,
    ListPlayer,
    UpdateStats
}

//Error codes enum for different types of errors
public enum RoomErrorCodes
{
    SameName = 32766,
    GameFull = 32762,
    GameEnded = 32764,
    GameDoesNotExist = 32758
}

//Game states
public enum GameState
{
    Waiting,
    Playing,
    Paused,
    Ending
}

//Scene codes
public enum SceneCode
{
    MainMenu,
    MainGame
}

//Character id
public enum CharacterId
{
    Ballu,
    Chintu,
    Gabbar,
    Jagdish,
    Kanya,
    Mahesh,
    Rudra,
    Seema
}

//Class to keep track of player data
[Serializable]
public class PlayerInfo
{
    public string NickName { get; private set; }
    public int Actor { get; private set; }
    public int Kills { get; private set; }
    public int Deaths { get; private set; }
    public int Damage { get; private set; }

    public PlayerInfo(object[] data)
    {
        NickName = (string)data[0];
        Actor = (int)data[1];
        Kills = (int)data[2];
        Deaths = (int)data[3];
        Damage = (int)data[4];
    }

    public object[] GetData()
    {
        object[] data = new object[5];
        data[0] = NickName;
        data[1] = Actor;
        data[2] = Kills;
        data[3] = Deaths;
        data[4] = Damage;
        return data;
    }

    public void UpdateStat(int index, int amount)
    {
        switch (index)
        {
            case 0: //Kills
                Kills += amount;
                break;

            case 1: //Deaths
                Deaths += amount;
                break;

            case 2: //Damage
                Damage += amount;
                break;
        }
    }
}