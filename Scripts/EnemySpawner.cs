using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;

public class EnemySpawner : MonoBehaviour
{
    private float _xPlus;
    private float _zPlus;
    private GameObject _tempEnemy;
    private int _numTypes;
    private bool _allowSpawn = true;

    [SerializeField] private GameObject _coin;
    [SerializeField] private GameObject _gem;
    [SerializeField] private GameObject _potion;
    [SerializeField] private Transform _poolParent;
    [SerializeField] private GameObject _enemyParent;

    public float maxEnemy = 10;
    public float curEnemy = 0;
    public float killedEnemy = 0;
    public static UnityEvent kill = new UnityEvent();
    public float pause;
    public List<GameObject> enemies = new List<GameObject>();
    public List<SpawnPoint> points = new List<SpawnPoint>();
    public ObjectPool<GameObject> enemyPool;
    public GameObject boss;
    public List<EnemySO> enemyTypes;
    public float progress;
    public List<EnemyController> canBeShootList = new List<EnemyController>();
    public float enemiesHealthMultiplier;
    public float enemiesDamageMultiplier;
    public float enemiesSpeedMultiplier;
    public float goldPerEnemy;
    public Material whiteMat;
    public int wavenum;

    public static EnemySpawner Instance;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        enemyPool = new ObjectPool<GameObject>(
         createFunc: () =>

             Instantiate(_enemyParent, _poolParent)
         ,
         actionOnGet: (obj) =>
         {

             obj.GetComponent<EnemyController>().enemyData = enemyTypes[SpawnRandomizer()];
             obj.SetActive(true);

         }
         ,
         actionOnRelease: (obj) => obj.SetActive(false),
         actionOnDestroy: (obj) => Destroy(obj),
         collectionCheck: false,
         defaultCapacity: 50,
         maxSize: 50
         );

        _numTypes = enemyTypes.Count;
        progress = 0;

        gameObject.SetActive(false);
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
        pause += Time.deltaTime;

        if (curEnemy < maxEnemy && points.Count > 0 && pause > ((1 - progress) / (1 + wavenum)) && _allowSpawn)
        {
            pause = 0;
            SpawnEnemy();
        }

    }

    private void CheckQuant()
    {

        var temp = 0;

        for (var i = 0; i < _poolParent.childCount; i++)
        {
            if (_poolParent.GetChild(i).gameObject.activeInHierarchy)
            {
                temp++;
            }
        }

        curEnemy = temp;

    }

    private void SpawnEnemy()
    {
        var tempEnemy = enemyPool.Get();
        SpawnPoint p = points[Random.Range(0, points.Count - 1)];

        tempEnemy.transform.position = p.transform.position;

        enemies.Add(tempEnemy);
    }

    public void SpawnBoss()
    {
        curEnemy++;
        var boss = Instantiate(boss, _poolParent);
        //boss.SetActive(true);
        //boss.GetComponent<BossController>()._dead = false;
        //boss.GetComponent<EnemyController>()._curHealth = boss.GetComponent<EnemyController>()._maxHealth;
        SpawnPoint p = points[Random.Range(0, points.Count - 1)];

        boss.transform.position = p.transform.position;

        enemies.Add(boss);
    }
    public void DespawnEnemy(GameObject enemy)
    {
        if (progress != 0)
        {
            PlayerMoveController.Instance.coins += goldPerEnemy;

            killedEnemy++;
            kill.Invoke();
        }
        enemyPool.Release(enemy);
        enemies.Remove(enemy);
        canBeShootList.Remove(enemy.GetComponent<EnemyController>());
    }

    private int SpawnRandomizer()
    {
        int num = Mathf.RoundToInt(progress * (enemyTypes.Count - 1));
        num = num >= (enemyTypes.Count - 1) ? (enemyTypes.Count - 1) : num;

        int rand = Random.Range(0, 100);

        if (rand < 50f)
            return num;
        else if (rand < 70 && num > 0)
            return num - 1;
        else if (rand < 85 && num > 1)
            return num - 2;
        else if (rand < 95f && num > 2)
            return num - 3;
        else if (rand <= 100f && num > 3)
            return num - 4;
        else
            return num;
    }

    public void StopSpawn()
    {
        _allowSpawn = false;
    }

    public void SpawnBonus()
    {

        var prob = Random.Range(0, 10);

        switch (prob)
        {
            case < 1:
                if (!_gem.activeInHierarchy)
                {
                    _gem.SetActive(true);
                    SpawnPoint p = points[Random.Range(0, points.Count - 1)];
                    _gem.transform.position = p.transform.position;
                }
                break;

            case < 4:
                if (!_potion.activeInHierarchy)
                {
                    _potion.SetActive(true);
                    SpawnPoint p = points[Random.Range(0, points.Count - 1)];
                    _potion.transform.position = p.transform.position;
                }
                break;

            case < 10:
                if (!_coin.activeInHierarchy)
                {
                    _coin.SetActive(true);
                    SpawnPoint p = points[Random.Range(0, points.Count - 1)];
                    _coin.transform.position = p.transform.position;
                }
                break;
        }
    }

    public void NewWave(int maxEnemies)
    {
        gameObject.SetActive(true);
        progress = 0;
        for (var i = 0; i < _poolParent.childCount; i++)
        {
            DespawnEnemy(_poolParent.GetChild(i).gameObject);
        }
        enemyPool.Clear();
        enemies.Clear();
        canBeShootList.Clear();

        maxEnemy = maxEnemies;

    }

    public void AllowSpawn()
    {
        _allowSpawn = true;
        InvokeRepeating("SpawnBonus", 5, 5);
        InvokeRepeating("CheckQuant", 0.2f, 0.2f);
    }
}