using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Mirror;
using Mirror.Examples.Basic;
using UnityEngine;


public class PolePositionManager : NetworkBehaviour
{
    #region Variables
    private readonly List<PlayerController> m_PlayerControllers = new List<PlayerController>();
    private readonly List<PlayerInfo> m_Players = new List<PlayerInfo>();
    private List<int> clasification = new List<int>();

    private CircuitController m_CircuitController;
    private RaceInfo m_RaceInfo;

    public NetworkManager networkManager;
    public UIManager m_UIManager;

    private System.Timers.Timer countdown;
    public GameObject[] m_DebuggingSpheres { get; set; }
    public Dictionary<int, string> colors = new Dictionary<int, string>();
    public int numPlayers;
    public int totalLaps;
    public bool startedRace = false;
    private int numPlayerFinished;

    public int MaxPlayersInGame;
    private int playersConected;
    #endregion

    #region Unity Callbacks
    private void Awake()
    {
        if (networkManager == null) networkManager = FindObjectOfType<NetworkManager>();
        if (m_CircuitController == null) m_CircuitController = FindObjectOfType<CircuitController>();
        if (m_UIManager == null) m_UIManager = FindObjectOfType<UIManager>();
        if (m_RaceInfo == null) m_RaceInfo = FindObjectOfType<RaceInfo>();

        networkManager.OnServerClientDisconnectedHandler += ClientDisconnected; // Evento de cliente desconectado

        m_DebuggingSpheres = new GameObject[networkManager.maxConnections];
        for (int i = 0; i < networkManager.maxConnections; ++i)
        {
            m_DebuggingSpheres[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            m_DebuggingSpheres[i].GetComponent<SphereCollider>().enabled = false;
            m_DebuggingSpheres[i].GetComponent<MeshRenderer>().enabled = false;
        }
        m_UIManager.isClient = false;
    }

    // Elimina a un jugador de la carrera
    public void ClientDisconnected(int clientID)
    {
        MaxPlayersInGame -= 1;
        string name = "";
        for (int i = 0; i < m_Players.Count; i++)
        {
            if (m_Players[i].ID == clientID)
            {
                name = m_Players[i].Name;
                m_Players.Remove(m_Players[i]);
            }
        }
        string clasificationText = "";
        for (int i = 0; i < m_Players.Count; i++)
        {
            clasificationText += m_Players[i].Name + "\n";
            m_RaceInfo.TargetUpdateClasification(m_Players[i].GetComponent<NetworkIdentity>().connectionToClient, i + 1); ;
        }
        m_RaceInfo.RpcUpdateClasificationText(clasificationText);
        if (numPlayerFinished == MaxPlayersInGame || MaxPlayersInGame==1)   // Acaba la partida si no quedan jugadores por terminar
        {
            m_RaceInfo.RpcFinishRace(clasificationText,"WINNER");
            m_RaceInfo.RpcAllPlayersFinished();
            m_UIManager.buttonBackMenu.gameObject.SetActive(true);
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

    // Añade a un jugador
    public void AddPlayer(PlayerInfo player)
    {
        m_Players.Add(player);
        numPlayers++;
        clasification.Add(-1);
        colors.Add(player.ID, player.Color);
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

    // Actualiza las posiciones de los jugadores y las vueltas
    public void UpdateRaceProgress()
    {
        // Crea las variables para pasar al Parallel.For
        NetworkIdentity[] clientID = new NetworkIdentity[m_Players.Count];
        PlayerController[] auxPlayerController = new PlayerController[m_Players.Count];
        Vector3[] carProjs = new Vector3[m_Players.Count];
        Vector3[] carPos = new Vector3[m_Players.Count];
        bool[] dirChanged = new bool[m_Players.Count];
        bool[] wrongDir = new bool[m_Players.Count];
        float[] arcLengths = new float[m_Players.Count];
        int[] segmentsId = new int[m_Players.Count];
        
        bool clasificationHasChanged = false;
        string clasificationText = "";

        for (int i = 0; i < m_Players.Count; i++)
        {
            if (m_Players[i] != null)
            {
                carPos[i] = this.m_Players[i].transform.position;
                clientID[i] = m_Players[i].GetComponent<NetworkIdentity>();
                auxPlayerController[i] = m_Players[i].GetComponent<PlayerController>();
            }
        }

        bool activateBackMenu = false;
        // Calcula las posiciones de los coches y actualiza las vueltas y comprueba si algún coche está recorriendo marcha atrás
        Parallel.For(0, m_Players.Count, i =>
        {
            if (m_Players[i] != null)
            {
                arcLengths[i] = ComputeCarArcLength(i, carPos, clientID, auxPlayerController, out activateBackMenu, out Vector3 carProj, out int segIdx);
                carProjs[i] = carProj;
                segmentsId[i] = segIdx;

                if (segIdx != m_Players[i].previousSegmentsId)
                {
                    dirChanged[i] = true;
                    if (segIdx < m_Players[i].previousSegmentsId)
                    {
                        if (segIdx == 0 && m_Players[i].previousSegmentsId == 23)
                        {
                            wrongDir[i] = false;
                        }
                        else
                        {
                            wrongDir[i] = true;
                        }
                    }
                    else
                    {
                        if (segIdx == 23 && m_Players[i].previousSegmentsId == 0)
                        {
                            wrongDir[i] = true;
                        }
                        else
                        {
                            wrongDir[i] = false;
                        }
                    }
                }
                else
                {
                    dirChanged[i] = false;
                }

                m_Players[i].previousSegmentsId = segIdx;
            }
        });
        
        if (activateBackMenu) 
        {
            m_UIManager.buttonBackMenu.gameObject.SetActive(true);
        }

        m_Players.Sort(new PlayerInfoComparer(arcLengths, m_Players));

        // Envía mensajes de actualización de clasificación y de ir marcha atrás
        for (int i = 0; i < m_Players.Count; ++i)
        {
            if (m_Players[i] != null)
            {
                this.m_DebuggingSpheres[i].transform.position = carProjs[i];
                clasificationText += m_Players[i].Name + " \n";

                if (m_Players[i].CurrentPosition != i + 1)
                {
                    m_Players[i].CurrentPosition = i + 1;
                    clientID[i] = m_Players[i].GetComponent<NetworkIdentity>();
                    m_RaceInfo.TargetUpdateClasification(clientID[i].connectionToClient, m_Players[i].CurrentPosition);
                    clasificationHasChanged = true;
                }

                if (dirChanged[i])
                {
                    if (wrongDir[i])
                    {
                        m_Players[i].GetComponent<PlayerController>().TargetRpcCheck_REVERSE(clientID[i].connectionToClient, wrongDir[i]);
                    }
                    else
                    {
                        m_Players[i].GetComponent<PlayerController>().TargetRpcCheck_REVERSE(clientID[i].connectionToClient, wrongDir[i]);
                    }
                }
            }            
        }
        if (clasificationHasChanged)
        {
            m_RaceInfo.RpcUpdateClasificationText(clasificationText);
        }
    }

    // Asigna el número máximo de conexiones
    public void SetMaxConnections()
    {
        networkManager.maxConnections = MaxPlayersInGame;
    }

    /*
     * Calcula la longitud de arco de cada coche y actualiza su número de vuelta cada vez que completa una vuelta
     * Además envía mensajes a los jugadores de actualizar sus tiempos de vueltas y de activar el HUD de fin de partida.
     */
    float ComputeCarArcLength(int id, Vector3[] carPos, NetworkIdentity[] clientID, PlayerController[] auxPlayerController, out bool activateBackMenu, out Vector3 carProj, out int segIdx)
    {
        // Compute the projection of the car position to the closest circuit 
        // path segment and accumulate the arc-length along of the car along
        // the circuit.

        float minArcL = this.m_CircuitController.ComputeClosestPointArcLength(carPos[id], out segIdx, out carProj, out float carDist);
        activateBackMenu = false;     
        switch (segIdx)
        {
            case 0:
                if (m_Players[id].circuitControlPoints[2])  //Caso normal
                {
                    m_Players[id].circuitControlPoints[2] = false;
                    m_Players[id].circuitControlPoints[0] = true;
                    if (m_Players[id].CurrentLap == 1)  // Fin carrera
                    {
                        //Debug.Log("HA GANADO EL JUGADOR: " + m_Players[id].Name);
                        numPlayerFinished++;
                        m_RaceInfo.TargetUpdateTimeLaps(clientID[id].connectionToClient);
                        m_RaceInfo.TargetStopTimer(clientID[id].connectionToClient);
                        //Debug.Log("tiempo " + m_UIManager.globalTime.ToString());
                        m_RaceInfo.RpcFinishRace(m_Players[id].Name, m_UIManager.globalTime.ToString());
                        m_RaceInfo.TargetFinishRace(clientID[id].connectionToClient);
                        auxPlayerController[id].TargetDisableWinner(clientID[id].connectionToClient);                        
                        if (numPlayerFinished == MaxPlayersInGame)
                        {
                            m_RaceInfo.RpcAllPlayersFinished();
                            activateBackMenu = true;
                        }
                    }

                    m_Players[id].CurrentLap--;

                    m_RaceInfo.TargetUpdateLaps(clientID[id].connectionToClient, m_Players[id].CurrentLap);
                    m_RaceInfo.TargetUpdateInGameLaps(clientID[id].connectionToClient);
                    m_RaceInfo.TargetUpdateTimeLaps(clientID[id].connectionToClient);
                    //Debug.Log(m_Players[id].Name + " ha dado una vuelta le quedan: " + m_Players[id].CurrentLap);
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
            minArcL = 4 - m_Players[id].CurrentPosition;
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

    // Calcula el texto de clasificación
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

    // Comprueba cuando estan unidos todos los jugadores para permitir empezar la partida
    public void StartRace()
    {
        playersConected = m_Players.Count;

        for (int i = 0; i < playersConected; i++)
        {
            if (m_PlayerControllers.Count <= i)
            {
                m_PlayerControllers.Add(m_Players[i].gameObject.GetComponent<PlayerController>());
            }
        }

        UpdateLobbyUI();

        if (playersConected == MaxPlayersInGame)
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
    
    // Inicia la partida para todos los jugadores.
    // Los bloquea durante los 5 primeros segundos y actualiza los colores de los coches
    public void StartAllPlayers()
    {
        m_UIManager.buttonReady.gameObject.SetActive(false);
        m_UIManager.buttonMenuServerOnly.gameObject.SetActive(true);

        for (int i = 0; i < m_Players.Count; i++)
        {
            m_RaceInfo.RpcChooseColor(m_Players[i].ID, colors[m_Players[i].ID]);
            m_PlayerControllers[i].RpcActivateMyInGameHUD();
            m_Players[i].CurrentLap = totalLaps;
        }

        m_RaceInfo.RpcUpdateLaps(totalLaps - 1);
        m_RaceInfo.RpcPaintCars();
        
        startedRace = true;
        FreezeAllCars(true);
        countdown = new System.Timers.Timer(5000);
        countdown.AutoReset = false;
        countdown.Elapsed += ((source, e) => FreezeAllCars(false));
        countdown.Elapsed += ((source, e) =>
        {
            m_RaceInfo.RpcStartTimer();
            if (isServerOnly)
                m_UIManager.startedGlobalTimer = true;
        });
        countdown.Enabled = true;
    }

    // Actualiza la lista de jugadores conectados en el lobby
    private void UpdateLobbyUI()
    {
        foreach (PlayerController PC in m_PlayerControllers)
        {
            PC.RpcUpdatePlayersConnected(playersConected,MaxPlayersInGame);
            PC.RpcUpdatePlayersListLobby(CalculatePlayersList());

        }
        //Actualizar UI de ServerOnly
        this.m_UIManager.UpdatePlayerListLobby(CalculatePlayersList());
        this.m_UIManager.UpdatePlayersConnected(playersConected, MaxPlayersInGame);
    }

    public void ShutDown()
    {
        NetworkManager.Shutdown();
    }
    #endregion
}