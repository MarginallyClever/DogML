using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Display the joint positions in the neighboring ArticulationBody
 */
public class AngleReport : MonoBehaviour {
    private ArticulationBody body;

    public List<float> angles;
    public bool visualize=false;

    public float startWidth = 0.01f;
    public float endWidth = 0.01f;
    public float distance = 2;
    public Color startColor = Color.red;
    public Color endColor = Color.red;

    // Start is called before the first frame update
    void Start()
    {
        body = GetComponent<ArticulationBody>();
        for(int i=0;i<body.jointPosition.dofCount;++i) {
            angles.Add(body.jointPosition[i]);
        }

        LineRenderer drawLine = gameObject.GetComponent<LineRenderer>();
        if(drawLine==null) drawLine = gameObject.AddComponent<LineRenderer>();
        drawLine.material = new Material(Shader.Find("Sprites/Default"));
        drawLine.useWorldSpace = false;
    }

    // Update is called once per frame
    void Update() {
        for (int i = 0; i < body.jointPosition.dofCount; ++i) {
            angles[i] = body.jointPosition[i];
        }

        if (visualize) {
            var points = new Vector3[4];
            // reference line
            points[0] = Vector3.zero;
            points[1] = transform.forward * distance;

            // show target angle
            points[2] = Vector3.zero;
            points[3] = new Vector3( Mathf.Cos(angles[0]), Mathf.Sin(angles[0]), 0) * distance;

            LineRenderer drawLine = gameObject.GetComponent<LineRenderer>();
            drawLine.startWidth = startWidth;
            drawLine.endWidth = endWidth;
            drawLine.startColor = startColor;
            drawLine.endColor = endColor;
            drawLine.positionCount = points.Length;
            drawLine.SetPositions(points);
        } else {
            LineRenderer drawLine = gameObject.GetComponent<LineRenderer>();
            drawLine.positionCount = 0;
        }
    }
}
