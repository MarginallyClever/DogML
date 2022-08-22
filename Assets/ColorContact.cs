using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorContact : MonoBehaviour {
    public bool inContact;
    private MeshRenderer meshRenderer;

    // Start is called before the first frame update
    void Start() {
        inContact = false;
        // finds the first MeshRenderer in any children, which shouldbe the very next Model down.
        meshRenderer = gameObject.GetComponentInChildren<MeshRenderer>();
    }

    private void OnCollisionEnter(Collision collision) {
        //Debug.Log("OnCollisionEnter");
        foreach(ContactPoint contact in collision.contacts) {
            Debug.DrawRay(contact.point, contact.normal, Color.white);
            if(contact.otherCollider.gameObject.name.Equals("Terrain")) {
                if(meshRenderer!=null) meshRenderer.material.color = new Color(1, 0, 0);
                inContact = true;
            }
        }
        if (collision.gameObject.name.Equals("Terrain")) {
            if (meshRenderer != null) meshRenderer.material.color = new Color(0, 1, 0);
            inContact = true;
        }
    }

    private void OnCollisionExit(Collision collision) {
        //Debug.Log("OnCollisionExit");
        if (meshRenderer != null) meshRenderer.material.color = new Color(1, 1, 1);
        inContact = false;
    }
}