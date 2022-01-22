using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class BoidController : MonoBehaviour
{
    public Slider SpeedSlider;
    public Slider TurnSlider;
    public Slider Align;
    public Slider Avoid;
    public Slider Follow;
    public Slider Flocking;

    public Text SpeedText;
    public Text TurnText;
    public Text AlignText;
    public Text AvoidText;
    public Text FollowText;
    public Text FlockingText;

    public Camera MainCam;
    public Color BoidColor;

    public ComputeShader BoidComputer;
    private ComputeBuffer BoidBuffer;

    //tribat ce ih swappat
    public RenderTexture BoidOutputTex;
    public RenderTexture BoidVelocityTexture1;
    public RenderTexture BoidVelocityTexture2;

    [Range(1024, 8192)]
    public int RenderTexResolution = 2048;

    [Range(1000, 10000000)]
    public int BoidCount = 5000;

    [Range(1f, 500f)]
    public float Speed = 5f;

    [Range(1f, 10f)]
    public float FlockGravity = 5f;


    [Range(-1f, 5f)]
    public float AlignFactor = 0.25f;
    [Range(0f, 10f)]
    public float AvoidFactor = 0.25f;
    [Range(-2f, 2f)]
    public float FollowFactor = 0.25f;
    [Range(0f, 100f)]
    public float SeparationFactor = 2f;
    [Range(1f, 10f)]
    public float AvoidRadius = 2f;



    private int ComputeId = -1;
    private int ResetId = -1;

    private Boid[] BoidsData;

    private bool Swap = false;
    //test
    public Material mainMat;
    public GameObject MainQuad;
    void Start()
    {
        InitSliders();
        InitBoids();
    }
    private void InitSliders()
    {
        SpeedSlider.value = Speed;
        TurnSlider.value = FlockGravity;
        Align.value = AlignFactor;
        Avoid.value = AvoidFactor;
        Follow.value = FollowFactor;
        Flocking.value = AvoidRadius;
    }
    private void ReadSliders()
    {
        Speed = SpeedSlider.value;
        FlockGravity = TurnSlider.value;
        AlignFactor = Align.value;
        AvoidFactor = Avoid.value;
        FollowFactor = Follow.value;
        AvoidRadius = Flocking.value;

        SpeedText.text = Speed.ToString("0");
        TurnText.text = FlockGravity.ToString("0.00");
        AlignText.text = AlignFactor.ToString("0.00");
        AvoidText.text = AvoidFactor.ToString("0.00");
        FollowText.text = FollowFactor.ToString("0.00");
        FlockingText.text = AvoidRadius.ToString("0.00");
    }

    void Update()
    {
        ReadSliders();
        CameraZoom();
        ComputeBoids();
        mainMat.SetTexture("_MainTex", BoidOutputTex);
    }

    private Vector3 MousePosOld;
    private float OrthoSize = 6f;
    public float CameraSpeed = 5f;
    public float CameraZoomSpeed = 200f;
    void CameraZoom()
    {
        MousePosOld = Input.mousePosition;

        //mouse scroll za zoom
        float Temp = Input.GetAxis("Mouse ScrollWheel");

        OrthoSize -= CameraZoomSpeed * Temp * Time.deltaTime;
        OrthoSize = Mathf.Clamp(OrthoSize, 1f, 4.5f);

        MainCam.orthographicSize = OrthoSize;
    }

    private void InitBoids()
    {
        ComputeId = BoidComputer.FindKernel("ComputeBoids");
        ResetId = BoidComputer.FindKernel("ResetTextures");

        //init RNDTX
        BoidVelocityTexture1 = new RenderTexture(RenderTexResolution, RenderTexResolution, 32);
        BoidVelocityTexture1.format = RenderTextureFormat.ARGB32;
        BoidVelocityTexture1.enableRandomWrite = true;
        BoidVelocityTexture1.Create();

        BoidVelocityTexture2 = new RenderTexture(RenderTexResolution, RenderTexResolution, 32);
        BoidVelocityTexture2.format = RenderTextureFormat.ARGB32;
        BoidVelocityTexture2.enableRandomWrite = true;
        BoidVelocityTexture2.Create();

        BoidOutputTex = new RenderTexture(RenderTexResolution, RenderTexResolution, 32);
        BoidOutputTex.format = RenderTextureFormat.ARGB32;
        BoidOutputTex.enableRandomWrite = true;
        BoidOutputTex.Create();

        //Buffer
        BoidsData = new Boid[BoidCount];
        for (int i = 0; i < BoidCount; i++)
        {
            Boid boid = new Boid();
            boid.Velocity = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
            boid.Position = new Vector2(Random.Range(0, (float)RenderTexResolution), Random.Range(0, (float)RenderTexResolution));
            boid.Speed = Random.Range(0.5f, 1.5f);
            float Alpha = Random.Range(0.0f, 1f);
            BoidsData[i] = boid;
        }

        int Size = sizeof(float) * 5;

        BoidBuffer = new ComputeBuffer(BoidsData.Length, Size);
        BoidBuffer.SetData(BoidsData);
        BoidComputer.SetBuffer(ComputeId, "Boids", BoidBuffer);


        //////Reset
        BoidComputer.SetTexture(ResetId, "BoidOutputTex", BoidOutputTex);
        BoidComputer.SetTexture(ResetId, "BoidVelocityTexture1", BoidVelocityTexture1);
        BoidComputer.SetTexture(ResetId, "BoidVelocityTexture2", BoidVelocityTexture2);

        //////result
        BoidComputer.SetTexture(ComputeId, "BoidOutputTex", BoidOutputTex);
        BoidComputer.SetTexture(ComputeId, "BoidVelocityTexture1", BoidVelocityTexture1);
        BoidComputer.SetTexture(ComputeId, "BoidVelocityTexture2", BoidVelocityTexture2);

        //setparams
        BoidComputer.SetFloat("TextureSize", RenderTexResolution);
    }


    private void SetParams()
    {
        BoidComputer.SetFloat("DeltaTime", Time.deltaTime);
        BoidComputer.SetFloat("Time", Time.realtimeSinceStartup);
        BoidComputer.SetFloat("Speed", Speed);
        BoidComputer.SetFloat("FlockGravity", FlockGravity);
        BoidComputer.SetFloat("AlignFactor", AlignFactor);
        BoidComputer.SetFloat("FollowFactor", FollowFactor);
        BoidComputer.SetFloat("AvoidFactor", AvoidFactor);
        BoidComputer.SetFloat("AvoidRadius", AvoidRadius);

        Vector2 TempCenter = MainCam.ScreenToWorldPoint(MousePosOld);
        Vector2 Center = TempCenter - (Vector2)MainQuad.transform.position;

        if (Input.GetKey(KeyCode.Mouse1))
        {
            Center = Center / 11;
            Center *= RenderTexResolution;
            Center += new Vector2(RenderTexResolution / 2, RenderTexResolution / 2);
        }
        else
            Center = new Vector2(RenderTexResolution / 2, RenderTexResolution / 2);

        // Debug.DrawLine(MainQuad.transform.position, TempCenter, Color.red, 0.2f);
        BoidComputer.SetVector("Center", Center);
    }

    private void ComputeBoids()
    {
        SetParams();

        if (Swap)
        {
            BoidComputer.SetTexture(ResetId, "BoidVelocityTexture2", BoidVelocityTexture2);
            BoidComputer.SetTexture(ComputeId, "BoidVelocityTexture1", BoidVelocityTexture1);
            BoidComputer.SetTexture(ComputeId, "BoidVelocityTexture2", BoidVelocityTexture2);
        }
        else
        {
            BoidComputer.SetTexture(ResetId, "BoidVelocityTexture2", BoidVelocityTexture1);
            BoidComputer.SetTexture(ComputeId, "BoidVelocityTexture1", BoidVelocityTexture2);
            BoidComputer.SetTexture(ComputeId, "BoidVelocityTexture2", BoidVelocityTexture1);
        }
        Swap = !Swap;

        BoidComputer.Dispatch(ResetId, RenderTexResolution / 8, RenderTexResolution / 8, 1);
        BoidComputer.Dispatch(ComputeId, BoidCount / 128, 1, 1);
    }
}
public struct Boid
{
    public Vector2 Velocity;
    public Vector2 Position;
    public float Speed;
};
