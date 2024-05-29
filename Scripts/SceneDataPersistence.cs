using UnityEngine;

public class SceneDataPersistence : MonoBehaviour
{
    public static SceneDataPersistence Instance { get; private set; }
    public float Sensitivity { get; set; }
    public Material SelectedCharacterMaterial {  get; set; }

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
