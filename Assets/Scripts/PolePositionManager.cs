using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Mirror;
using Mirror.Examples.Basic;
using UnityEngine;

public class PolePositionManager : NetworkBehaviour
{
    public int numPlayers;
    private System.Timers.Timer countdown;
    public NetworkManager networkManager;
    public UIManager m_UIManager;    

    private PlayerController[] m_PlayerControllers = new PlayerController[4];
    private readonly List<PlayerInfo> m_Players = new List<PlayerInfo>(4);
    private CircuitController m_CircuitController;
    private GameObject[] m_DebuggingSpheres;
    

    private void Awake()
    {
        if (networkManager == null) networkManager = FindObjectOfType<NetworkManager>();
        if (m_CircuitController == null) m_CircuitController = FindObjectOfType<CircuitController>();
        if (m_UIManager == null) m_UIManager = FindObjectOfType<UIManager>();

        m_DebuggingSpheres = new GameObject[networkManager.maxConnections];
        for (int i = 0; i < networkManager.maxConnections; ++i)
        {
            m_DebuggingSpheres[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            m_DebuggingSpheres[i].GetComponent<SphereCollider>().enabled = false;
        }
    }

    private void Update()
    {
        if (m_Players.Count == 0)
            return;

        UpdateRaceProgress();
    }

    public void AddPlayer(PlayerInfo player)
    {
        m_Players.Add(player);
        StartRace();
    }

    private class PlayerInfoComparer : Comparer<PlayerInfo>
    {
        Dictionary<int, float> playerLengths = new Dictionary<int, float>();

        public PlayerInfoComparer(float[] arcLengths, List<PlayerInfo> m_Players)
        {
            //m_ArcLengths = arcLengths;
            for(int i = 0; i < arcLengths.Length; i++)
            {
                playerLengths.Add(m_Players[i].ID, arcLengths[i]);
            }
        }

        public override int Compare(PlayerInfo x, PlayerInfo y)
        {
            if (this.playerLengths[x.ID] < this.playerLengths[y.ID])
                return 1;
            else return -1;
        }
    }

    public void UpdateRaceProgress()
    {
        // Update car arc-lengths
        float[] arcLengths = new float[m_Players.Count];

        for (int i = 0; i < m_Players.Count; ++i)
        {
            arcLengths[i] = ComputeCarArcLength(i);
            m_Players[i].CurrentPosition = i+1;
            NetworkIdentity clientID = m_Players[i].GetComponent<NetworkIdentity>();
            m_PlayerControllers[i].TargetUpdateMyPosition(clientID.connectionToClient , m_Players[i].CurrentPosition);
            //Debug.Log(m_Players[i].Name + "SU POSICION ES " + m_Players[i].CurrentPosition);
        }

        m_Players.Sort(new PlayerInfoComparer(arcLengths, m_Players));

        string myRaceOrder = "";
        foreach (var _player in m_Players)
        {
            myRaceOrder += _player.Name + " \n";
        }

        //m_UIManager.UpdateClasification(myRaceOrder);
        for (int i = 0; i < m_PlayerControllers.Length; i++)
        {
            if (m_PlayerControllers[i] != null)
                m_PlayerControllers[i].RpcUpdateClasification(myRaceOrder);
        }
        //Debug.Log("El orden de carrera es: " + myRaceOrder);
    }

    float ComputeCarArcLength(int ID)
    {
        // Compute the projection of the car position to the closest circuit 
        // path segment and accumulate the arc-length along of the car along
        // the circuit.
        Vector3 carPos = this.m_Players[ID].transform.position;
        int segIdx;
        float carDist;
        Vector3 carProj;

        float minArcL =
            this.m_CircuitController.ComputeClosestPointArcLength(carPos, out segIdx, out carProj, out carDist);
        
        this.m_DebuggingSpheres[ID].transform.position = carProj;

        if (this.m_Players[ID].CurrentLap == 0)
        {
            minArcL -= m_CircuitController.CircuitLength;
        }
        else
        {
            minArcL += m_CircuitController.CircuitLength *
                       (m_Players[ID].CurrentLap - 1);
        }

        return minArcL;
    }

    // Use the position of the debug sphere to recolocate the car
    public Vector3 ResetPosition(int ID)
    {
        return this.m_DebuggingSpheres[ID].transform.position;
    }

    public int GetTotalLaps()
    {
       
        return m_CircuitController.totalLaps;

    }

    

    

        //Use de ID and the segment of the circuit to check % of lap
        public void CalculateLap(int ID, int segIdx)
    {
        bool[] controlpoints = new bool[24];
        bool finishlap = true;
        foreach (bool segment in controlpoints)
        {
            if (!segment)
            {
                finishlap = false;
                break;
            }
        }
        if (finishlap) this.m_Players[ID].CurrentLap++;
        
    }

    // Bloquea/desbloquea el movimiento de todos los coches de la escena
    private void FreezeAllCars(bool freeze)
    {
        for (int i = 0; i < m_PlayerControllers.Length; i++)
        {
            if (m_PlayerControllers[i] != null)
            {
                m_PlayerControllers[i].RpcFreezeCar(freeze);
            }
        }
    }

    //Bloquea a los coches durante 5 segundos
    public void StartRace()
    {
        for (int i = 0; i < m_Players.Count; i++)
        {
            m_PlayerControllers[i] = m_Players[i].gameObject.GetComponent<PlayerController>();
        }
        FreezeAllCars(true);
        countdown = new System.Timers.Timer(5000);
        countdown.AutoReset = false;
        countdown.Elapsed += ((System.Object source, System.Timers.ElapsedEventArgs e) => FreezeAllCars(false));
        countdown.Enabled = true;
    }

}