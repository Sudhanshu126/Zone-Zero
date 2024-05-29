using UnityEngine;
using Photon.Pun;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviourPunCallbacks
{
    //Private variables
    [SerializeField] private Transform viewPoint, groundCheckPoint, gunAdjuster, gunPlaceholder, ADSOnTransform, ADSOffTransform;
    [SerializeField] private float topViewAngleLimit, bottomViewAngleLimit, defaultFOV, adsSwitchTime;
    [SerializeField] private bool invertMouse;
    [SerializeField] private float moveSpeed, runSpeed, gravity, jumpForce;
    [SerializeField] private int playerLayerMaskId, maxPlayerHealth;
    [SerializeField] private LayerMask jumpableLayerMask;
    [SerializeField] private GameObject bulletHitEffect, playerHitImpact;
    [SerializeField] private SkinnedMeshRenderer playerModel;
    [SerializeField] private GunData[] allGuns;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private AudioSource gunAudioSource, playerAudioSource;
    [SerializeField] private AudioClip walkFootSteps, runFootSteps;

    private CharacterController characterController;
    private float verticalRotation, activeMoveSpeed, shotCounter, mouseSensitivity;
    private Camera mainCamera;
    private Vector2 mouseInput;
    private Vector3 moveDirection;
    private bool isGrounded, ADSOn, isMoving;
    private int selectedGun, currentHealth;
    private UIController uIController;
    private Gun_SO selectedGunSO;
    private InventoryGunData[] allGunsInventoryData;


    /*---------- Standard Methods ----------*/

    //Start Method
    private void Start()
    {
        characterController = GetComponent<CharacterController>();

        mainCamera = Camera.main;

        uIController = UIController.Instance;

        allGunsInventoryData = new InventoryGunData[allGuns.Length];

        SetPlayerData();

        if (photonView.IsMine)
        {
            currentHealth = maxPlayerHealth;
            uIController.SetHealthUI(currentHealth, maxPlayerHealth);

            SetDefaultInventoryGunData();
            SwitchGun();
            Reload();

            playerModel.gameObject.SetActive(false);
        }
        else
        {
            gunPlaceholder.parent = gunAdjuster;
            gunPlaceholder.localPosition = Vector3.zero;
            gunPlaceholder.localRotation = Quaternion.identity;
        }

        uIController.ChangeCursorVisibility(false);
    }

    //Update Method
    private void Update()
    {
        if (photonView.IsMine && MatchManager.Instance.State == GameState.Playing)
        {
            mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), -Input.GetAxisRaw("Mouse Y")) * mouseSensitivity;
            mouseInput.y *= invertMouse == true ? -1f : 1f;

            Vector2 keyboardInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

            CalculateMovements(keyboardInput);
            CalculateIsGrounded();
            MovePlayer();

            if (Input.GetMouseButtonDown(0) && allGunsInventoryData[selectedGun].ammoCount > 0)
            {
                Shoot();
            }

            if (Input.GetMouseButton(0) && allGunsInventoryData[selectedGun].ammoCount > 0 && selectedGunSO.isAutomatic)
            {
                shotCounter -= Time.deltaTime;
                if (shotCounter <= 0)
                {
                    Shoot();
                }
            }

            if (Input.GetAxisRaw("Mouse ScrollWheel") < 0 && selectedGun < allGuns.Length - 1)
            {
                selectedGun++;
                SwitchGun();
            }
            else if (Input.GetAxisRaw("Mouse ScrollWheel") > 0 && selectedGun > 0)
            {
                selectedGun--;
                SwitchGun();
            }

            playerAnimator.SetBool("grounded", isGrounded);
            playerAnimator.SetFloat("speed", keyboardInput.magnitude);

            if (Input.GetKeyDown(KeyCode.R))
            {
                Reload();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                uIController.ShowCursor = !uIController.ShowCursor;
                uIController.ChangeCursorVisibility(uIController.ShowCursor);
            }

            if (Input.GetMouseButtonDown(1))
            {
                ADSOn = !ADSOn;
                StartCoroutine(AdjustADS());
            }
        }
    }

    //Late Update
    private void LateUpdate()
    {
        if (photonView.IsMine)
        {
            MoveCamera();
        }
    }

    /*---------- Movement and Physics ----------*/

    //Move camera
    private void MoveCamera()
    {
        mainCamera.transform.position = viewPoint.position;
        mainCamera.transform.rotation = viewPoint.rotation;
    }

    //Move and rotate player
    private void CalculateMovements(Vector2 keyboardInput)
    {
        Vector3 transformRotation = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(transformRotation.x, transformRotation.y + mouseInput.x, transformRotation.z);

        verticalRotation += mouseInput.y;
        verticalRotation = Mathf.Clamp(verticalRotation, topViewAngleLimit, bottomViewAngleLimit);
        viewPoint.rotation = Quaternion.Euler(verticalRotation, viewPoint.rotation.eulerAngles.y, viewPoint.rotation.eulerAngles.z);

        float lastVerticalVelocity = moveDirection.y;
        moveDirection = (transform.forward * keyboardInput.y + transform.right * keyboardInput.x).normalized * activeMoveSpeed;
        moveDirection.y = lastVerticalVelocity;

        if (Input.GetKey(KeyCode.LeftShift))
        {
            activeMoveSpeed = runSpeed;
        }
        else
        {
            activeMoveSpeed = moveSpeed;
        }

        isMoving = keyboardInput.magnitude > 0 ? true : false;
    }

    //Check if grounded (for custom gravity and jumping)
    private void CalculateIsGrounded()
    {
        float groundCheckDistance = 0.35f;
        isGrounded = Physics.Raycast(groundCheckPoint.position, Vector3.down, groundCheckDistance, jumpableLayerMask);

        if (characterController.isGrounded)
        {
            moveDirection.y = 0;
        }
        else
        {
            moveDirection.y += gravity * Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            moveDirection.y = jumpForce;
        }
    }

    //Moves player
    private void MovePlayer()
    {
        characterController.Move(moveDirection * Time.deltaTime);
        if(isMoving && isGrounded)
        {
            if(activeMoveSpeed == moveSpeed)
            {
                playerAudioSource.clip = walkFootSteps;
            }
            else
            {
                playerAudioSource.clip = runFootSteps;
            }
            if (!playerAudioSource.isPlaying)
            {
                playerAudioSource.Play();
            }
        }
        else
        {
            playerAudioSource.Stop();
        }
    }

    /*---------- Player FPS Actions ----------*/

    //Shooting
    private void Shoot()
    {
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        ray.origin = mainCamera.transform.position;

        if (Physics.Raycast(ray, out RaycastHit hitInfo, selectedGunSO.range))
        {
            float bulletEffectDuration = 7f;
            GameObject hitObject = hitInfo.collider.gameObject;
            if (hitObject.layer == playerLayerMaskId)
            {
                GameObject playerImpact =  PhotonNetwork.Instantiate(playerHitImpact.name, hitInfo.point, Quaternion.identity);
                Destroy(playerImpact, bulletEffectDuration);

                PhotonView enemyPhotonView = hitObject.GetPhotonView();
                if(enemyPhotonView != null)
                {
                    enemyPhotonView.RPC("TakeDamage", RpcTarget.All, photonView.Owner.NickName, selectedGunSO.damage, PhotonNetwork.LocalPlayer.ActorNumber);
                }
            }
            else
            {
                GameObject bulletEffectInstance = Instantiate(bulletHitEffect, hitInfo.point, Quaternion.LookRotation(hitInfo.normal, Vector3.up));

                Destroy(bulletEffectInstance, bulletEffectDuration);
            }
        }

        gunAudioSource.Play();
        allGuns[selectedGun].GetMuzzleFlash().SetActive(true);
        Invoke("DisableMuzzleFlash", 0.1f);
        allGunsInventoryData[selectedGun].ammoCount -= 1;

        UpdateAmmoCount(false);
        shotCounter = selectedGunSO.firingRate;
    }

    //Reloading
    private void Reload()
    {
        UpdateAmmoCount(true);
    }

    //Switching gun
    private void SwitchGun()
    {
        selectedGunSO = allGuns[selectedGun].GetGunData();
        gunAudioSource.clip = selectedGunSO.shootSound;

        UpdateAmmoCount(false);

        photonView.RPC("SetGun", RpcTarget.All, selectedGun);
    }

    //Taking damage
    private void Damage(string hitter, int damage, int hitterActor)
    {
        if(photonView.IsMine)
        {
            int damageDealt = currentHealth - damage > 0 ? damage : currentHealth;
            MatchManager.Instance.UpdateStatsSend(hitterActor, 2, damageDealt);

            currentHealth -= damage;
            uIController.SetHealthUI(currentHealth, maxPlayerHealth);
            if(currentHealth <= 0)
            {
                currentHealth = 0;
                PlayerSpawner.Instance.KillPlayer(hitter);
                MatchManager.Instance.UpdateStatsSend(hitterActor, 0, 1);
            }
        }
    }

    private IEnumerator AdjustADS()
    {
        float timePassed = 0f;

        while(timePassed < adsSwitchTime)
        {
            float time = timePassed / adsSwitchTime;
            if (ADSOn)
            {
                mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, selectedGunSO.adsFOV, time);
                gunPlaceholder.position = Vector3.Lerp(gunPlaceholder.position, ADSOnTransform.position, time);
            }
            else
            {
                mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, defaultFOV, time);
                gunPlaceholder.position = Vector3.Lerp(gunPlaceholder.position, ADSOffTransform.position, time);
            }
            timePassed += Time.deltaTime;
            yield return null;
        }
    }

    /*---------- Gameplay related to Player ----------*/

    //Updating ammo count
    private void UpdateAmmoCount(bool reload)
    {
        if (reload)
        {
            allGunsInventoryData[selectedGun].ammoCount = selectedGunSO.maxAmmoCount;
        }
        uIController.ammoCount = allGunsInventoryData[selectedGun].ammoCount;
        uIController.SetAmmoCount();
    }
    
    //Setting inventory guns default data
    private void SetDefaultInventoryGunData()
    {
        for (int i = 0; i < allGuns.Length; i++)
        {
            allGunsInventoryData[i].ammoCount = allGuns[i].GetGunData().maxAmmoCount;
        }
    }

    //Disabling Muzzle flash
    private void DisableMuzzleFlash()
    {
        allGuns[selectedGun].GetMuzzleFlash().SetActive(false);
    }

    private void SetPlayerData()
    {
        SceneDataPersistence dataPersist = SceneDataPersistence.Instance;
        mouseSensitivity = dataPersist.Sensitivity;
        playerModel.material = dataPersist.SelectedCharacterMaterial;
    }

    /*---------- RPCs ----------*/

    //Dealing damage
    [PunRPC]
    private void TakeDamage(string hitter, int damageTaken, int hitterActor)
    {
        Damage(hitter, damageTaken, hitterActor);
    }

    //Changing Gun
    [PunRPC]
    private void SetGun(int gunIndex)
    {
        foreach (var guns in allGuns)
        {
            guns.GetMuzzleFlash().SetActive(false);
            guns.gameObject.SetActive(false);
        }
        allGuns[gunIndex].gameObject.SetActive(true);
    }
}
