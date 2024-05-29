using UnityEngine;

public class GunData : MonoBehaviour
{
    [SerializeField] private Gun_SO gunSO;
    [SerializeField] private GameObject muzzleFlash;

    public Gun_SO GetGunData()
    {
        return gunSO;
    }

    public GameObject GetMuzzleFlash()
    {
        return muzzleFlash;
    }
}
