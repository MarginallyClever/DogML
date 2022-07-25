using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockedOnDog : MonoBehaviour
{
    public DogController Dog;
    public float behind = -10;
    public float above = 4;
    public float tilt = 0;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        LockCameraOnDog();

        if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return)) {
            DogController[] allDogs = FindObjectsOfType<DogController>();
            foreach (DogController dog in allDogs) {
                dog.EndEpisode();
            }
        }
    }

    private void LockCameraOnDog() {
        GameObject DogTorso = Dog.transform.Find("Torso(Clone)").gameObject;
        if (DogTorso == null) return;
        transform.rotation = DogTorso.transform.rotation;
        transform.position = DogTorso.transform.position;
        transform.position += transform.forward * behind;
        transform.position += transform.up * above;
        transform.Rotate(new Vector3(1,0,0), tilt);
    }
}
