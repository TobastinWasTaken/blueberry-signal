using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightningArcLogic : MonoBehaviour
{
    [SerializeField] private MeshRenderer arcQuad;
    [SerializeField] private Material[] arcMaterials = new Material[4];
    [SerializeField] private Color lightningColor;
    [SerializeField] private float lifetime = 0.1f;

    [SerializeField] private Vector3 startPos, endPos;

    private Material chosenMaterial;

    private void Awake()
    {
        int rand = Mathf.FloorToInt(Random.value * arcMaterials.Length);
        chosenMaterial = arcMaterials[rand];
    }

    // Start is called before the first frame update
    void Start()
    {
        arcQuad.material = chosenMaterial;
        arcQuad.material.SetColor("_EmissionColor", lightningColor);

        transform.position = startPos;
        transform.LookAt(endPos);

        float dist = (endPos - startPos).magnitude;

        transform.localScale = new Vector3(dist, dist, dist);
    }

    // Update is called once per frame
    void Update()
    {
        lifetime -= Time.deltaTime;

        if (lifetime <= 0)
            Destroy(gameObject);
    }

    public void SetPositions(Vector3 start, Vector3 end)
    {
        startPos = start;
        endPos = end;
    }
}
