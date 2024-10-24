using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using Cinemachine;
using System.Runtime.CompilerServices;
using System.Linq.Expressions;
using System.Security;


public class PlayerMoveController : MonoBehaviour
{
    //INIT DATA BLOCK
    public static PlayerMoveController Instance;
    public HeroData heroData;
    public ShopData shopData;
    public UpgradePerLevel upgradesData;

    // MOVE BLOCK
    public float moveSpeed;
    [SerializeField] private FloatingJoystick _joystick;
    [SerializeField] private float _rotateSpeed;
    private Vector3 _moveVector;
    private Animator _animatorController;
    private bool _grounded;

    // GET HIT BLOCK
    [SerializeField] private GameObject _meshObj;

    private bool _damagePause;
    private Rigidbody _rb;
    private Vector3 _hitAway;
    private float _elapsedTime;
    private Material _tempMaterial;
    private Collider _collider;

    public float defence;

    public static UnityEvent healthChange = new UnityEvent();

    // HEALTH BLOCK
    private float _regenTime;

    public float curHealth;
    public float maxHealth = 100;
    public float regeneration;
    public bool allowRegen;
    public float vamp;

    // XP BLOCK
    private float _xpCollectMultiplier;

    [SerializeField] private ParticleSystem _getXPPS;

    public float curXP;
    public float maxXP = 50;
    public float level;
    public GameObject levelUpEffect;

    public static UnityEvent xpChange = new UnityEvent();
    public static UnityEvent levelUpChange = new UnityEvent();
    public static UnityEvent getMaxLevel = new UnityEvent();

    // ATTACK BLOCK
    private float _attackSpeed = 2;

    public float critChance;
    public WeaponTypeParent baseWeapon;

    //CINEMACHINE
    [SerializeField] private CinemachineVirtualCamera _camera;

    private CinemachineBasicMultiChannelPerlin _noise;
    private bool _zoomed;

    //COLLECT
    private float _coinCollectMultiplier;

    [SerializeField] private ParticleSystem _getCoin;
    [SerializeField] private ParticleSystem _getGem;
    [SerializeField] private ParticleSystem _getPotion;

    public float coins;
    public int gems;
    public bool deadFromFall;
    public bool maxLevel;

    public static UnityEvent balance = new UnityEvent();

    private void Awake()
    {
        Instance = this;
        _rb = GetComponentInChildren<Rigidbody>();
        _animatorController = GetComponentInChildren<Animator>();

        _collider = GetComponent<Collider>();
        Material thisMaterial = _meshObj.GetComponent<Renderer>().material;
        _tempMaterial = new Material(thisMaterial);

        _meshObj.GetComponent<Renderer>().material = _tempMaterial;

        ParamsInit();

        xpChange.Invoke();
        healthChange.Invoke();

        _noise = _camera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        allowRegen = true;
    }

    private void ParamsInit()
    {
        level = 1;
        curXP = 0;
        maxXP = 50;

        InitSpecificParam(ref maxHealth, heroData.heroMaxHealth, upgradesData.MaxHealth, shopData.heroMaxHealthLevel);
        curHealth = maxHealth;
        InitSpecificParam(ref regeneration, heroData.heroRegenerate, upgradesData.Regenerate, shopData.heroRegenerateLevel);
        InitSpecificParam(ref moveSpeed, heroData.heroSpeed, upgradesData.Speed, shopData.heroSpeedLevel);
        InitSpecificParam(ref defence, heroData.heroDefence, upgradesData.Defence, shopData.heroDefenceLevel);
        InitSpecificParam(ref _coinCollectMultiplier, heroData.heroMultiplyCoinCollect, upgradesData.MultiplyCoinCollect, shopData.heroMultiplyCoinCollectLevel);
        InitSpecificParam(ref _xpCollectMultiplier, heroData.heroMultiplyXpReceived, upgradesData.MultiplyXpReceived, shopData.heroMultiplyXpReceivedLevel);

        //New test parameters
        vamp = 0;
        critChance = 0.1f;
    }

    private void InitSpecificParam(ref float heroParam, float heroSOParam, float upgradePerLevel, float level)
    {
        if (level == 0)
        {
            heroParam = heroSOParam;
        }
        else if (level == 1)
        {
            heroParam = heroSOParam * upgradesData.basicModifier;
        }
        else
        {
            heroParam = heroSOParam * (upgradesData.basicModifier + (upgradePerLevel * (level - 1)));
        }
    }

    private void OnEnable()
    {
        // Subscribe to the OnTick event when the script is enabled
        Ticker.OnTick += UpdateLogic;

    }

    private void OnDisable()
    {
        // Unsubscribe from the OnTick event when the script is disabled or destroyed
        Ticker.OnTick -= UpdateLogic;
    }

    private void UpdateLogic()
    {
        if (curHealth > 0)
            Move();
        _regenTime += Time.deltaTime;
        if (_regenTime > 1)
        {
            Regenerate();
            _regenTime = 0;
        }
    }

