using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class RaceInfo : NetworkBehaviour
{
    UIManager m_UIManager;
    private int clientClasification;
    private string clasificationText;

    // Start is called before the first frame update
    void Start()
    {
        if (m_UIManager == null) m_UIManager = FindObjectOfType<UIManager>();    
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

    // Update is called once per frame
    void Update()
    {
        
    }
}
