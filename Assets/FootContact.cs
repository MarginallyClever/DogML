using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FootContact : MonoBehaviour {
    public bool inContact;
    private MeshRenderer meshRenderer;

    // Start is called before the first frame update
    void Start() {
        inContact = false;
        meshRenderer = gameObject.GetComponent<MeshRenderer>();
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
        meshRenderer.material.color = color;
    }
}