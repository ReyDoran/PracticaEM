using System;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Variables")]
    public string myColor;
    public float time = 0;
    public float globalTime = 0;
    public int numPlayers;
    public int numLaps;
    public bool startedTimer;
    public bool startedGlobalTimer;
    public bool isClient = true;

    private NetworkManager m_NetworkManager;
    private CircuitController m_CircuitController;
    public GameObject m_Reverse_Panel;
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
    [SerializeField] private Button buttonBackMenuIngame;


    [Header("Lobby HUD")]
    [SerializeField]
    private GameObject lobbyHUD;
    [SerializeField] private Text textPlayersConnected;
    [SerializeField] private Text textPlayerListLobby;
    [SerializeField] public Button buttonReady;
    [SerializeField] public Button buttonMenuServerOnly;
    [SerializeField] private Button buttonCancel;


    [Header("Finish HUD")]
    [SerializeField]
    private GameObject finishHUD;
    [SerializeField] private Text textPlayersfinished;
    [SerializeField] public Text textTimes;
    [SerializeField] public Text textWaitingPlayers;
    [SerializeField] public Button buttonBackMenu;
    [SerializeField] public Button buttonBackMenuClient;


    private void Awake()
    {
        m_NetworkManager = FindObjectOfType<NetworkManager>();
        m_CircuitController = FindObjectOfType<CircuitController>();
        buttonReady.gameObject.SetActive(false);
        buttonBackMenu.gameObject.SetActive(false);
        buttonMenuServerOnly.gameObject.SetActive(false);
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
        buttonReady.onClick.AddListener(() => StartRace());
        buttonBackMenu.onClick.AddListener(() => PlayersToMenu());
        buttonBackMenuClient.onClick.AddListener(() => ClientToMenu());
        buttonBackMenuIngame.onClick.AddListener(() => BackFromRace());
        buttonMenuServerOnly.onClick.AddListener(() => BackToMainMenu());
        buttonCancel.onClick.AddListener(() => CancelLobby());

        ChangeHudColor();
        ActivateMainMenu();
    }

    public void CancelLobby()
    {
        NetworkManager.Shutdown();
        SceneManager.LoadScene("Game");
    }

    // Actualiza cronómetros dependiendo de si es cliente o host
    private void Update()
    {  
        if (startedTimer)
        {
            time += Time.deltaTime;
            textTime.text = "Time : " + time.ToString("f1");
        }
        if (!isClient)
        {
            if (startedGlobalTimer)
            {
                globalTime += Time.deltaTime;
            }
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

    public void UpdatePlayersConnected(int playersConnected, int maxplayers)
    {
        textPlayersConnected.text = ("( " + playersConnected.ToString() + " / " + maxplayers  + " )");
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
        DesactivateHud();
        mainMenu.SetActive(true);
    }

    public void ActivateReadyButton()
    {
        buttonReady.gameObject.SetActive(true);
    }

    private void ActivateLobbyHUD()
    {
        DesactivateHud();
        UpdatePlayersConnected(0, m_PolePositionManager.MaxPlayersInGame);
        this.ChangeHudColor();
        lobbyHUD.SetActive(true);
    }

    public void ActivateInGameHUD()
    {
        CheckNumberLaps();
        DesactivateHud();
        inGameHUD.SetActive(true);
        DesActivateReverseHUD();
    }

    public void ActivateFinishHUD()
    {
        DesactivateHud();
        finishHUD.SetActive(true);    
    }

    public void ActivateReverseHUD()
    {
        m_Reverse_Panel.SetActive(true);
    }

    public void DesActivateReverseHUD()
    {
        m_Reverse_Panel.SetActive(false);
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
    
    private void StartRace()
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
    }

    private void ClientToMenu()
    {
        SceneManager.LoadScene("Game");
    }

    private void BackToMainMenu()
    {
        //m_PolePositionManager.ShutDown();
        NetworkManager.Shutdown();
        SceneManager.LoadScene("Game");
    }

    private void BackFromRace()
    {
        NetworkManager.Shutdown();
        SceneManager.LoadScene("Game");        
    }

    #region CheckInputMainMenu

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
        m_PolePositionManager.SetMaxConnections();
    }
    private void CheckNumberLaps()
    {
        if (textTotalLaps.text == "") m_CircuitController.totalLaps = 4;
        else
        {
            int num = int.Parse(textTotalLaps.text);
            if (num > 0 && num < 4)
                m_CircuitController.totalLaps = int.Parse(textTotalLaps.text);
            else m_CircuitController.totalLaps = 4;
        }
    }

    #endregion

    private void ChangeHudColor()
    {
        foreach (Text text in inGameHUD.GetComponentsInChildren<Text>())
        {
            if (text.name != "Text_REVERSE")
                text.color = Color.black;
        }
        foreach (var texto in lobbyHUD.GetComponentsInChildren<Text>())
        {
            texto.color = Color.white;
        }
    }

    private void DesactivateHud()
    {
        mainMenu.SetActive(false);
        inGameHUD.SetActive(false);
        lobbyHUD.SetActive(false);
        finishHUD.SetActive(false);
    }
}