using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Mirror;
using Mirror.Examples.Basic;
using UnityEngine;


public class PolePositionManager : NetworkBehaviour
{
    #region Variables
    private List<PlayerController> m_PlayerControllers = new List<PlayerController>();
    private readonly List<PlayerInfo> m_Players = new List<PlayerInfo>();
    private List<int> clasification = new List<int>();

    private CircuitController m_CircuitController;
    private RaceInfo m_RaceInfo;

    public NetworkManager networkManager;
    public UIManager m_UIManager;

    private System.Timers.Timer countdown;
    public GameObject[] m_DebuggingSpheres { get; set; }
    public string[] colors = new string[4];
    private int numPlayerFinished;
    public int numPlayers;
    public int totalLaps;
    public int MaxPlayersInGame = 4;// MAX 4
    public bool startedRace = false;
    #endregion

    #region Unity Callbacks
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
            m_DebuggingSpheres[i].GetComponent<MeshRenderer>().enabled = false;
        }
    }

    private void Update()
    {
        if (m_Players.Count == 0)
            return;
        if(startedRace) UpdateRaceProgress();
    }
    #endregion

    #region Methods

    public void AddPlayer(PlayerInfo player)
    {
        m_Players.Add(player);
        numPlayers++;
        clasification.Add(-1);
        colors[player.ID] = player.Color;
        player.GetComponent<PlayerController>().ID = player.ID;
        StartRace();
    }

    private class PlayerInfoComparer : Comparer<PlayerInfo>
    {
        Dictionary<int, float> playerLengths = new Dictionary<int, float>();

        public PlayerInfoComparer(float[] arcLengths, List<PlayerInfo> m_Players)
        {
            for(int i = 0; i < arcLengths.Length; i++)
            {
                playerLengths.Add(m_Players[i].ID, arcLengths[i]);
            }
        }

        public override int Compare(PlayerInfo x, PlayerInfo y)
        {
            if (this.playerLengths[x.ID] < this.playerLengths[y.ID]) return 1;
            else return -1;
        }
    }

    public void UpdateRaceProgress()
    {
        float[] arcLengths = new float[m_Players.Count];
        bool clasificationHasChanged = false;
        string clasificationText = "";

        for (int i = 0; i < m_Players.Count; ++i)
        {
            arcLengths[i] = ComputeCarArcLength(i);
        }

        m_Players.Sort(new PlayerInfoComparer(arcLengths, m_Players));

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
        if (clasificationHasChanged)
        {
            m_RaceInfo.RpcUpdateClasificationText(clasificationText);
        }

    }

    float ComputeCarArcLength(int id)
    {
        // Compute the projection of the car position to the closest circuit 
        // path segment and accumulate the arc-length along of the car along
        // the circuit.
        Vector3 carPos = this.m_Players[id].transform.position;

        int segIdx;
        float carDist;
        Vector3 carProj;

        float minArcL = this.m_CircuitController.ComputeClosestPointArcLength(carPos, out segIdx, out carProj, out carDist);
        
        this.m_DebuggingSpheres[id].transform.position = carProj;

        switch (segIdx)
        {
            case 0:
                if (m_Players[id].circuitControlPoints[2])  //Caso normal
                {
                    m_Players[id].circuitControlPoints[2] = false;
                    m_Players[id].circuitControlPoints[0] = true;
                    NetworkIdentity clientID = m_Players[id].GetComponent<NetworkIdentity>();
                    if (m_Players[id].CurrentLap == 1)  // Fin carrera
                    {
                        Debug.Log("HA GANADO EL JUGADOR: " + m_Players[id].Name);
                        numPlayerFinished++;
                        m_RaceInfo.TargetUpdateTimeLaps(clientID.connectionToClient);
                        m_RaceInfo.RpcStopTimer();
                        m_RaceInfo.RpcFinishRace(m_Players[id].Name,m_UIManager.globalTime.ToString());
                        m_RaceInfo.TargetDisableWinner(clientID.connectionToClient);
                        if(numPlayerFinished == MaxPlayersInGame)
                        {
                            m_RaceInfo.RpcAllPlayersFinished();
                            m_UIManager.buttonBackMenu.gameObject.SetActive(true);

                        }
                    }
                    m_Players[id].CurrentLap--;

                    m_RaceInfo.TargetUpdateLaps(clientID.connectionToClient, m_Players[id].CurrentLap);
                    m_RaceInfo.TargetUpdateInGameLaps(clientID.connectionToClient);
                    m_RaceInfo.TargetUpdateTimeLaps(clientID.connectionToClient);
                    Debug.Log(m_Players[id].Name + " ha dado una vuelta le quedan: " + m_Players[id].CurrentLap);
                }
                else if (m_Players[id].circuitControlPoints[1])
                {
                    m_Players[id].circuitControlPoints[1] = false;
                    m_Players[id].circuitControlPoints[0] = true;
                }
                break;

            case 1:
                if (m_Players[id].circuitControlPoints[0])  //Caso normal
                {
                    m_Players[id].circuitControlPoints[0] = false;
                    m_Players[id].circuitControlPoints[1] = true;
                }
                else if (m_Players[id].circuitControlPoints[2])
                {
                    m_Players[id].circuitControlPoints[2] = false;
                    m_Players[id].circuitControlPoints[1] = true;
                }
                break;

            case 2:
                if (m_Players[id].circuitControlPoints[1])  //Caso normal
                {
                    m_Players[id].circuitControlPoints[1] = false;
                    m_Players[id].circuitControlPoints[2] = true;
                }
                else if (m_Players[id].circuitControlPoints[0])
                {
                    m_Players[id].circuitControlPoints[0] = false;
                    m_Players[id].circuitControlPoints[2] = true;
                }
                break;
        }
        if (this.m_Players[id].CurrentLap == 1)
        {
            minArcL -= m_CircuitController.CircuitLength;
        } else if (this.m_Players[id].CurrentLap == 0)
        {
            minArcL = 0;
        }
        else
        {
            minArcL -= m_CircuitController.CircuitLength * m_Players[id].CurrentLap;
        }
        if (m_Players[id].circuitControlPoints[0] && minArcL > m_CircuitController.m_CumArcLength[2] - (m_Players[id].CurrentLap) * m_CircuitController.CircuitLength)
        {
            minArcL -= m_CircuitController.CircuitLength;
        }
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
        }

        if (CalculatePlayers() == MaxPlayersInGame)
        {
            try
            {
                totalLaps = int.Parse(m_UIManager.textTotalLaps.text) + 1;
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex);
                totalLaps = 5;
            }
            m_UIManager.ActivateReadyButton();
        }
    }
    
    public void StartAllPlayers()
    {
        for (int i = 0; i < m_Players.Count; i++)
        {
            m_RaceInfo.RpcChangeColor(m_Players[i].ID, colors[i]);
            m_PlayerControllers[i].RpcActivateMyInGameHUD();
            m_Players[i].CurrentLap = totalLaps;
        }

        m_RaceInfo.RpcUpdateLaps(totalLaps - 1);
        m_RaceInfo.RpcSetColors();
        startedRace = true;
        FreezeAllCars(true);
        countdown = new System.Timers.Timer(5000);
        countdown.AutoReset = false;
        countdown.Elapsed += ((System.Object source, System.Timers.ElapsedEventArgs e) => FreezeAllCars(false));
        countdown.Elapsed += ((System.Object source, System.Timers.ElapsedEventArgs e) => m_RaceInfo.RpcStartTimer());
        countdown.Enabled = true;
    }
    #endregion
}