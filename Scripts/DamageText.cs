using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;

public class _damage_text : MonoBehaviour
{
    [SerializeField] private GameObject _canvas;
    [SerializeField] private Vector3 _rotation;

    private int _damage;
    private TMP__text _text;
    private Vector3 _pos;
    private Vector3 _initPos;
    private int _goTop;

    private void Awake()
    {
        _text = GetComponentInChildren<TMP__text>();
        if (GetComponentInParent<BossController>() != null)
        {
            _goTop = 10;
        }
        else
        {
            _goTop = 6;
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
        StopCoroutine("ShowCor");
    }

    private void UpdateLogic()
    {
        transform.rotation = Quaternion.Euler(_rotation);
    }

    public void Show(float dam, bool crit)
    {
        if (!gameObject.activeInHierarchy && gameObject.transform.parent.gameObject.activeInHierarchy)
        {
            _text.color = crit ? Color.yellow : Color.white;

            _text._text = crit ? "Crit!" + "\n" + Convert.ToInt32(dam).ToString() : Convert.ToInt32(dam).ToString();

            gameObject.SetActive(true);
            StartCoroutine(ShowCor(dam));
        }
    }

    private IEnumerator ShowCor(float dam)
    {
        _initPos = new Vector3(transform.parent.position.x, transform.parent.position.y + _goTop, transform.parent.position.z);

        float elaps = 0;

        while (elaps < 0.9f)
        {
            transform.position = _initPos;
            elaps += Time.deltaTime;
            yield return null;
        }

        gameObject.SetActive(false);
    }

    public void Stop()
    {
        StopCoroutine("ShowCor");
        gameObject.SetActive(false);
    }
}

