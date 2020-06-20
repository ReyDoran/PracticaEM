using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Data;

public class RaceInfo : NetworkBehaviour
{
    UIManager m_UIManager;
    PlayerController m_PlayerController;
    PlayerInfo m_PlayerInfo;
    NetworkManager networkManager;

    public string clasificationText;
    public string winners ="";

    public int laps;
    public int totalLaps;
    public string lapsInGame = "";
    private List<float> timeLaps = new List<float>();
    public string timesText = "";

    public Dictionary<int, string> colors;

    public Material blueglassMaterial;
    public Material greyMaterial;
    public Material greenMaterial;
    public Material blueMaterial;
    public Material orangeMaterial;
    public Material purpleMaterial;
    public Material pinkMaterial;
    public Material blackMaterial;
    public Material redMaterial;

    // Start is called before the first frame update
    void Start()
    {
        if (m_UIManager == null) m_UIManager = FindObjectOfType<UIManager>();
        if (m_PlayerController == null) m_PlayerController = FindObjectOfType<PlayerController>();
        if (m_PlayerInfo == null) m_PlayerInfo = FindObjectOfType<PlayerInfo>();
        if (networkManager == null) networkManager = FindObjectOfType<NetworkManager>();
        colors = new Dictionary<int, string>();
        networkManager.OnClientServerDisconnectedHandler += ServerDisconnected;
    }

    // Vuelve al menú si el servidor se desconecta
    void ServerDisconnected()
    {
        SceneManager.LoadScene("Game");
    }

    // Actualiza la clasificación de un jugador
    #region TargetRpc
    [TargetRpc]
    public void TargetUpdateClasification(NetworkConnection client, int clientClasification)
    {
        m_UIManager.UpdateMyPosition(clientClasification);
    }

    // Activa el HUD de fin de partida a un jugador
    [TargetRpc]
    public void TargetFinishRace(NetworkConnection con)
    {
        m_UIManager.ActivateFinishHUD();
        timesToString(timeLaps);
    }

    // Actualiza las vueltas restantes de un jugador
    [TargetRpc]
    public void TargetUpdateLaps(NetworkConnection client, int laps)
    {
        this.laps = laps;
        m_UIManager.UpdateLap(laps);
    }

    // Almacena el tiempo de la vuelta de un jugador
    [TargetRpc]
    public void TargetUpdateTimeLaps(NetworkConnection client)
    {
        timeLaps.Add(m_UIManager.time);
        m_UIManager.time = 0;
        Debug.Log("Tiempo de vuelta: " + timeLaps[0]);

    }

    // Actualiza el texto de tiempos de vueltas en partida
    [TargetRpc]
    public void TargetUpdateInGameLaps(NetworkConnection client)
    {
        if (laps != totalLaps + 1)
        {
            lapsInGame += m_UIManager.time.ToString() + "\n";
            m_UIManager.textTimeLaps.text = lapsInGame;
        }
    }

    // Detiene el cronómetro
    [TargetRpc]
    public void TargetStopTimer(NetworkConnection con)
    {
        m_UIManager.startedTimer = false;
    }

    #endregion

    #region ClientRpc

    // Almacena el color escogido por un jugador
    [ClientRpc]
    public void RpcChooseColor(int index, string color)
    {
        colors.Add(index, color);
    }

    // Pinta los coches
    [ClientRpc]
    public void RpcPaintCars()
    {
        PlayerInfo[] playerInfos = FindObjectsOfType<PlayerInfo>();
        Material[] Mymaterials = new Material[3];
        greyMaterial = (Material)Resources.Load("grey", typeof(Material));
        blueglassMaterial = (Material)Resources.Load("blueglass", typeof(Material));
        Mymaterials[0] = greyMaterial;
        Mymaterials[2] = blueglassMaterial;
        for (int i = 0; i < playerInfos.Length; i++)
        {
            MeshRenderer body = playerInfos[i].gameObject.GetComponentInChildren<MeshRenderer>();
            string newColor = colors[playerInfos[i].GetComponent<PlayerController>().ID];
            switch (newColor)
            {
                default:
                    redMaterial = (Material)Resources.Load("red", typeof(Material));
                    Mymaterials[1] = redMaterial;
                    break;
                case "green":
                    greenMaterial = (Material)Resources.Load("green", typeof(Material));
                    Mymaterials[1] = greenMaterial;
                    break;
                case "blue":
                    blueMaterial = (Material)Resources.Load("blue", typeof(Material));
                    Mymaterials[1] = blueMaterial;
                    break;
                case "red":
                    redMaterial = (Material)Resources.Load("red", typeof(Material));
                    Mymaterials[1] = redMaterial;
                    break;
                case "orange":
                    orangeMaterial = (Material)Resources.Load("orange", typeof(Material));
                    Mymaterials[1] = orangeMaterial;
                    break;
                case "black":
                    blackMaterial = (Material)Resources.Load("black", typeof(Material));
                    Mymaterials[1] = blackMaterial;
                    break;
                case "purple":
                    purpleMaterial = (Material)Resources.Load("purple", typeof(Material));
                    Mymaterials[1] = purpleMaterial;
                    break;
                case "pink":
                    pinkMaterial = (Material)Resources.Load("pink", typeof(Material));
                    Mymaterials[1] = pinkMaterial;
                    break;
            }
            body.materials = Mymaterials;
        }
    }

    // Actualiza la clasificación de nombres
    [ClientRpc]
    public void RpcUpdateClasificationText(string clasificationText)
    {
        this.clasificationText = clasificationText;
        m_UIManager.UpdateClasification(clasificationText);
    }

    // Actualiza el HUD de fin de carrera con los datos del nuevo jugador que ha terminado
    [ClientRpc]
    public void RpcFinishRace(string newName, string FinishTime)
    {
        winners += newName + " - " + FinishTime + "\n";
        m_UIManager.UpdateFinishList(winners);
        if(!isServer)
        m_UIManager.buttonBackMenuClient.gameObject.SetActive(true);
    }

    // Actualiza el número total de vueltas de la carrera
    [ClientRpc]
    public void RpcUpdateLaps(int laps)
    {
        this.laps = laps;
        totalLaps = laps - 1;
        FindObjectOfType<CircuitController>().totalLaps = laps;
        m_UIManager.UpdateLap(laps);
    }

    // Activa los contadores de vuelta y total
    [ClientRpc]
    public void RpcStartTimer()
    {
        m_UIManager.startedTimer = true;
        m_UIManager.startedGlobalTimer = true;
    }


    // Avisa de que todos los jugadores han terminado la carrera y activa sus botones de vuelta al menú
    [ClientRpc]
    public void RpcAllPlayersFinished()
    {
        m_UIManager.AllPlayersFinished();
        if (!isServer)
        {
            
            //NetworkManager.singleton.StopClient();
        }
        else if (isServer)
        {
            m_UIManager.buttonBackMenu.gameObject.SetActive(true);
        }

    }

    // Vuelta al menú
    [ClientRpc]
    public void RpcBackToMenu()
    {
        if (networkManager.mode == NetworkManagerMode.ServerOnly || networkManager.mode == NetworkManagerMode.Host)
        {
            NetworkManager.Shutdown();
        }
        SceneManager.LoadScene("Game");
    }

    public void timesToString(List<float> times)
    {

        timesText = "---Lap Times--- \n";

        for (int i = 1; i < times.Count; ++i)
        {
            timesText +=i +"º -> " + times[i].ToString() + " segs  \n";
        }
        m_UIManager.textTimes.text = timesText;
    }
    #endregion

}
