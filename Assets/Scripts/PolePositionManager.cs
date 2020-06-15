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
    public NetworkManager networkManager;
    public UIManager m_UIManager;
    private List<PlayerController> m_PlayerControllers = new List<PlayerController>();
    private readonly List<PlayerInfo> m_Players = new List<PlayerInfo>();
    private CircuitController m_CircuitController;
    private RaceInfo m_RaceInfo;

    public string[] colors = new string[4];
    public int numPlayers;
    public int totalLaps;
    public readonly int MaxPlayersInGame =2 ;// MAX 4
    private List<int> clasification = new List<int>();
    public bool startedRace = false;
    private System.Timers.Timer countdown;
    public GameObject[] m_DebuggingSpheres { get; set; }

    public int debugVariable = 0;

    private void Awake()
    {
        if (networkManager == null) networkManager = FindObjectOfType<NetworkManager>();
        if (m_CircuitController == null) m_CircuitController = FindObjectOfType<CircuitController>();
        if (m_UIManager == null) m_UIManager = FindObjectOfType<UIManager>();
        if (m_RaceInfo == null) m_RaceInfo = FindObjectOfType<RaceInfo>();

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
        if(startedRace==true)
        UpdateRaceProgress();
    }

    public void AddPlayer(PlayerInfo player)
    {
        m_Players.Add(player);
        numPlayers++;   // Error concurrencia?
        //numPlayers++;   // Error concurrencia?
        clasification.Add(-1);  // Idem
        colors[player.GetComponent<PlayerInfo>().ID] = player.GetComponent<PlayerInfo>().Color;
        //m_RaceInfo.RpcChangeColor(player.GetComponent<PlayerInfo>().ID, player.GetComponent<PlayerInfo>().Color);
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
        bool clasificationHasChanged = false;
        // Update car arc-lengths
        float[] arcLengths = new float[m_Players.Count];

        /*
        foreach (PlayerInfo player in m_Players){            
            arcLengths[player.ID] = ComputeCarArcLength(player.ID);
        }
        */
        for (int i = 0; i < m_Players.Count; ++i)
        {
            arcLengths[i] = ComputeCarArcLength(i);
        }

        /*
        if (debugVariable == 30)
        {
            for (int i = 0; i < m_Players.Count; i++)
            {
                Debug.Log(m_Players[i].Name + ": " + arcLengths[i]);
            }
            debugVariable = 0;
        }
        else
        {
            debugVariable++;
        }
        */

        m_Players.Sort(new PlayerInfoComparer(arcLengths, m_Players));

        
        
        string clasificationText = "";
        for (int i = 0; i < m_Players.Count; ++i)
        {
            clasificationText += m_Players[i].Name + " \n";
            if (m_Players[i].CurrentPosition != i + 1)
            {
                m_Players[i].CurrentPosition = i + 1;
                NetworkIdentity clientID = m_Players[i].GetComponent<NetworkIdentity>();
                m_RaceInfo.TargetUpdateClasification(clientID.connectionToClient, m_Players[i].CurrentPosition);
                clasificationHasChanged = true;
            }
        }
        if (clasificationHasChanged == true)
        {
            m_RaceInfo.RpcUpdateClasificationText(clasificationText);
        }

        //m_UIManager.UpdateClasification(myRaceOrder);
        /*
        for (int i = 0; i < m_PlayerControllers.Length; i++)
        {
            if (m_PlayerControllers[i] != null)
                m_PlayerControllers[i].RpcUpdateClasification(clasificationText);
        }
        */

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

        //CalculateLap(ID, segIdx);
        //
        switch (segIdx)
        {
            case 0:
                if (m_Players[ID].circuitControlPoints[2] == true)  //Caso normal
                {
                    m_Players[ID].circuitControlPoints[2] = false;
                    m_Players[ID].circuitControlPoints[0] = true;
                    NetworkIdentity clientID = m_Players[ID].GetComponent<NetworkIdentity>();
                    if (m_Players[ID].CurrentLap == 1)  // Fin carrera
                    {
                        Debug.Log("HA GANADO EL JUGADOR: " + m_Players[ID].Name);
                        m_RaceInfo.TargetUpdateTimeLaps(clientID.connectionToClient);
                        m_RaceInfo.RpcStopTimer();
                        m_RaceInfo.RpcFinishRace(m_Players[ID].Name,m_UIManager.globalTime.ToString());
                        //m_UIManager.ActivateFinishHUD();
                        //m_UIManager.UpdateFinishList(m_RaceInfo.clasificationText);
                    }
                    m_Players[ID].CurrentLap -= 1;

                    m_RaceInfo.TargetUpdateLaps(clientID.connectionToClient, m_Players[ID].CurrentLap);
                    m_RaceInfo.TargetUpdateInGameLaps(clientID.connectionToClient);
                    m_RaceInfo.TargetUpdateTimeLaps(clientID.connectionToClient);
                    Debug.Log(m_Players[ID].Name + " ha dado una vuelta le quedan: " + m_Players[ID].CurrentLap);
                }
                else if (m_Players[ID].circuitControlPoints[1] == true)
                {
                    m_Players[ID].circuitControlPoints[1] = false;
                    m_Players[ID].circuitControlPoints[0] = true;
                }
                break;

            case 9:
                if (m_Players[ID].circuitControlPoints[0] == true)  //Caso normal
                {
                    m_Players[ID].circuitControlPoints[0] = false;
                    m_Players[ID].circuitControlPoints[1] = true;
                }
                else if (m_Players[ID].circuitControlPoints[2] == true)
                {
                    m_Players[ID].circuitControlPoints[2] = false;
                    m_Players[ID].circuitControlPoints[1] = true;
                }
                break;

            case 18:
                if (m_Players[ID].circuitControlPoints[1] == true)  //Caso normal
                {
                    m_Players[ID].circuitControlPoints[1] = false;
                    m_Players[ID].circuitControlPoints[2] = true;
                }
                else if (m_Players[ID].circuitControlPoints[0] == true)
                {
                    m_Players[ID].circuitControlPoints[0] = false;
                    m_Players[ID].circuitControlPoints[2] = true;
                    m_Players[ID].CurrentLap += 1;
                }
                break;
        }
        //
        /*
        if (this.m_Players[ID].CurrentLap <= 1)
        {
            float aux = minArcL;
            minArcL -= m_CircuitController.CircuitLength;
            if (m_Players[ID].circuitControlPoints[0] == true && -minArcL < m_CircuitController.m_CumArcLength[17])
            {
                minArcL -= m_CircuitController.CircuitLength;
            }
        }
        else
        {
            float aux = minArcL;
            minArcL -= m_CircuitController.CircuitLength * m_Players[ID].CurrentLap;
            if (m_Players[ID].circuitControlPoints[0] == true && -minArcL < - m_CircuitController.m_CumArcLength[17] + m_Players[ID].CurrentLap * m_CircuitController.CircuitLength)
            {
                minArcL -= m_CircuitController.CircuitLength;
            }
        }
        */
        if (this.m_Players[ID].CurrentLap <= 1)
        {
            minArcL -= m_CircuitController.CircuitLength;
        }
        else
        {
            minArcL -= m_CircuitController.CircuitLength * m_Players[ID].CurrentLap;
        }
        if (m_Players[ID].circuitControlPoints[0] == true && minArcL > m_CircuitController.m_CumArcLength[17] - (m_Players[ID].CurrentLap) * m_CircuitController.CircuitLength)
        {
            minArcL -= m_CircuitController.CircuitLength;
        }
        /*
        if (this.m_Players[ID].CurrentLap <= 1)
        {
            minArcL -= m_CircuitController.CircuitLength;
        }
        else
        {
            minArcL -= m_CircuitController.CircuitLength * m_Players[ID].CurrentLap;
        }
        */
        return minArcL;
    }
    
    public int CalculatePlayers()
    {
        int players = m_Players.Count;
        //Debug.Log("Numero de jugadores " + players);
 
        for (int i = 0; i < m_Players.Count; i++)
        {
            m_PlayerControllers[i].RpcUpdatePlayersConnected(players);
            m_PlayerControllers[i].RpcUpdatePlayersListLobby(CalculatePlayersList());
            
        }
        return players;
    }

    public string CalculatePlayersList()
    {
        string PlayerList = "";
        foreach (var _player in m_Players)
        {
           PlayerList += _player.Name + " \n";
        }
        return PlayerList;
    }


    //Use de ID and the segment of the circuit to check % of lap
    public void CalculateLap(int ID, int segIdx)
    {
        bool finishLap = true;
        if (segIdx == 0 && this.m_Players[ID].FirstTime)
        {
            finishLap = true;
            this.m_Players[ID].FirstTime = false;
        }
        else
        {
            if (segIdx == 0 || this.m_Players[ID].controlpoints[segIdx - 1] == true)
                this.m_Players[ID].controlpoints[segIdx] = true;
            
            foreach (bool segment in this.m_Players[ID].controlpoints)
            {
                if (!segment)
                {
                    finishLap = false;
                    break;
                }
            }  
        }
        if (segIdx == 0 && finishLap == true)
        {
            this.m_Players[ID].CurrentLap++;
            for (int i = 0; i < this.m_Players[ID].controlpoints.Length - 1; i++)
            {
                this.m_Players[ID].controlpoints[i] = false;
            }
            this.m_PlayerControllers[ID].ChangeLap();
        }
    }

    // Bloquea/desbloquea el movimiento de todos los coches de la escena
    private void FreezeAllCars(bool freeze)
    {
        for (int i = 0; i < m_PlayerControllers.Count; i++)
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
            if (m_PlayerControllers.Count <= i)
            {
                m_PlayerControllers.Add(m_Players[i].gameObject.GetComponent<PlayerController>());
            }
            //m_Players[i].CurrentLap = 3;
        }

        if (CalculatePlayers() == MaxPlayersInGame)
        {
            try
            {
                totalLaps = int.Parse(m_UIManager.textTotalLaps.text);
            }
            catch (Exception ex)
            {
                totalLaps = 5;
            }
            for (int i = 0; i < m_Players.Count; i++)
            {
                m_RaceInfo.RpcChangeColor(i, colors[i]);
                m_PlayerControllers[i].RpcActivateMyInGameHUD();
                //m_Players[i].CurrentLap = 3;
                m_Players[i].CurrentLap = totalLaps;
            }
            m_RaceInfo.RpcUpdateLaps(totalLaps);
            m_RaceInfo.RpcSetColors();
            startedRace = true;
            FreezeAllCars(true);
            countdown = new System.Timers.Timer(5000);
            countdown.AutoReset = false;
            countdown.Elapsed += ((System.Object source, System.Timers.ElapsedEventArgs e) => FreezeAllCars(false));
            countdown.Elapsed += ((System.Object source, System.Timers.ElapsedEventArgs e) => m_RaceInfo.RpcStartTimer());
            countdown.Enabled = true;
            //m_RaceInfo.RpcSwitchTimer();
        }
    }

}