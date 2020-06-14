using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class RaceInfo : NetworkBehaviour
{
    UIManager m_UIManager;
    PlayerController m_PlayerController;
    private int clientClasification;
    public string clasificationText;
    public int laps;

    // Start is called before the first frame update
    void Start()
    {
        if (m_UIManager == null) m_UIManager = FindObjectOfType<UIManager>();
        if (m_UIManager == null) m_PlayerController = FindObjectOfType<PlayerController>();
    }

    [TargetRpc]
    public void TargetUpdateClasification(NetworkConnection client, int clientClasification)
    {
        m_UIManager.UpdateMyPosition(clientClasification);
    }

    [ClientRpc]
    public void RpcUpdateClasificationText(string clasificationText)
    {
        this.clasificationText = clasificationText;
        m_UIManager.UpdateClasification(clasificationText);
    }

    [ClientRpc]
    public void RpcFinishRace(string finishList)
    {
        if (laps <=1)
        {
            m_UIManager.ActivateFinishHUD();
            m_UIManager.UpdateFinishList(finishList);
        }
    }

    [TargetRpc]
    public void TargetUpdateLaps(NetworkConnection client, int laps)
    {
        this.laps = laps;
        m_UIManager.UpdateLap(laps);
    }

    [ClientRpc]
    public void RpcUpdateLaps(int laps)
    {
        this.laps = laps;
        FindObjectOfType<CircuitController>().totalLaps = laps;
        m_UIManager.UpdateLap(laps);
    }

    [ClientRpc]
    public void RpcStartTimer()
    {
        m_UIManager.startedTimer = true;
    }

    [ClientRpc]
    public void RpcStopTimer()
    {
        if (laps <= 1)
        {

            m_UIManager.startedTimer = false;
        }
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
