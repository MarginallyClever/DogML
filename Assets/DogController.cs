using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

/**
 * Uses Unity ml-agents: https://github.com/Unity-Technologies/ml-agents
 * 1. Activate python virtual environment.  c:\Users\aggra\documents\github\dogml> venv\Scripts\activate
 * 2. Run learn script: (venv) c:\Users\aggra\documents\github\dogml> mlagents-learn
 * @author Dan Royer
 * @since 2022-06-15
 */
public class DogController : Agent {
    public GameObject dog;
    private GameObject dogCopy;

    private ArticulationBody Torso;
    private ArticulationBody Neck;
    private ArticulationBody Head;

    class DogLeg {
        public ArticulationBody Shoulder;
        public ArticulationBody Thigh;
        public ArticulationBody Calf;
        public FootContact Foot;
    };

    class JointLimit {
        public int bodyIndex;
        public float lower;
        public float upper;
    };

    private DogLeg[] legs = new DogLeg[4];

    private float startTime;

    private List<float> jointTargets = new List<float>();
    private List<int> jointIndexes = new List<int>();
    private List<JointLimit> limits = new List<JointLimit>();

    private void Start() {
        for (int i = 0; i < legs.Length; ++i) {
            legs[i] = new DogLeg();
        }

        Torso = dog.transform.Find("Torso").GetComponent<ArticulationBody>();
        Torso.GetDriveTargets(jointTargets);
        //Debug.Log("list = " + string.Join(", ", jointTargets));
        Torso.GetDofStartIndices(jointIndexes);
        //Debug.Log("indexes = " + string.Join(", ", jointIndexes));
    }

    public override void OnEpisodeBegin() {
        dog.SetActive(false);

        Debug.Log("Episode " + this.CompletedEpisodes);
        if (dogCopy != null) Destroy(dogCopy);
        dogCopy = Instantiate(dog);
        dogCopy.SetActive(true);


        Torso = dogCopy.transform.Find("Torso").GetComponent<ArticulationBody>();
        if (Torso == null) Debug.LogError("no torso model found.");

        Torso.transform.Rotate(new Vector3(
            Random.Range(-180, 180),
            Random.Range(-180, 180),
            Random.Range(-180, 180)));

        legs[0].Shoulder = Torso.transform.Find("ShoulderRF").GetComponent<ArticulationBody>();
        legs[1].Shoulder = Torso.transform.Find("ShoulderRB").GetComponent<ArticulationBody>();
        legs[2].Shoulder = Torso.transform.Find("ShoulderLF").GetComponent<ArticulationBody>();
        legs[3].Shoulder = Torso.transform.Find("ShoulderLB").GetComponent<ArticulationBody>();

        for (int i = 0; i < legs.Length; ++i) {
            legs[i].Thigh = legs[i].Shoulder.transform.Find("Thigh").GetComponent<ArticulationBody>();
            legs[i].Calf = legs[i].Thigh.transform.Find("Calf").GetComponent<ArticulationBody>();
            legs[i].Foot = legs[i].Calf.transform.Find("Foot/Foot Model").GetComponent<FootContact>();
            //if (legs[i].Shoulder == null) Debug.LogError("Shoulder " + i + " not found.");
            //if (legs[i].Thigh    == null) Debug.LogError("Thigh " + i + " not found.");
            //if (legs[i].Calf     == null) Debug.LogError("Calf " + i + " not found.");
            //if (legs[i].Foot     == null) Debug.LogError("Foot " + i + " not found.");
            //Debug.Log("Shoulder " + i + " dof = " + legs[i].Shoulder.dofCount);
            //Debug.Log("Thigh " + i + " dof = " + legs[i].Thigh.dofCount);
            //Debug.Log("Calf " + i + " dof = " + legs[i].Calf.dofCount);
        }

        Neck = Torso.transform.Find("Neck").GetComponent<ArticulationBody>();
        Head = Neck.transform.Find("Head").GetComponent<ArticulationBody>();
        //Debug.Log("Neck dof = " + Neck.dofCount);
        //Debug.Log("Head dof = " + Head.dofCount);

        FindLimits();
        //Debug.Log("limits = " + string.Join(", ", limits));

        startTime = Time.time;
    }

    private void FindLimits() {
        for (int i = 0; i < legs.Length; ++i) {
            FindLimits(legs[i].Shoulder);
            FindLimits(legs[i].Thigh);
            FindLimits(legs[i].Calf);
        }
    }

    private void FindLimits(ArticulationBody body) {
        JointLimit lim = new JointLimit();
        lim.lower = body.xDrive.lowerLimit;
        lim.upper = body.xDrive.upperLimit;
        lim.bodyIndex = body.index;
        limits.Add(lim);
    }

    private float GetFacingUp() {
        return Torso.transform.up.y;
    }

    private float GetHeight() {
        return Torso.transform.position.y;
    }
    
    public override void CollectObservations(VectorSensor sensor) {
        for (int i = 0; i < legs.Length; ++i) {
            sensor.AddObservation(legs[i].Shoulder.jointPosition[0]);
            sensor.AddObservation(legs[i].Thigh.jointPosition[0]);
            sensor.AddObservation(legs[i].Calf.jointPosition[0]);
            sensor.AddObservation(legs[i].Foot.inContact ? 1 : 0);
        }

        sensor.AddObservation(GetFacingUp());
        sensor.AddObservation(GetHeight());
    }
        
    public override void OnActionReceived(ActionBuffers actions) {
        //Debug.Log("first = " + actions.ContinuousActions[0]);

        int least = Mathf.Min(actions.ContinuousActions.Length, jointTargets.Count);
        for (int i = 0; i < least; ++i) {
            JointLimit lim = limits[i];
            int index = jointIndexes[lim.bodyIndex];
            float f = jointTargets[index] + actions.ContinuousActions[i]*10f-5f;
            jointTargets[index] = Mathf.Max(Mathf.Min(f, lim.upper), lim.lower);
        }

        Torso.SetDriveTargets(jointTargets);
    }

    // Update is called once per frame
    void FixedUpdate() {
        // (GameObject.Find("Torso Model")).GetComponent<Rigidbody>().AddForce(new Vector3(0,5,0), ForceMode.Impulse);

        // range -1...1
        //AddReward(GetFacingUp());

        // range 0... almost 1.  dog floating off floor at 3.66
        //AddReward(GetHeight());

        AddReward(Torso.velocity.x);

        if(Time.time - startTime > 30) {
            EndEpisode();
        }
    }
}
