using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Reflection.Emit;
using Assets.Scripts;
using UnityEngine.UI;
using Random = System.Random;
using System.Timers;

/*
	Documentation: https://mirror-networking.com/docs/Guides/NetworkBehaviour.html
	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkBehaviour.html
*/

public class PlayerController : NetworkBehaviour
{
    #region Variables
    [Header("Textures")]
    public Material blueglassMaterial;
    public Material greyMaterial;
    public Material greenMaterial;
    public Material blueMaterial;
    public Material orangeMaterial;
    public Material purpleMaterial;
    public Material pinkMaterial;
    public Material blackMaterial;
    public Material redMaterial;
    public String color;

    [Header("Movement")] 
    public List<AxleInfo> axleInfos;
    public float forwardMotorTorque = 100000;
    public float backwardMotorTorque = 50000;
    public float maxSteeringAngle = 15;
    public float engineBrake = 1e+12f;
    public float footBrake = 1e+24f;
    public float topSpeed = 200f;
    public float downForce = 100f;
    public float slipLimit = 0.2f;

    private CircuitController m_CircuitController;
    private float CurrentRotation { get; set; }
    private float InputAcceleration { get; set; }
    private float InputSteering { get; set; }
    private float InputBrake { get; set; }
    private Boolean InputReset { get; set; }

    private PlayerInfo m_PlayerInfo;
    private Rigidbody m_Rigidbody;
    private float m_SteerHelper = 0.8f;
    private float m_CurrentSpeed = 0;
    private UIManager m_UIManager;

    private PolePositionManager m_PolePositionManager;
    private RaceInfo m_RaceInfo;


    private Text textMyName;
    private int depuracionInt = 0;

    public delegate void OnLapChangeDelegate(int newLap);

    public event OnLapChangeDelegate OnLapChangeHandler;

    private float Speed
    {
        get { return m_CurrentSpeed; }
        set
        {
            if (Math.Abs(m_CurrentSpeed - value) < float.Epsilon) return;
            m_CurrentSpeed = value;
            if (OnSpeedChangeHandler != null)
                OnSpeedChangeHandler(m_CurrentSpeed);
        }
    }

    public delegate void OnSpeedChangeDelegate(float newVal);

    public event OnSpeedChangeDelegate OnSpeedChangeHandler;

    #endregion Variables

    #region Unity Callbacks

    public void Awake()
    {
 
        m_Rigidbody = GetComponent<Rigidbody>();
        m_PlayerInfo = GetComponent<PlayerInfo>();
        //m_PolePositionManager = FindObjectOfType<PolePositionManager>();
        m_UIManager = FindObjectOfType<UIManager>();
        if (m_CircuitController == null) m_CircuitController = FindObjectOfType<CircuitController>();
        if (m_RaceInfo == null) m_RaceInfo = FindObjectOfType<RaceInfo>();
        greyMaterial = (Material)Resources.Load("grey", typeof(Material));
        blueglassMaterial = (Material)Resources.Load("blueglass", typeof(Material));
        greenMaterial = (Material)Resources.Load("green", typeof(Material));
        blueMaterial = (Material)Resources.Load("blue", typeof(Material));
        redMaterial = (Material)Resources.Load("red", typeof(Material));
        orangeMaterial = (Material)Resources.Load("orange", typeof(Material));
        blackMaterial = (Material)Resources.Load("black", typeof(Material));
        purpleMaterial = (Material)Resources.Load("purple", typeof(Material));
        pinkMaterial = (Material)Resources.Load("pink", typeof(Material));
        m_Rigidbody.constraints = RigidbodyConstraints.FreezeAll;

        //ChangeColor();
    }

    public void Update()
    {
        InputAcceleration = Input.GetAxis("Vertical");
        InputSteering = Input.GetAxis(("Horizontal"));
        InputBrake = Input.GetAxis("Jump");
        InputReset = Input.GetKey(KeyCode.Escape);
        Speed = m_Rigidbody.velocity.magnitude;

    }