    private void Regenerate()
    {
        if (curHealth < maxHealth && curHealth > 0 && allowRegen)
        {
            curHealth += regeneration + heroData.heroRegenerate;
            healthChange.Invoke();
        }
    }


    private void Move()
    {
        _moveVector = Vector3.zero;
        _moveVector.x = _joystick.Horizontal * moveSpeed * Time.deltaTime;
        _moveVector.z = _joystick.Vertical * moveSpeed * Time.deltaTime;

        var absHor = Math.Abs(_joystick.Horizontal);
        var absVer = Math.Abs(_joystick.Vertical);

        if (absHor > 0.1 || absVer > 0.1)
        {
            Vector3 direction = Vector3.RotateTowards(transform.forward, _moveVector, _rotateSpeed * Time.deltaTime, 0.0f);
            transform.rotation = Quaternion.LookRotation(direction);
            transform.position = transform.position + _moveVector;

            var animSpeed = absHor > absVer ? absHor : absVer;
            _animatorController.SetFloat("MoveSpeed", animSpeed * moveSpeed);
            _animatorController.SetBool("Move", true);
            if (!_zoomed)
                _camera.m_Lens.FieldOfView = 44 + animSpeed;
        }

        else if (absHor == 0 || absVer == 0)
        {
            _animatorController.SetBool("Move", false);
            if (!_zoomed && _camera.m_Lens.FieldOfView != 44)
                StartCoroutine(ZoomIn());
        }
    }

    private IEnumerator Death()
    {
        WeaponsManager.Instance.gameObject.SetActive(false);
        var pos = transform.position;
        _joystick.gameObject.SetActive(false);
        _moveVector = new Vector3(0, 0, 0);
        _rb.drag = 100;
        _rb.mass = 100;
        _collider.isTrigger = true;

        _animatorController.SetBool("Death", true);
        healthChange.Invoke();
        _elapsedTime = 0;

        _tempMaterial.SetColor("_EmissionColor", Color.white * 100);
        yield return new WaitForSeconds(0.1f);
        _tempMaterial.SetColor("_EmissionColor", Color.white * 1);
        _elapsedTime += Time.deltaTime;
        yield return new WaitForSeconds(0.1f);



    }

    private IEnumerator DamagePause()
    {
        _elapsedTime = 0;

        for (var i = 0; i < 3; i++)
        {
            if (i == 0)
            {
                _noise.m_AmplitudeGain = 3;
            }
            _tempMaterial.SetColor("_EmissionColor", Color.white * 100);
            yield return new WaitForSeconds(0.1f);
            _tempMaterial.SetColor("_EmissionColor", Color.white * 1);
            _elapsedTime += Time.deltaTime;
            yield return new WaitForSeconds(0.1f);

            if (i == 0)
            {
                _noise.m_AmplitudeGain = 0;
            }
        }

        _elapsedTime = 0;

        while (_elapsedTime < 0.4f)
        {
            _elapsedTime += Time.deltaTime;
            yield return null;
        }

        _damagePause = false;
        allowRegen = true;
    }

    private IEnumerator LevelUp()
    {
        if (!maxLevel)
        {
            levelUpEffect.SetActive(true);
            AudioManager.Instance.OnLevelUp();
            level++;
            curXP = 0;
            maxXP += level * 25;
            levelUpChange.Invoke();

            WeaponsManager.Instance.CreateScreenArray(level);

            yield return new WaitForSeconds(2f);
            levelUpEffect.SetActive(false);
        }
    }

    private void GetXP()
    {
        curXP += 15 * _xpCollectMultiplier;

        AudioManager.Instance.OnTakeXP();
        xpChange.Invoke();

        if (curXP >= maxXP)
        {
            StartCoroutine(LevelUp());
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("XP") || other.CompareTag("Coin") || other.CompareTag("Gem") || other.CompareTag("Potion"))
        {
            StartCoroutine(Magnit(other.gameObject));
        }
        else if (other.gameObject.CompareTag("Despawn"))
        {
            curHealth = 0;
            deadFromFall = true;
            StartCoroutine(Death());
        }
    }

    private IEnumerator Magnit(GameObject xp)
    {
        float elapsTime = 0;
        var pos = xp.transform.position;
        while (elapsTime < 0.5f)
        {
            xp.transform.position = Vector3.Lerp(pos, transform.position - new Vector3(0, 0.5f, 0), (elapsTime / 0.5f) * (elapsTime / 0.5f));
            elapsTime += Time.deltaTime;
            yield return null;
        }
        xp.transform.position = transform.position;

        switch (xp.tag)
        {
            case "XP":
                _getXPPS.Play();
                GetXP();
                XPSpawner.Instance.DespawnXP(xp);
                break;

            case "Coin":
                _getCoin.Play();
                coins += (int)(10 * _coinCollectMultiplier);
                balance.Invoke();
                AudioManager.Instance.OnTakeSpecial();
                xp.SetActive(false);
                break;

            case "Gem":
                _getGem.Play();
                gems += 1;
                balance.Invoke();
                xp.SetActive(false);
                AudioManager.Instance.OnTakeSpecial();
                break;

            case "Potion":
                _getPotion.Play();
                curHealth = curHealth + (maxHealth * 0.5f) >= maxHealth ? maxHealth : curHealth + (maxHealth * 0.5f);
                healthChange.Invoke();
                AudioManager.Instance.OnTakeSpecial();
                xp.SetActive(false);
                break;
        }
    }

