using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillController : MonoBehaviour
{
    //BUTTON BLOCK
    [SerializeField] private Button _skillButton;
    [SerializeField] private Image _loader;

    //SKILL BLOCK
    [SerializeField] private ParticleSystem _skillSystem;
    [SerializeField] private SkillAttack _skillAction;


    private bool _active;
    [SerializeField] private float _reloadTime;

    [SerializeField] private MatchManager _matchManager;
    [SerializeField] private UIManager _uiManager;
    private bool tutorShown;

    private void Start()
    {
        _skillButton.onClick.AddListener(() => ActivateSkill());
        StartCoroutine(CorReload());
    }

    private void OnEnable()
    {
        StartCoroutine(CorReload());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private void ActivateButton()
    {
        _active = true;

        if (!tutorShown && _matchManager.needTutor)
        {
            _uiManager.ShowTutor(_uiManager._tutorSkillScreen);
        }
    }

    private void ActivateSkill()
    {
        if (_active)
        {
            if (_uiManager._tutorSkillScreen.activeInHierarchy)
            {
                _uiManager.DisactTutor(_uiManager._tutorSkillScreen);
                tutorShown = true;
            }

            _skillSystem.Play();
            _loader.fillAmount = 1;
            _active = false;
            _skillAction.Action();
            StartCoroutine(CorReload());
        }
    }

    private IEnumerator CorReload()
    {
        var elapsedTime = _reloadTime;

        if (!tutorShown && _matchManager.needTutor)
        {
            elapsedTime += 20;
        }

        while (_loader.fillAmount > 0)
        {
            _loader.fillAmount = elapsedTime / _reloadTime;
            elapsedTime -= Time.deltaTime;
            yield return null;
        }
        ActivateButton();
    }
}
