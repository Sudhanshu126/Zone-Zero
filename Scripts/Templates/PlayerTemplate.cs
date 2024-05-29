using TMPro;
using UnityEngine;

public class PlayerTemplate : MonoBehaviour
{
    [SerializeField] private TMP_Text playerName;

    public void SetTemplateData(string playerName)
    {
        this.playerName.text = playerName;
    }
}