    public void FixedUpdate()
    {
        InputSteering = Mathf.Clamp(InputSteering, -1, 1);
        InputAcceleration = Mathf.Clamp(InputAcceleration, -1, 1);
        InputBrake = Mathf.Clamp(InputBrake, 0, 1);

        float steering = maxSteeringAngle * InputSteering;

        Timer countdown;

        //If esc key is pressed the car is recolocated in the middle of the track
        if (InputReset)
        {
            InputReset = false;

            int segIdx;
            float carDist;
            Vector3 carProj;
            Vector3 posReset;
            float angleReset;
            Vector3 tempVector;

            posReset = this.transform.position;

            float minArcL =
                this.m_CircuitController.ComputeClosestPointArcLength(posReset, out segIdx, out carProj, out carDist);

            carProj.y += 0.5f;

            tempVector = this.m_CircuitController.GetSegment(segIdx);
            angleReset = Vector2.Angle(new Vector2(tempVector.x, tempVector.z), new Vector2(0.0f, 1.0f));

            if (segIdx > 16 || segIdx < 2)
            {
                angleReset = 360 - angleReset;
            }

            this.m_PlayerInfo.transform.position = carProj;
            this.m_PlayerInfo.transform.eulerAngles = new Vector3(this.m_PlayerInfo.transform.eulerAngles.x, angleReset, 0.0f);

            float templimit = topSpeed;
            topSpeed = 0;
            countdown = new System.Timers.Timer(1000);
            countdown.AutoReset = false;
            countdown.Elapsed += ((source, e) => topSpeed = templimit);
            countdown.Enabled = true;
        }
        else
        {
            foreach (AxleInfo axleInfo in axleInfos)
            {
                if (axleInfo.steering)
                {
                    axleInfo.leftWheel.steerAngle = steering;
                    axleInfo.rightWheel.steerAngle = steering;
                }

                if (axleInfo.motor)
                {
                    if (InputAcceleration > float.Epsilon)
                    {
                        axleInfo.leftWheel.motorTorque = forwardMotorTorque;
                        axleInfo.leftWheel.brakeTorque = 0;
                        axleInfo.rightWheel.motorTorque = forwardMotorTorque;
                        axleInfo.rightWheel.brakeTorque = 0;
                    }

                    if (InputAcceleration < -float.Epsilon)
                    {
                        axleInfo.leftWheel.motorTorque = -backwardMotorTorque;
                        axleInfo.leftWheel.brakeTorque = 0;
                        axleInfo.rightWheel.motorTorque = -backwardMotorTorque;
                        axleInfo.rightWheel.brakeTorque = 0;
                    }

                    if (Math.Abs(InputAcceleration) < float.Epsilon)
                    {
                        axleInfo.leftWheel.motorTorque = 0;
                        axleInfo.leftWheel.brakeTorque = engineBrake;
                        axleInfo.rightWheel.motorTorque = 0;
                        axleInfo.rightWheel.brakeTorque = engineBrake;
                    }

                    if (InputBrake > 0)
                    {
                        axleInfo.leftWheel.brakeTorque = footBrake;
                        axleInfo.rightWheel.brakeTorque = footBrake;
                    }
                }

                ApplyLocalPositionToVisuals(axleInfo.leftWheel);
                ApplyLocalPositionToVisuals(axleInfo.rightWheel);
            }
        }
        SteerHelper();
        SpeedLimiter();
        AddDownForce();
        TractionControl();
    }

    private void Depuracion()
    {
        String[] colores = new String[5] {"pink", "red", "orange", "purple", "black"};
        m_UIManager.myColor = colores[depuracionInt];
        depuracionInt++;
        if (depuracionInt >= 5)
        {
            depuracionInt = 0;
        }
        this.ChangeColor();
    }

    public void ChangeColor()
    {
        string newColor = m_UIManager.myColor;
        m_PlayerInfo.Color = newColor;

        GameObject body = transform.Find("raceCarRed").transform.Find("body").gameObject;
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
        body.GetComponent<Renderer>().materials = Mymaterials;
    }

    #endregion

    #region Methods

