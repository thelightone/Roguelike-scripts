using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class EnemyController : MonoBehaviour
{
    public EnemySO enemyData;

    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public Vector3 hitAway;
    [HideInInspector] public Transform target;
    [HideInInspector] public float elapsedTime;
    [HideInInspector] public Vector3 prevPosition;
    [HideInInspector] public float damage = 5;
    [HideInInspector] public PlayerMoveController player;
    [HideInInspector] public Animator animator;
    [HideInInspector] public Collider collider;
    [HideInInspector] public bool deadCorStarted = false;
    [HideInInspector] public GameObject meshObj;
    [HideInInspector] public ParticleSystem hitPS;
    [HideInInspector] public GameObject deathPS;
    [HideInInspector] public TMP_Text hitText;
    [HideInInspector] public Animator textAnimator;
    [HideInInspector] public bool dead = false;
    [HideInInspector] public bool fly = false;
    [HideInInspector] public bool damagePause = false;
    [HideInInspector] public Material whiteMaterial;
    [HideInInspector] public Material tempMaterialMain;

    [HideInInspector] public float maxSpeed = 0.5f;
    [HideInInspector] public float moveSpeed = 0.5f;
    [HideInInspector] public float maxHealth = 10;
    [HideInInspector] public float curHealth = 10;

    [HideInInspector] public bool canBeShoot = false;

    [HideInInspector] public Slider hpSlider;

    private DamageText _damageText;
    private bool _getCrit;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnDisable()
    {
        // Unsubscribe from the OnTick event when the script is disabled or destroyed
        EnemySpawner.Instance.canBeShootList.Remove(this);
        Ticker.OnTick -= UpdateLogic;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            player.GetHit(collision.transform.position, transform.position, 3, damage);
            rb.AddRelativeForce(Vector3.back * 2, ForceMode.Impulse);
        }
    }

    private void OnTriggerExit(Collider collider)
    {
        if (collider.gameObject.CompareTag("Spawner"))
        {
            canBeShoot = true;
            EnemySpawner.Instance.canBeShootList.Add(this);
        }
    }

    private IEnumerator DeathCor()
    {

        XPSpawner.Instance.SpawnXP(transform);

        elapsedTime = 0;

        deadCorStarted = true;

        elapsedTime = 0;

        meshObj.GetComponent<Renderer>().material = whiteMaterial;

        while (elapsedTime < 0.1f)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        meshObj.SetActive(false);
        EnemySpawner.Instance.canBeShootList.Remove(this);
        while (elapsedTime < 1.5f)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        EnemySpawner.Instance.DespawnEnemy(gameObject);
        hpSlider.gameObject.SetActive(false);
        Clear();

    }

    private void Death()
    {
        XPSpawner.Instance.SpawnXP(transform);

        elapsedTime = 0;

        deadCorStarted = true;

        meshObj.GetComponent<Renderer>().material = whiteMaterial;
        meshObj.SetActive(false);
        EnemySpawner.Instance.canBeShootList.Remove(this);
        EnemySpawner.Instance.DespawnEnemy(gameObject);
        hpSlider.gameObject.SetActive(false);
        Clear();
    }

    private IEnumerator DamagePause()
    {
        elapsedTime = 0;
        meshObj.GetComponent<Renderer>().material = whiteMaterial;
        yield return new WaitForSeconds(0.2f);
        meshObj.GetComponent<Renderer>().material = enemyData.mainMat;
        damagePause = false;
    }

    private void LoadDataFromSO()
    {
        for (int i = 0; i < transform.GetChild(0).childCount; i++)
        {
            transform.GetChild(0).GetChild(i).gameObject.SetActive(false);
        }

        var x0 = transform;
        var x1 = x0.GetChild(0);
        var x3 = enemyData.enemyNumber;
        var x2 = x1.GetChild(x3);
        x2.gameObject.SetActive(true);
        var prevMesh = meshObj;
        meshObj = transform.GetChild(0).GetChild(enemyData.enemyNumber).GetChild(0).gameObject;

        maxSpeed = enemyData.maxSpeed * EnemySpawner.Instance.enemiesSpeedMultiplier;
        moveSpeed = maxSpeed;
        maxHealth = enemyData.maxHealth * EnemySpawner.Instance.enemiesHealthMultiplier;

        damage = enemyData.damage * EnemySpawner.Instance.enemiesDamageMultiplier;

        meshObj.GetComponent<Renderer>().material = enemyData.mainMat;

        curHealth = maxHealth;

        deathPS.transform.GetChild(2).GetComponent<ParticleSystemRenderer>().material = enemyData.deathMat;

        animator = transform.GetChild(0).GetChild(enemyData.enemyNumber).GetComponentInChildren<Animator>();
        hpSlider.gameObject.SetActive(false);

        damagePause = false;
        _damageText.Stop();

        meshObj.SetActive(true);
    }

    public virtual void Clear()
    {
        curHealth = maxHealth;
        fly = false;
        dead = false;
        rb.useGravity = true;
        rb.drag = 0;
        deadCorStarted = false;
        damagePause = false;
        deathPS.SetActive(false);
        EnemySpawner.Instance.canBeShootList.Remove(this);
        canBeShoot = false;
    }

    public virtual void GetHit(float force, float upForce, float damage)
    {
        if (EnemySpawner.Instance.curEnemy > 1)
        {
            if (!damagePause && !dead)
            {
                AudioManager.Instance.OnEnemyGetHit();
                damagePause = true;

                //New things Test block
                PlayerMoveController.Instance.Vamp(damage);

                _getCrit = false;
                float critChance = Random.Range(0.00f, 1.00f);
                if (critChance < PlayerMoveController.Instance._critChance)
                {
                    damage *= 2;
                    _getCrit = true;
                }

                curHealth -= damage;
                dead = curHealth > 0 ? false : true;
                _damageText.Show(damage, _getCrit);

                if (dead)
                {
                    hpSlider.gameObject.SetActive(false);
                    deathPS.SetActive(true);
                    rb.AddRelativeForce((Vector3.back + Vector3.up) * 10, ForceMode.Impulse);
                    if (gameObject.activeInHierarchy)
                        StartCoroutine(DeathCor());
                    else
                        Death();
                }
                else
                {
                    hpSlider.gameObject.SetActive(true);
                    hpSlider.value = (curHealth / maxHealth) * 100;
                    hitPS.Play();
                    rb.AddRelativeForce(Vector3.back * Random.Range(force / 1.2f, force / 1.2f) + Vector3.up * Random.Range(upForce / 1.2f, upForce / 1.2f), ForceMode.Impulse);
                    animator.SetTrigger("GetHit");
                    StartCoroutine(DamagePause());
                }
            }
        }
        else if (EnemySpawner.Instance.curEnemy == 1 && WeaponsManager.Instance._matchManager._stage == MatchManager.Stage.KillThem)
        {
            curHealth -= damage;
            dead = curHealth > 0 ? false : true;

            if (dead)
            {
                if (gameObject.activeInHierarchy)
                    StartCoroutine(DeathCor());
                else
                    Death();
            }
        }
    }

    public virtual void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.CompareTag("Despawn"))
        {
            EnemySpawner.Instance.canBeShootList.Remove(this);
            EnemySpawner.Instance.DespawnEnemy(gameObject);
            Clear();
        }
        else if (collider.gameObject.CompareTag("LevelCollider"))
        {
            collider.isTrigger = false;
            fly = false;
        }
    }
    public virtual void OnTriggerStay(Collider collider)
    {
        if (collider.gameObject.CompareTag("LevelCollider"))
        {
            collider.isTrigger = false;
            fly = false;
        }
    }

    public virtual void OnCollisionExit(Collision collider)
    {
        if (collider.gameObject.CompareTag("LevelCollider"))
        {
            collider.isTrigger = true;
            fly = true;
        }
    }

    public virtual void UpdateLogic()
    {
        if (!dead && !fly && target != null)
            EnemyMove();

    }

    public void EnemyMove()
    {
        transform.LookAt(target.position);
        transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
        transform.Translate(Vector3.forward / 50 * moveSpeed);
    }

    public virtual void Start()
    {
        _damageText = GetComponentInChildren<DamageText>();
        _damageText.gameObject.SetActive(false);
        player = PlayerMoveController.Instance;
        target = player.transform;

        LoadDataFromSO();

        collider = GetComponentInChildren<Collider>();
        whiteMaterial = EnemySpawner.Instance.whiteMat;
    }

    public virtual void OnEnable()
    {
        // Subscribe to the OnTick event when the script is enabled
        Ticker.OnTick += UpdateLogic;
        if (_collider != null)
        {
            LoadDataFromSO();

        }
    }
}
