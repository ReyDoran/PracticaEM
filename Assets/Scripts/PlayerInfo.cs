using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class PlayerInfo : MonoBehaviour
{

    public bool[] controlpoints = new bool[24];

    public string Name { get; set; }

    public int ID { get; set; }

    public int CurrentPosition { get; set; }

    public int CurrentLap { get; set; }

    public float[] TimePerLap { get; set; }

    public override string ToString()
    {
        return Name;
    }
}