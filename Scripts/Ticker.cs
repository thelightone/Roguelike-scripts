using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ticker : MonoBehaviour
{
    public delegate void TickDelegate();
    public static event TickDelegate OnTick;

    void FixedUpdate()
    {
        OnTick?.Invoke();
    }
}