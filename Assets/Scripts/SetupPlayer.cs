using System;
using Mirror;
using UnityEngine;
using UnityEngine.Networking.Types;
using Random = System.Random;

/*
	Documentation: https://mirror-networking.com/docs/Guides/NetworkBehaviour.html
	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkBehaviour.html
*/

public class SetupPlayer : NetworkBehaviour
{
    [SyncVar] private int m_ID;
    [SyncVar] private string m_Name;

    public uint My_Net_ID;
    public int Check_ID;

    private UIManager m_UIManager;
    private PlayerController m_PlayerController;
    private PlayerInfo m_PlayerInfo;
    private PolePositionManager m_PolePositionManager;

    #region Start & Stop Callbacks

    /// <summary>
    /// This is invoked for NetworkBehaviour objects when they become active on the server.
    /// <para>This could be triggered by NetworkServer.Listen() for objects in the scene, or by NetworkServer.Spawn() for objects that are dynamically created.</para>
    /// <para>This will be called for objects on a "host" as well as for object on a dedicated server.</para>
    /// </summary>
    public override void OnStartServer()
    {
        base.OnStartServer();
        My_Net_ID = this.gameObject.GetComponent<NetworkIdentity>().netId;
        m_ID = connectionToClient.connectionId;

    }

    /// <summary>
    /// Called on every NetworkBehaviour when it is activated on a client.
    /// <para>Objects on the host have this function called, as there is a local client on the host. The values of SyncVars on object are guaranteed to be initialized correctly with the latest state from the server when this function is called on the client.</para>
    /// </summary>
    public override void OnStartClient()
    {
        base.OnStartClient();
        m_PlayerInfo.ID = m_ID;
        //m_PlayerInfo.Name = "Player" + m_ID;
        m_PlayerInfo.Name = m_Name;
        m_PlayerInfo.CurrentLap = 0;
        //m_PolePositionManager.AddPlayer(m_PlayerInfo);
    }

    /// <summary>
    /// Called when the local player object has been set up.
    /// <para>This happens after OnStartClient(), as it is triggered by an ownership message from the server. This is an appropriate place to activate components or functionality that should only be active for the local player, such as cameras and input.</para>
    /// </summary>
    public override void OnStartLocalPlayer()
    {
        string name = m_UIManager.GetName();
        CmdAddPlayer(name, m_UIManager.myColor);

    }

    #endregion

    private void Awake()
    {
        m_PlayerInfo = GetComponent<PlayerInfo>();
        m_PlayerController = GetComponent<PlayerController>();
        m_PolePositionManager = FindObjectOfType<PolePositionManager>();
        m_UIManager = FindObjectOfType<UIManager>();
    }

    // Start is called before the first frame update
    void Start()
    {
        My_Net_ID = this.gameObject.GetComponent<NetworkIdentity>().netId;
        if (isLocalPlayer)
        {
            m_PlayerController.enabled = true;
            m_PlayerController.OnSpeedChangeHandler += OnSpeedChangeEvent;
            m_PlayerController.OnLapChangeHandler += OnLapChangeEvent;
            m_PlayerController.ChangeLap();
            ConfigureCamera();
        }
    }

    void OnSpeedChangeEvent(float speed)
    {
        m_UIManager.UpdateSpeed((int) speed * 5); // 5 for visualization purpose (km/h)
    }

    void OnLapChangeEvent(int lap)
    {
        m_UIManager.UpdateLap((int)lap);
    }

    void ConfigureCamera()
    {
        if (Camera.main != null) Camera.main.gameObject.GetComponent<CameraController>().m_Focus = this.gameObject;
    }

    [Command]
    void CmdAddPlayer(string name, string color)
    {
        m_PlayerInfo.Name = name;
        m_PlayerInfo.Color = color;
        m_PolePositionManager.AddPlayer(m_PlayerInfo);
    }



}