using UnityEngine;
using Photon.Pun;
using System.Collections;

public class PlayerSpawner : MonoBehaviour
{
    public static PlayerSpawner Instance { get; private set; }

    [SerializeField] private GameObject playerPrefab, playerDeathEffect, playerSpawnEffect;
    [SerializeField] private float respawnTime;

    private GameObject playerPrefabOnNetwork;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if(PhotonNetwork.IsConnected)
        {
            SpawnPlayer(true);
        }
    }

    private void SpawnPlayer(bool newPlayer)
    {
        UIController.Instance.PlayerKilled(false);
        Transform spawnPoint = newPlayer == true ? SpawnManager.Instance.GetSpawnPoint(PhotonNetwork.LocalPlayer.ActorNumber - 1) : SpawnManager.Instance.GetRandomSpawnPoint();

        playerPrefabOnNetwork =  PhotonNetwork.Instantiate(playerPrefab.name, spawnPoint.position, spawnPoint.rotation);
        
        PhotonNetwork.Instantiate(playerSpawnEffect.name, playerPrefabOnNetwork.transform.position, playerSpawnEffect.transform.rotation);
    }

    public void KillPlayer(string hitter)
    {
        MatchManager.Instance.UpdateStatsSend(PhotonNetwork.LocalPlayer.ActorNumber, 1, 1);

        UIController.Instance.deathScreenText.text = "You were killed by " + hitter;
        UIController.Instance.PlayerKilled(true);

        StartCoroutine(DeathCoroutine());
    }

    private IEnumerator DeathCoroutine()
    {
        PhotonNetwork.Destroy(playerPrefabOnNetwork);

        GameObject deathEffect = PhotonNetwork.Instantiate(playerDeathEffect.name, playerPrefabOnNetwork.transform.position, Quaternion.identity);
        float deathEffectTiming = 2f;
        Destroy(deathEffect, deathEffectTiming);

        yield return new WaitForSeconds(respawnTime);

        SpawnPlayer(false);
    }
}
