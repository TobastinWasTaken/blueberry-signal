using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TireEffectsController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TrailRenderer[] rearSkidMarks = new TrailRenderer[2];
    [SerializeField] private ParticleSystem[] rearSkidSmokes = new ParticleSystem[2];
    [SerializeField] GameObject[] staticChargeMeter = new GameObject[2];
    [SerializeField] GameObject[] staticChargeMinis = new GameObject[2];
    [SerializeField] GameObject[] staticChargeClouds = new GameObject[2];
    [SerializeField] GameObject[] chargeMeters = new GameObject[2];
    [SerializeField] Material staticChargeMiniMaterial;
    [SerializeField] GameObject lightningArcPrefab;

    [Header("Mini Lightning")]
    [SerializeField] float miniLightningTimerMax = 0.8f;
    [SerializeField] float miniLightningActiveTime = 0.4f;
    [SerializeField] float miniLightningXOffset = 0.25f;
    [SerializeField] float miniLightningTextureColumns = 4;

    float miniLightningTimer = 0f;
    bool miniLightningEnabled = false;
    bool cloudsEnabled = false;

    #region Unity Functions

    // Start is called before the first frame update
    private void Awake()
    {
        
    }

    private void Start()
    {
        EnableMiniLightning(false);
    }

    // Update is called once per frame
    void Update()
    {
        AnimateMiniLightning();
    }

    #endregion

    public void SetTireLightning(float staticChargeLevel)
    {
        for (int i = 0; i < staticChargeMeter.Length; i++)
        {
            if (staticChargeLevel > 0)
            {
                staticChargeMeter[i].SetActive(true);
                staticChargeMeter[i].transform.localScale = new Vector3(1, 1, staticChargeLevel);
            }
            else
            {
                staticChargeMeter[i].SetActive(false);
            }

        }
    }

    public void EnableMiniLightning(bool b)
    {
        if (b == miniLightningEnabled)
            return;

        miniLightningEnabled = b;
        
        for (int i = 0; i < staticChargeMinis.Length; i++)
        {
            staticChargeMinis[i].SetActive(b);
        }

        staticChargeMiniMaterial.SetTextureOffset("_MainTex", Vector2.zero);

        // Skid smoke

        for (int i = 0; i < rearSkidSmokes.Length; i++)
        {
            if (b)
                rearSkidSmokes[i].Play();
            else
                rearSkidSmokes[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    public void EnableClouds(bool b)
    {
        if (b == cloudsEnabled)
            return;

        cloudsEnabled = b;

        for (int i = 0; i < staticChargeClouds.Length; i++)
        {
            staticChargeClouds[i].SetActive(b);
        }
    }

    private void AnimateMiniLightning()
    {
        if (!miniLightningEnabled)
            return;

        if (miniLightningTimer < 0)
            miniLightningTimer = miniLightningTimerMax;

        float offset = 0;

        if (miniLightningTimer > miniLightningActiveTime)
        {
            offset = (int)Mathf.Floor(Random.value * miniLightningTextureColumns) / miniLightningTextureColumns;
        }

        Vector2 offsetVec = new Vector2(offset, 0f);

        staticChargeMiniMaterial.SetTextureOffset("_MainTex", offsetVec);

        miniLightningTimer -= Time.deltaTime;
    }

    public void SpawnArcTireToCharger()
    {
        for (int i = 0; i < staticChargeMeter.Length; i++)
        {
            GameObject arc = Instantiate(lightningArcPrefab);
            LightningArcLogic logic = arc.GetComponent<LightningArcLogic>();

            Vector3 startWorldPos = staticChargeMeter[i].transform.position;
            Vector3 endWorldPos = chargeMeters[i].transform.position;

            logic.SetPositions(startWorldPos + new Vector3(i, 0, 0), endWorldPos + new Vector3(i, 0, 0));
        }
    }
}
