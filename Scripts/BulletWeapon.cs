using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;
using static UnityEngine.ParticleSystem;
using Random = UnityEngine.Random;

public class BulletWeapon : MonoBehaviour
{
    private List<EnemyController> _enemys = new List<EnemyController>();
    private Transform _player;
    private AimShootWeapon _shootParent;
    private float _distance;
    private bool _repeated;

    private Collider _collider;
    private Rigidbody _rb;

    [SerializeField] private ParticleSystem _miss;

    public GameObject shootEnemy;
    public EnemyController enemyController;

    private void Start()
    {
        _shootParent = GetComponentInParent<AimShootWeapon>();
        _shootParent._bullets.Add(this);
        gameObject.SetActive(false);
        _rb = GetComponent<Rigidbody>();
        _player = PlayerMoveController.Instance.transform;
        _collider = GetComponent<Collider>();
    }

    private IEnumerator PreShoot()
    {
        float elapsedTime = 0;
        while (elapsedTime < 0.1f)
        {
            gameObject.transform.position = _player.position + _player.forward * 2;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator Fly()
    {
        float elapsedTime = 0;

        var basePos = gameObject.transform.position;

        if (shootEnemy != null)
        {
            Vector3 targPos = new Vector3(shootEnemy.transform.position.x, shootEnemy.transform.position.y + 2, shootEnemy.transform.position.z);

            transform.LookAt(targPos);
        }

        else
        {
            transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        }

        var getRepeatRate = _shootParent.reloadTime;
        _collider.enabled = true;
        _rb.AddForce(transform.forward / getRepeatRate * 5, ForceMode.Impulse);

        while (elapsedTime < getRepeatRate * 0.9)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        gameObject.SetActive(false);
        _miss.transform.position = gameObject.transform.position;
        _miss.Play();
        shootEnemy = null;
        _repeated = false;
        yield return null;
    }

    public void OnTriggerEnter(Collider collision)
    {
        if (collision.TryGetComponent<EnemyController>(out EnemyController enemy))
        {
            enemy.GetHit(_shootParent._hitForce * Modifiers.multiplHitForce * _shootParent.hitForceModif, _shootParent._upForce * Modifiers.multiplHitForceUp * _shootParent.weapHitForceUpModif, _shootParent._damage * Modifiers.multiplDamage * _shootParent.weapDamageModif);

            _miss.transform.position = gameObject.transform.position;
            _miss.Play();

        }
    }

    public void Shoot()
    {
        gameObject.SetActive(true);
        _collider.enabled = false;
        StartCoroutine(PreShoot());
        _enemys = EnemySpawner.Instance.canBeShootList;
        _distance = 100000;


        foreach (EnemyController enemy in _enemys)
        {
            if (enemy != null)
            {
                if (!_repeated)
                {
                    var _distance2 = Vector3._distance(PlayerMoveController.Instance.transform.position, enemy.gameObject.transform.position);
                    if (_distance2 < _distance && !_shootParent._busyEnemies.Contains(enemy))
                    {
                        enemyController = enemy;
                        shootEnemy = enemy.gameObject;
                        _distance = _distance2;
                    }
                }
                else
                {
                    var _distance2 = Vector3._distance(transform.position, enemy.gameObject.transform.position);
                    if (_distance2 < _distance && !_shootParent._busyEnemies.Contains(enemy))
                    {
                        enemyController = enemy;
                        shootEnemy = enemy.gameObject;
                        _distance = _distance2;
                    }
                }
            }
        }

        if (_enemys.Count > 1)
        {
            _shootParent._busyEnemies.Add(enemyController);
        }
        _rb.velocity = Vector3.zero;
        StartCoroutine(Fly());
    }
}
