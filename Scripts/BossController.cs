using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BossController : EnemyController
{
    public static UnityEvent defeatBoss = new UnityEvent();

    private bool _doSkill;
    private float _skillTime;

    [SerializeField] private GameObject _skill;

    public override void Start()
    {
        base.Start();
        _hpSlider.gameObject.SetActive(true);
        _hpSlider.value = _hpSlider.maxValue;
    }

    public override void OnEnable()
    {
        base.OnEnable();
        _hpSlider.value = _hpSlider.maxValue;
    }

    public override void UpdateLogic()
    {
        _skillTime += Time.deltaTime;

        if (_skillTime>5 && canBeShoot && !_doSkill)
        {
            _doSkill = true;
            _animator.SetBool("Skill", true);
            _animator.ResetTrigger("GetHit");
        }

        if (!_dead && !_fly && _target != null &&!_doSkill)
            EnemyMove();
    }

    public override void GetHit(float force, float upForce, float damage)
    {
        upForce = 0;
        force = 1;
        base.GetHit(force, upForce, damage);
    }

    public override void OnCollisionExit(Collision collider)
    {
        return;
    }

    public override void Clear()
    {
        base.Clear();
        defeatBoss.Invoke();
    }

    public override void OnTriggerEnter(Collider collider)
    {
        return;
    }

    public void StartSkill()
    {
        _skill.SetActive(true); 
    }

    public void StopSkill()
    {
        _animator.SetBool("Skill", false);
        _skill.SetActive(false);
        skillTime = 0;
        _doSkill = false;
    }
}
