using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FootContact : MonoBehaviour {
    private GameObject cube;
    public bool inContact;

    // Start is called before the first frame update
    void Start() {
        cube = gameObject;
        inContact = false;
    }

    // Update is called once per frame
    void Update() {
        Color color = new Color(1, 1, 1);
        inContact = false;

        RaycastHit hit;
        if(Physics.Raycast(new Ray(transform.position,-transform.up),out hit)) {
            if(hit.distance < 1 && hit.collider.gameObject.name.Equals("Terrain")) {
                color = new Color(1, 0, 0);
                inContact = true;
            }
        }
        cube.GetComponent<MeshRenderer>().material.color = color;
    }
}