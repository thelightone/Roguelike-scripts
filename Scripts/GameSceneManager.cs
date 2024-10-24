using System.Collections;
using System.Collections.Generic;
using Unity.Loading;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance;
    public float progress;

    private void Start()
    {
        if (Instance != null)
            Destroy(Instance.gameObject);

        Instance = this;
        DontDestroyOnLoad(this);

    }
    public void Game()
    {
        SceneManager.LoadScene("BattleScene");
    }

    public void Menu()
    {
        SceneManager.LoadScene("MenuScene");
    }
}
