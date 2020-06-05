using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Schema;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public bool showGUI = true;

    private NetworkManager m_NetworkManager;
    private CircuitController m_CircuitController;

    [Header("Main Menu")] [SerializeField] private GameObject mainMenu;
    [SerializeField] private Button buttonHost;
    [SerializeField] private Button buttonClient;
    [SerializeField] private Button buttonServer;
    [SerializeField] private InputField inputFieldIP;
    [SerializeField] private InputField textTotalLaps;

    [Header("In-Game HUD")] [SerializeField]
    private GameObject inGameHUD;

    [SerializeField] private Text textSpeed;
    [SerializeField] private Text textLaps;
    [SerializeField] private Text textPosition;

    private void Awake()
    {
        m_NetworkManager = FindObjectOfType<NetworkManager>();
        m_CircuitController = FindObjectOfType<CircuitController>();
    }

    private void Start()
    {
        buttonHost.onClick.AddListener(() => StartHost());
        buttonClient.onClick.AddListener(() => StartClient());
        buttonServer.onClick.AddListener(() => StartServer());
        ActivateMainMenu();
    }

    public void UpdateSpeed(int speed)
    {
        textSpeed.text = "Speed " + speed + " Km/h";
    }

    public void UpdateLap(int lap, int totalLaps)
    {
        textLaps.text = "LAP: " + lap + " / " + totalLaps; 
    }

    private void ActivateMainMenu()
    {
        mainMenu.SetActive(true);
        inGameHUD.SetActive(false);
    }

    private void ActivateInGameHUD()
    {
        InitNumberLaps();
        mainMenu.SetActive(false);
        inGameHUD.SetActive(true);
    }

    private void StartHost()
    {

        m_NetworkManager.networkAddress = inputFieldIP.text;
        m_NetworkManager.StartHost();
        ActivateInGameHUD();
    }

    private void StartClient()
    {
        m_NetworkManager.networkAddress = inputFieldIP.text;
        m_NetworkManager.StartClient();
        ActivateInGameHUD();
    }

    private void StartServer()
    {
        m_NetworkManager.networkAddress = inputFieldIP.text;
        m_NetworkManager.StartServer();
        ActivateInGameHUD();
    }

    private void InitNumberLaps()
    {
        if (textTotalLaps.text != "")
            m_CircuitController.totalLaps = int.Parse(textTotalLaps.text);
        else m_CircuitController.totalLaps = 5;
    }
}