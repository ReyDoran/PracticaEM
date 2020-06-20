using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class PlayerInfo : MonoBehaviour
{

    public void Start()
    {
        CurrentLap = -1;
    }

    public bool[] circuitControlPoints = { false, false, true };    // Cálculo de vueltas

    public int previousSegmentsId = 18; // Cálculo marcha atrás

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