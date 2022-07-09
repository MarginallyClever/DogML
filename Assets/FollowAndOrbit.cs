using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowAndOrbit : MonoBehaviour {
    public Transform toFollow = default;
    public float pan = 0;
    private float panStart;

    [Range(-90f, 90f)] 
    public float tilt = 0;
    private float tiltStart;

    [Range(1f, 20f)] 
    public float distance = 10;

    private bool mouseOn;

    public Material SelectedMaterial;
    public Material FurMaterial;

    // Start is called before the first frame update
    void Start() {
        mouseOn = false;
        pan = transform.eulerAngles.y;
        tilt = transform.eulerAngles.x;
    }

    // Update is called once per frame
    void Update() {
        ClickOnDog();
        MoveCamera();
    }

    List<DogController> approvedDogs = new List<DogController>();
    /**
     * Let the user click on dogs to upvote them.
     * Updogs survive to the next round; downdogs are replaced.
     */
    private void ClickOnDog() {
        if (Input.GetMouseButtonDown(0)) {
            Vector3 cursor = Input.mousePosition;
            Ray ray = Camera.main.ScreenPointToRay(cursor);
            if (Physics.Raycast(ray, out RaycastHit hitInfo)) {
                DogController dc = hitInfo.collider.gameObject.GetComponentInParent<DogController>();
                if (dc != null ) {
                    if (!approvedDogs.Contains(dc)) {
                        approvedDogs.Add(dc);
                        dc.SetSelectedMaterial(SelectedMaterial);
                        Debug.Log("Updog " + approvedDogs + ": " + dc.gameObject.name + "!");
                    } else {
                        approvedDogs.Remove(dc);
                        dc.SetReward(0);
                        dc.SetSelectedMaterial(FurMaterial);
                    }
                }
            }
        }

        ApprovedDogsGet(1);
        UnapprovedDogsGet(-1);

        if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return)) {
            EndAllDogs();
        }
    }

    private void ApprovedDogsGet(float newScore) {
        foreach (DogController dog in approvedDogs) {
            dog.SetReward(newScore);
        }
    }

    private void UnapprovedDogsGet(float newScore) {
        DogController[] allDogs = FindObjectsOfType<DogController>();
        foreach (DogController dog in allDogs) {
            if (!approvedDogs.Contains(dog)) {
                dog.SetReward(newScore);
            }
        }
    }

    private void EndAllDogs() {
        DogController[] allDogs = FindObjectsOfType<DogController>();
        foreach (DogController dog in allDogs) {
            dog.EndEpisode();
        }
        approvedDogs.Clear();
    }

    private void MoveCamera() {
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
        // scroll wheel to change distance from orbit point.
        if (Input.mouseScrollDelta.y != 0) {
            distance += Input.mouseScrollDelta.y > 0 ? 1 : -1;
            distance = Mathf.Max(2, distance);
        }

        if (toFollow != null) {
            transform.position = toFollow.transform.position + transform.forward * -distance;
        }
    }
}
