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
    [Header("Variables")]
    public bool showGUI = true;
    public string myColor;
    public float time = 0;
    public float globalTime = 0;
    public int numPlayers = 2;
    public int numLaps = 2;
    public bool startedTimer;
    public bool startedGlobalTimer;

    private NetworkManager m_NetworkManager;
    private CircuitController m_CircuitController;
    public RaceInfo m_RaceInfo;
    public PolePositionManager m_PolePositionManager;

    [Header("Main Menu")] [SerializeField] private GameObject mainMenu;
    [SerializeField] private Button buttonHost;
    [SerializeField] private Button buttonClient;
    [SerializeField] private Button buttonServer;
    [SerializeField] private InputField inputFieldIP;
    [SerializeField] public InputField textTotalLaps;
    [SerializeField] private InputField inputFieldName;
    [SerializeField] private InputField inputMaxPlayers;
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
    [SerializeField] private Button buttonReady;


    [Header("Finish HUD")]
    [SerializeField]
    private GameObject finishHUD;
    [SerializeField] private Text textPlayersfinished;
    [SerializeField] public Text textTimes;
    [SerializeField] public Text textWaitingPlayers;
    [SerializeField] public Button buttonBackMenu;


    private void Awake()
    {
        m_NetworkManager = FindObjectOfType<NetworkManager>();
        m_CircuitController = FindObjectOfType<CircuitController>();
        buttonReady.gameObject.SetActive(false);
        buttonBackMenu.gameObject.SetActive(false);

    }

    private void Start()
    {
        buttonHost.onClick.AddListener(() => StartHost());
        buttonClient.onClick.AddListener(() => StartClient());
        buttonServer.onClick.AddListener(() => StartServer());
        buttongreen.onClick.AddListener(()  => myColor = "green");
        buttonred.onClick.AddListener(()    => myColor="red");
        buttonorange.onClick.AddListener(() => myColor="orange");
        buttonblue.onClick.AddListener(()   => myColor="blue");
        buttonblack.onClick.AddListener(()  => myColor="black");
        buttonpurple.onClick.AddListener(() => myColor="purple");
        buttonpink.onClick.AddListener(()   => myColor="pink");
        buttonReady.onClick.AddListener(() => startRace());
        buttonBackMenu.onClick.AddListener(() => PlayersToMenu());

        ActivateMainMenu();
    }

    private void Update()
    {
        if (startedTimer)
        {
            time += Time.deltaTime;
            textTime.text = "Time : " + time.ToString("f1");
        }

        if (startedGlobalTimer)
        {
            globalTime += Time.deltaTime;
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
            textPlayersConnected.text = ("( " + playersConnected.ToString() + " / " + m_PolePositionManager.MaxPlayersInGame + " )");
        }
    }

    public void UpdatePlayerListLobby(string PlayerList)
    {
        textPlayerListLobby.text = PlayerList;
    }

    public void UpdateFinishList(string players)
    {
        textPlayersfinished.text = players;
    }

    private void ActivateMainMenu()
    {
        mainMenu.SetActive(true);
        inGameHUD.SetActive(false);
        lobbyHUD.SetActive(false);
        finishHUD.SetActive(false);
    }

    public void ActivateReadyButton()
    {
        buttonReady.gameObject.SetActive(true);
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
        Text[] TextObjects = inGameHUD.GetComponentsInChildren<Text>();
        foreach(Text text in TextObjects)
        {
            text.color = Color.black;
        }
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
        CheckIP();
        CheckNPlayers();
        m_NetworkManager.StartHost();
        ActivateLobbyHUD();
    }

    private void StartClient()
    {
        CheckIP();
        m_NetworkManager.StartClient();
        ActivateLobbyHUD();
    }

    private void StartServer()
    {
        CheckIP();
        CheckNPlayers();
        m_NetworkManager.StartServer();
        ActivateLobbyHUD();
    }
    
    private void InitNumberLaps()
    {
        if (textTotalLaps.text == "") m_CircuitController.totalLaps = numLaps;
        else m_CircuitController.totalLaps = int.Parse(textTotalLaps.text);
    }
    
    private void startRace()
    {
        m_PolePositionManager.StartAllPlayers();
    }

    public void AllPlayersFinished()
    {
        textWaitingPlayers.text = "ALL PLAYERS FINISHED THE RACE";
    }

    private void PlayersToMenu()
    {
        m_RaceInfo.RpcBackToMenu();
        NetworkServer.Shutdown();
    }

    private void CheckIP()
    {
        if (inputFieldIP.text == "") m_NetworkManager.networkAddress = "localhost";
        else m_NetworkManager.networkAddress = inputFieldIP.text;
    }
    private void CheckNPlayers()
    {
        if (inputMaxPlayers.text != "")
        {
            int num = Int16.Parse(inputMaxPlayers.text);
            if (num > 0 && num <= 4)
            {
                m_PolePositionManager.MaxPlayersInGame = Int16.Parse(inputMaxPlayers.text);
            }
            else
                m_PolePositionManager.MaxPlayersInGame = numPlayers;
        }
        else
            m_PolePositionManager.MaxPlayersInGame = numPlayers;
    }
}