    // crude traction control that reduces the power to wheel if the car is wheel spinning too much
    private void TractionControl()
    {
        foreach (var axleInfo in axleInfos)
        {
            WheelHit wheelHitLeft;
            WheelHit wheelHitRight;
            axleInfo.leftWheel.GetGroundHit(out wheelHitLeft);
            axleInfo.rightWheel.GetGroundHit(out wheelHitRight);

            if (wheelHitLeft.forwardSlip >= slipLimit)
            {
                var howMuchSlip = (wheelHitLeft.forwardSlip - slipLimit) / (1 - slipLimit);
                axleInfo.leftWheel.motorTorque -= axleInfo.leftWheel.motorTorque * howMuchSlip * slipLimit;
            }

            if (wheelHitRight.forwardSlip >= slipLimit)
            {
                var howMuchSlip = (wheelHitRight.forwardSlip - slipLimit) / (1 - slipLimit);
                axleInfo.rightWheel.motorTorque -= axleInfo.rightWheel.motorTorque * howMuchSlip * slipLimit;
            }
        }
    }

// this is used to add more grip in relation to speed
    private void AddDownForce()
    {
        foreach (var axleInfo in axleInfos)
        {
            axleInfo.leftWheel.attachedRigidbody.AddForce(
                -transform.up * (downForce * axleInfo.leftWheel.attachedRigidbody.velocity.magnitude));
        }
    }

    private void SpeedLimiter()
    {
        float speed = m_Rigidbody.velocity.magnitude;
        if (speed > topSpeed)
            m_Rigidbody.velocity = topSpeed * m_Rigidbody.velocity.normalized;
    }

    /* Asigna a topSpeed 0 para bloquear el movimiento del coche,
     * o restaura el valor previo
     * Es RPC para que se ejecute en los clientes, no en el servidor.
     */
     [ClientRpc]
    public void RpcFreezeCar(bool freeze)
    {
        if (freeze == true)
            m_Rigidbody.constraints = RigidbodyConstraints.FreezeAll;
        else
            m_Rigidbody.constraints = RigidbodyConstraints.None;
    }

    [ClientRpc]
    public void RpcUpdateClasification(string clasification)
    {
        m_UIManager.UpdateClasification(clasification);
    }

// finds the corresponding visual wheel
// correctly applies the transform
    public void ApplyLocalPositionToVisuals(WheelCollider col)
    {
        if (col.transform.childCount == 0)
        {
            return;
        }

        Transform visualWheel = col.transform.GetChild(0);
        Vector3 position;
        Quaternion rotation;
        col.GetWorldPose(out position, out rotation);
        var myTransform = visualWheel.transform;
        myTransform.position = position;
        myTransform.rotation = rotation;
    }

    private void SteerHelper()
    {
        foreach (var axleInfo in axleInfos)
        {
            WheelHit[] wheelHit = new WheelHit[2];
            axleInfo.leftWheel.GetGroundHit(out wheelHit[0]);
            axleInfo.rightWheel.GetGroundHit(out wheelHit[1]);
            foreach (var wh in wheelHit)
            {
                if (wh.normal == Vector3.zero)
                    return; // wheels arent on the ground so dont realign the rigidbody velocity
            }
        }

// this if is needed to avoid gimbal lock problems that will make the car suddenly shift direction
        if (Mathf.Abs(CurrentRotation - transform.eulerAngles.y) < 10f)
        {
            var turnAdjust = (transform.eulerAngles.y - CurrentRotation) * m_SteerHelper;
            Quaternion velRotation = Quaternion.AngleAxis(turnAdjust, Vector3.up);
            m_Rigidbody.velocity = velRotation * m_Rigidbody.velocity;
        }

        CurrentRotation = transform.eulerAngles.y;
    }

    private void GetLap()
    {
       
    }
    [TargetRpc]
    public void TargetUpdateMyPosition(NetworkConnection client, int position)
    {

        m_UIManager.UpdateMyPosition(position);
    }

    [ClientRpc]
    public void RpcActivateMyInGameHUD()
    {
        m_UIManager.ActivateInGameHUD();
    }

    [ClientRpc]
    public void RpcUpdatePlayersConnected(int players)
    {
        m_UIManager.UpdatePlayersConnected(players);
    }

    [ClientRpc]
    public void RpcUpdatePlayersListLobby(string playerList)
    {
        m_UIManager.UpdatePlayerListLobby(playerList);
    }

    public void ChangeLap()
    {
        if (OnLapChangeHandler != null)
            OnLapChangeHandler(this.m_PlayerInfo.CurrentLap);
    }

    public void disableWinner()
    {
        m_Rigidbody.constraints = RigidbodyConstraints.FreezeAll;
        
        m_Rigidbody.gameObject.SetActive(false);
    }
    #endregion
}