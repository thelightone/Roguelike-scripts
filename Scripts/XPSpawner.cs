using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Pool;

public class XPSpawner : MonoBehaviour
{

    [SerializeField] private GameObject _prefab;

    public ObjectPool<GameObject> _pool;
    private GameObject _tempXP;
    public static XPSpawner Instance;
    public List<GameObject> listXP = new List<GameObject>();

    private void Start()
    {
        Instance = this;

        _pool = new ObjectPool<GameObject>(
         createFunc: () => Instantiate(_prefab, transform),
         actionOnGet: (obj) =>
         {
             obj.SetActive(true);
         }
         ,
         actionOnRelease: (obj) => obj.SetActive(false),
         actionOnDestroy: (obj) => Destroy(obj),
         collectionCheck: false,
         defaultCapacity: 100,
         maxSize: 200
         );
    }

    public void SpawnXP(Transform enemy)
    {
        _tempXP = _pool.Get(); ;
        _tempXP.transform.position = enemy.position;
        listXP.Add(_tempXP);
    }

    public void DespawnXP(GameObject xp)
    {
        _pool.Release(xp);
        listXP.Remove(xp);
    }

}
