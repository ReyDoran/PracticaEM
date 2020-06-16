using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;

public class RaceInfo : NetworkBehaviour
{
    UIManager m_UIManager;
    PlayerController m_PlayerController;
    PlayerInfo m_PlayerInfo;
    private int clientClasification;
    public string clasificationText;
    public int laps;
    public int totalLaps;
    public string[] colors;
    public string timesText = "";
    public string lapsInGame = "";
    public string winners ="";
    float totalTime = 0;
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

        greyMaterial = (Material)Resources.Load("grey", typeof(Material));
        blueglassMaterial = (Material)Resources.Load("blueglass", typeof(Material));
        greenMaterial = (Material)Resources.Load("green", typeof(Material));
        blueMaterial = (Material)Resources.Load("blue", typeof(Material));
        redMaterial = (Material)Resources.Load("red", typeof(Material));
        orangeMaterial = (Material)Resources.Load("orange", typeof(Material));
        blackMaterial = (Material)Resources.Load("black", typeof(Material));
        purpleMaterial = (Material)Resources.Load("purple", typeof(Material));
        pinkMaterial = (Material)Resources.Load("pink", typeof(Material));
        colors = new string[4];
    }

    [TargetRpc]
    public void TargetUpdateClasification(NetworkConnection client, int clientClasification)
    {
        m_UIManager.UpdateMyPosition(clientClasification);
    }


    [ClientRpc]
    public void RpcChangeColor(int index, string color)
    {
        Debug.Log(index + " " + color);
        colors[index] = color;
    }

    [ClientRpc]
    public void RpcSetColors()
    {
        PlayerInfo[] playerInfos = FindObjectsOfType<PlayerInfo>();
        for (int i = 0; i < playerInfos.Length; i++)
        {
            Debug.Log("PlayerContrLEng:" + i);
            Debug.Log("colorslength" + colors[i]);
            MeshRenderer body = playerInfos[i].gameObject.GetComponentInChildren<MeshRenderer>();
            string newColor = colors[playerInfos[i].ID];
            Material[] Mymaterials = new Material[3];
            Mymaterials[0] = greyMaterial;
            Mymaterials[2] = blueglassMaterial;

            switch (newColor)
            {
                case "green":
                    Mymaterials[1] = greenMaterial;
                    break;
                case "blue":
                    Mymaterials[1] = blueMaterial;
                    break;
                case "red":
                    Mymaterials[1] = redMaterial;
                    break;
                case "orange":
                    Mymaterials[1] = orangeMaterial;
                    break;
                case "black":
                    Mymaterials[1] = blackMaterial;
                    break;
                case "purple":
                    Mymaterials[1] = purpleMaterial;
                    break;
                case "pink":
                    Mymaterials[1] = pinkMaterial;
                    break;
                case "":
                    Mymaterials[1] = redMaterial;
                    break;
                case null:
                    Mymaterials[1] = redMaterial;
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
            //m_UIManager.textTimes
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



    public void timesToString(List<float> times)
    {

        timesText = "---Lap Times--- \n";

        for (int i = 1; i < times.Count; ++i)
        {
            //totalTime += times[i];
            timesText +=i +"º - " + times[i].ToString() + " segs  \n";
        }
        /*
        timesText += "---Total Time---  \n" + totalTime; 
        */
        m_UIManager.textTimes.text = timesText;
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