    private void OnCollisionStay(Collision other)
    {
        if (other.gameObject.CompareTag("LevelCollider"))
        {
            _grounded = true;
            _animatorController.SetBool("Fly", false);
        }
    }

    private void OnCollisionExit(Collision other)
    {
        if (other.gameObject.CompareTag("LevelCollider"))
        {
            _grounded = false;
            StartCoroutine("FlyCheck");
        }
    }

    private IEnumerator FlyCheck()
    {
        yield return new WaitForSeconds(0.5f);
        if (!_grounded)
        {
            _animatorController.SetBool("Fly", true);
        }

    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("LevelCollider"))
        {
            _grounded = true;
            _animatorController.SetBool("Fly", false);
        }
    }

    public void ChangeMaxHealth(float change)
    {
        maxHealth += change;
        healthChange.Invoke();
    }

    public void NewWave()
    {
    }

    public void Revive()
    {
        allowRegen = true;
        curHealth = maxHealth;
        _joystick.gameObject.SetActive(true);
        _moveVector = new Vector3(0, 0, 0);
        _rb.drag = 1;
        _rb.mass = 5;
        _collider.isTrigger = false;

        _animatorController.SetTrigger("Revive");
        _animatorController.SetBool("Death", false);
        healthChange.Invoke();

        StartCoroutine(DamagePause());
        deadFromFall = false;
    }

    public void MaxLevel()
    {
        maxLevel = true;
        getMaxLevel.Invoke();
    }

    public void Vamp(float damage)
    {
        if (curHealth < maxHealth && curHealth > 0)
        {
            curHealth += damage * vamp;
            healthChange.Invoke();
        }
    }

    public void GetHit(Vector3 collision, Vector3 weapon, float force, float damage)
    {
        if (!_damagePause)
        {
            allowRegen = false;
            _damagePause = true;
            _hitAway = ((collision - weapon)).normalized;
            _rb.AddForce(_hitAway * force, ForceMode.Impulse);
            curHealth -= (damage - (damage * defence));

            AudioManager.Instance.OnGetHit();
            //Handheld.Vibrate();

            if (curHealth <= 0)
            {

                StartCoroutine(Death());
            }
            else
            {
                _animatorController.SetTrigger("GetHit");
                healthChange.Invoke();
                StartCoroutine(DamagePause());
            }
        }
    }

    public void BaseAttack()
    {
        _animatorController.SetFloat("AttackBlend", (float)Math.Round(UnityEngine.Random.Range(0.6f, 3.4f), 0));
        _animatorController.SetTrigger("Attack");
    }

    public IEnumerator ZoomInOut()
    {
        _zoomed = true;
        float zoomTime = 0;
        float cur = _camera.m_Lens.FieldOfView;

        while (zoomTime < 0.1f)
        {
            _camera.m_Lens.FieldOfView = Mathf.Lerp(cur, cur - 0.5f, zoomTime / 0.1f);
            zoomTime += Time.deltaTime;
            yield return null;
        }

        zoomTime = 0;
        cur = _camera.m_Lens.FieldOfView;
        while (zoomTime < 0.1f)
        {
            _camera.m_Lens.FieldOfView = Mathf.Lerp(cur, cur + 0.5f, zoomTime / 0.1f);
            zoomTime += Time.deltaTime;
            yield return null;
        }
        _zoomed = false;
    }

    public IEnumerator ZoomIn()
    {
        float zoomTime = 0;
        float cur = _camera.m_Lens.FieldOfView;

        while (zoomTime < 1)
        {
            _camera.m_Lens.FieldOfView = Mathf.Lerp(cur, 44, zoomTime / 1);
            zoomTime += Time.deltaTime;
            yield return null;
        }
        _zoomed = false;
    }

    public IEnumerator ZoomOut()
    {
        _zoomed = true;
        float zoomTime = 0;
        float cur = _camera.m_Lens.FieldOfView;

        while (zoomTime < 2)
        {
            _camera.m_Lens.FieldOfView = Mathf.Lerp(cur, 45, zoomTime / 2);
            zoomTime += Time.deltaTime;
            yield return null;
        }
    }

    public void MagnitAllXP()
    {
        foreach (var xp in XPSpawner.Instance.listXP)
        {
            StartCoroutine(Magnit(xp));
        }
    }
}




