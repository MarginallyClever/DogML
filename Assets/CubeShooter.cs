using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeShooter : MonoBehaviour
{
    private GameObject thingToThrow;
    public float speed = 20;
    public float lifeTime = 3;

    // Start is called before the first frame update
    void Start() {
        thingToThrow = transform.Find("Cube").gameObject;
        thingToThrow.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space)) {
            GameObject copy = Instantiate(thingToThrow);
            var rb = copy.GetComponent<Rigidbody>();
            copy.transform.SetPositionAndRotation(transform.position, transform.rotation);
            rb.velocity = transform.forward * speed;
            copy.SetActive(true);
            Destroy(copy, lifeTime);
        }
    }
}
