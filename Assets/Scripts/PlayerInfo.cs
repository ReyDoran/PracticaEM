using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class PlayerInfo : MonoBehaviour
{

    public void Start()
    {
        FirstTime = true;
        CurrentLap = -1;
    }

    public bool[] circuitControlPoints = { false, false, true };

    public bool FirstTime { get; set; }

    public string Name { get; set; }

    public int ID { get; set; }

    public int CurrentPosition { get; set; }

    public int CurrentLap { get; set; }

    public string Color { get; set; }

    public override string ToString()
    {
        return Name;
    }
}