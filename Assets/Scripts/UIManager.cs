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
    public string myColor;
    public float time = 0;
    public float globalTime = 0;
    public bool startedTimer;

    private NetworkManager m_NetworkManager;
    private CircuitController m_CircuitController;
    private RaceInfo m_RaceInfo;
    private PolePositionManager m_PolePositionManager;

    [Header("Main Menu")] [SerializeField] private GameObject mainMenu;
    [SerializeField] private Button buttonHost;
    [SerializeField] private Button buttonClient;
    [SerializeField] private Button buttonServer;
    [SerializeField] private InputField inputFieldIP;
    [SerializeField] public InputField textTotalLaps;
    [SerializeField] private InputField inputFieldName;
    [SerializeField] private Button buttongreen;
    [SerializeField] private Button buttonred;
    [SerializeField] private Button buttonorange;
    [SerializeField] private Button buttonblack;
    [SerializeField] private Button buttonblue;
    [SerializeField] private Button buttonpurple;
    [SerializeField] private Button buttonpink;


    [Header("In-Game HUD")]
    [SerializeField]
    private GameObject inGameHUD;
    [SerializeField] private GameObject semaphore;

    [SerializeField] private Text textSpeed;
    [SerializeField] private Text textLaps;
    [SerializeField] private Text textPosition;
    [SerializeField] private Text textMyPosition;
    [SerializeField] private Text textTime;
    [SerializeField] public Text textTimeLaps;


    [Header("Lobby HUD")]
    [SerializeField]
    private GameObject lobbyHUD;
    [SerializeField] private Text textPlayersConnected;
    [SerializeField] private Text textPlayerListLobby;


    [Header("Finish HUD")]
    [SerializeField]
    private GameObject finishHUD;
    [SerializeField] private Text textPlayersfinished;
    [SerializeField] public Text textTimes;


    private void Awake()
    {
        m_NetworkManager = FindObjectOfType<NetworkManager>();
        m_CircuitController = FindObjectOfType<CircuitController>();
        m_PolePositionManager = FindObjectOfType<PolePositionManager>();
        m_RaceInfo = FindObjectOfType<RaceInfo>();
    }

    private void Start()
    {
        buttonHost.onClick.AddListener(() => StartHost());
        buttonClient.onClick.AddListener(() => StartClient());
        buttonServer.onClick.AddListener(() => StartServer());
        buttongreen.onClick.AddListener(() => SetColor("green"));
        buttonred.onClick.AddListener(() => SetColor("red"));
        buttonorange.onClick.AddListener(() => SetColor("orange"));
        buttonblue.onClick.AddListener(() => SetColor("blue"));
        buttonblack.onClick.AddListener(() => SetColor("black"));
        buttonpurple.onClick.AddListener(() => SetColor("purple"));
        buttonpink.onClick.AddListener(() => SetColor("pink"));

        ActivateMainMenu();
    }

    private void Update()
    {
        if (startedTimer)
        {
            time += Time.deltaTime;
            globalTime += Time.deltaTime;
            textTime.text = "Time : " + time.ToString("f1");
        }
    }

    public void UpdateSpeed(int speed)
    {
        textSpeed.text = "Speed " + speed + " Km/h";
    }

    public void UpdateLap(int lap)
    {
        textLaps.text = "LAP: " + lap + " / " + m_CircuitController.totalLaps;
    }

    public void UpdateClasification(string clasification)
    {
        textPosition.text = clasification;
    }

    public void UpdateMyPosition(int position)
    {
        textMyPosition.text = position.ToString() + " º";
    }

    public string GetName()
    {
        name = inputFieldName.text;
        return name;
    }

    public void UpdatePlayersConnected(int playersConnected)
    {
        if (lobbyHUD.activeSelf)
        {
            textPlayersConnected.text = ("( " + playersConnected.ToString()+ " / "+ m_PolePositionManager.MaxPlayersInGame + " )");
        }
    }


    public void UpdatePlayerListLobby(string PlayerList)
    {
        textPlayerListLobby.text = PlayerList;
    }

    public void UpdateFinishList(string players)
    {
        textPlayersfinished.text = players;
        //textTimes.text = times;

    }


    public void SetColor(string color)
    {
        myColor = color;
    }

    public string GetColor()
    {
        return myColor;
    }

    private void ActivateMainMenu()
    {
        mainMenu.SetActive(true);
        inGameHUD.SetActive(false);
        lobbyHUD.SetActive(false);
        finishHUD.SetActive(false);
    }

    private void ActivateLobbyHUD()
    {
        mainMenu.SetActive(false);
        inGameHUD.SetActive(false);
        lobbyHUD.SetActive(true);
        finishHUD.SetActive(false);
    }

    public void ActivateInGameHUD()
    {
        InitNumberLaps();
        mainMenu.SetActive(false);
        inGameHUD.SetActive(true);
        lobbyHUD.SetActive(false);
        finishHUD.SetActive(false);

    }

    public void ActivateFinishHUD()
    { 
            mainMenu.SetActive(false);
            inGameHUD.SetActive(false);
            lobbyHUD.SetActive(false);
            finishHUD.SetActive(true);
        
    }

    private void StartHost()
    {
        if (inputFieldIP.text == "") m_NetworkManager.networkAddress = "localhost";
        else m_NetworkManager.networkAddress = inputFieldIP.text;
        m_NetworkManager.StartHost();
        ActivateLobbyHUD();
    }

    private void StartClient()
    {
        if (inputFieldIP.text == "") m_NetworkManager.networkAddress = "localhost";
        else m_NetworkManager.networkAddress = inputFieldIP.text;
        m_NetworkManager.StartClient();
        ActivateLobbyHUD();
    }

    private void StartServer()
    {
        if (inputFieldIP.text == "") m_NetworkManager.networkAddress = "localhost";
        else m_NetworkManager.networkAddress = inputFieldIP.text;
        m_NetworkManager.StartServer();
        ActivateLobbyHUD();
    }

    private void InitNumberLaps()
    {
        if (textTotalLaps.text == "") m_CircuitController.totalLaps = 5;
        else m_CircuitController.totalLaps = int.Parse(textTotalLaps.text);
    }
}