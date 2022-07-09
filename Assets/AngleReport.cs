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

    public float startWidth = 1;
    public float endWidth = 1;
    public float distance = 10;
    public Color startColor = Color.red;
    public Color endColor = Color.red;

    // Start is called before the first frame update
    void Start()
    {
        body = GetComponent<ArticulationBody>();
        for(int i=0;i<body.jointPosition.dofCount;++i) {
            angles.Add(body.jointPosition[i]);
        }

        //LineRenderer drawLine = gameObject.AddComponent<LineRenderer>();
        //drawLine.material = new Material(Shader.Find("Sprites/Default"));
    }

    // Update is called once per frame
    void Update() {
        for (int i = 0; i < body.jointPosition.dofCount; ++i) {
            angles[i] = body.jointPosition[i];
        }

        if (visualize) {/*
            var points = new Vector3[4];
            // reference line
            points[0] = Vector3.zero;
            points[1] = new Vector3(distance,0,0);

            // show target angle
            points[2] = Vector3.zero;
            points[3] = new Vector3(
                Mathf.Cos(angles[0]) * distance,
                Mathf.Sin(angles[0]) * distance,
                0);

            LineRenderer drawLine = gameObject.GetComponent<LineRenderer>();
            drawLine.startWidth = startWidth;
            drawLine.endWidth = endWidth;
            drawLine.startColor = startColor;
            drawLine.endColor = endColor;
            drawLine.positionCount = points.Length;
            drawLine.SetPositions(points);*/
        } else {
            //drawLine.positionCount = 0;
        }
    }
}
