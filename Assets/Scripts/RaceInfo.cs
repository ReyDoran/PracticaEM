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
    private int clientClasification;
    public string clasificationText;
    public int laps;
    public int totalLaps;
    public Dictionary<int, string> colors;
    public string timesText = "";
    public string lapsInGame = "";
    public string winners ="";
    private List<float> timeLaps = new List<float>();

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
        colors = new Dictionary<int, string>();
    }

    [TargetRpc]
    public void TargetUpdateClasification(NetworkConnection client, int clientClasification)
    {
        m_UIManager.UpdateMyPosition(clientClasification);
    }


    [ClientRpc]
    public void RpcChangeColor(int index, string color)
    {
        colors.Add(index, color);
    }

    [ClientRpc]
    public void RpcSetColors()
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
            GameObject[] PREFAB = GameObject.FindGameObjectsWithTag("Vehicle");
            string newColor = colors[playerInfos[i].ID];
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

    

    [ClientRpc]
    public void RpcUpdateClasificationText(string clasificationText)
    {
        this.clasificationText = clasificationText;
        m_UIManager.UpdateClasification(clasificationText);
    }

    [ClientRpc]
    public void RpcFinishRace(string newName,string FinishTime)
    {
        winners += newName + " - " + FinishTime +"\n";
        if (laps <= 1)
        {
            m_UIManager.ActivateFinishHUD();
            m_UIManager.UpdateFinishList(winners);
            timesToString(timeLaps);
        }        
    }

    [TargetRpc]
    public void TargetUpdateLaps(NetworkConnection client, int laps)
    {
        this.laps = laps;
        m_UIManager.UpdateLap(laps);
    }

    [TargetRpc]
    public void TargetUpdateTimeLaps(NetworkConnection client)
    {
        timeLaps.Add(m_UIManager.time);
        m_UIManager.time = 0;
        Debug.Log("Tiempo de vuelta: " + timeLaps[0]);

    }

    [TargetRpc]
    public void TargetUpdateInGameLaps(NetworkConnection client)
    {
        if (laps != totalLaps + 1)
        {
            lapsInGame += m_UIManager.time.ToString() + "\n";
            m_UIManager.textTimeLaps.text = lapsInGame;
        }
    }


    [TargetRpc]
    public void TargetDisableWinner(NetworkConnection client)
    {
        m_PlayerController.disableWinner();
    }

    [ClientRpc]
    public void RpcUpdateLaps(int laps)
    {
        this.laps = laps;
        totalLaps = laps - 1;
        FindObjectOfType<CircuitController>().totalLaps = laps;
        m_UIManager.UpdateLap(laps);
    }

    [ClientRpc]
    public void RpcStartTimer()
    {
        m_UIManager.startedTimer = true;
        m_UIManager.startedGlobalTimer = true;
    }

    [ClientRpc]
    public void RpcStopTimer()
    {
        if (laps <= 1)
        {

            m_UIManager.startedTimer = false;
        }
    }

    [ClientRpc]
    public void RpcAllPlayersFinished()
    {
        m_UIManager.AllPlayersFinished();
    }

    [ClientRpc]
    public void RpcBackToMenu()
    {
        SceneManager.LoadScene("Game");
    }

    public void timesToString(List<float> times)
    {

        timesText = "---Lap Times--- \n";

        for (int i = 1; i < times.Count; ++i)
        {
            timesText +=i +"º - " + times[i].ToString() + " segs  \n";
        }
        m_UIManager.textTimes.text = timesText;
    }

}
