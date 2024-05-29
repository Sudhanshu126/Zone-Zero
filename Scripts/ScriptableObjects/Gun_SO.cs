using UnityEngine;

[CreateAssetMenu]
public class Gun_SO : ScriptableObject
{
    public string gunName;
    public float firingRate, range, adsFOV;
    public int damage, maxAmmoCount;
    public bool isAutomatic;
    public GameObject gunPrefab;
    public AudioClip shootSound;
}
