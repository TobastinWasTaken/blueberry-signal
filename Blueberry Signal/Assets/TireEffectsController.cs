using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TireEffectsController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] GameObject staticChargeMeter;
    [SerializeField] GameObject staticChargeMinis;
    [SerializeField] GameObject staticChargeClouds;
    [SerializeField] Material staticChargeMiniMaterial;

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

    public void EnableMiniLightning(bool b)
    {
        if (b == miniLightningEnabled)
            return;

        miniLightningEnabled = b;
        staticChargeMinis.SetActive(b);
        staticChargeMiniMaterial.SetTextureOffset("_MainTex", Vector2.zero);
    }

    public void EnableClouds(bool b)
    {
        if (b == cloudsEnabled)
            return;

        cloudsEnabled = b;
        staticChargeClouds.SetActive(b);
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
}
