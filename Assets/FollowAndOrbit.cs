using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowAndOrbit : MonoBehaviour
{
    public Transform toFollow = default;

    public float pan = 0;
    private float panStart;

    [Range(-90f, 90f)] 
    public float tilt = 0;
    private float tiltStart;

    [Range(1f, 20f)] 
    public float distance = 10;

    private bool mouseOn;

    // Start is called before the first frame update
    void Start() {
        mouseOn = false;
        pan = transform.eulerAngles.y;
        tilt = transform.eulerAngles.x;
    }

    // Update is called once per frame
    void Update() {
        distance = Mathf.Clamp(distance + Input.mouseScrollDelta.y, 1, 20);

        if (Input.GetMouseButton(1)) {
            if(mouseOn==false) {
                panStart = Input.mousePosition.x;
                tiltStart = Input.mousePosition.y;
            }
            mouseOn = true;

            pan += Input.mousePosition.x - panStart;

            tilt += Input.mousePosition.y - tiltStart;
            tilt = Mathf.Clamp(tilt, -90, 90);

            transform.eulerAngles = new Vector3(tilt, pan, 0);
            panStart = Input.mousePosition.x;
            tiltStart = Input.mousePosition.y;
        } else {
            mouseOn = false;
        }

        if (toFollow != null) {
            transform.position = toFollow.transform.position + transform.forward * -distance;
        }
    }
}
