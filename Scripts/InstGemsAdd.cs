using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstGemsAdd : MonoBehaviour
{
    [SerializeField] private GameObject addGemsScreen;

    public bool SpendGems()
    {
        if (PrefsManager.GetGems() > 10)
        {
            PrefsManager.ChangeGems(-10);
            return true;
        }
        else
        {
            StartCoroutine(NeedGems());
            return false;
        }

    }

    private IEnumerator NeedGems()
    {
        addGemsScreen.SetActive(true);
        if (Time.timeScale < 1)
        {
            yield return new WaitForSeconds(0.1f);
        }
        else
        {
            yield return new WaitForSeconds(1f);
        }
        addGemsScreen.SetActive(false);
    }
}
