using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System;

/**
 * Uses Unity ml-agents: https://github.com/Unity-Technologies/ml-agents
 * 1. Activate python virtual environment.  c:\Users\aggra\documents\github\dogml> venv\Scripts\activate
 * 2. Run learn script: (venv) c:\Users\aggra\documents\github\dogml> mlagents-learn
 * @author Dan Royer
 * @since 2022-06-15
 */
public class DogController : Agent {
    private GameObject torsoCopy;

    private ArticulationBody Torso;
    private ArticulationBody Neck;
    private ArticulationBody Head;

    private DogLeg[] legs = new DogLeg[4];

    private float startTime;

    private List<float> jointTargets = new List<float>();
    private List<int> jointIndexes = new List<int>();
    private List<DogJointLimit> limits = new List<DogJointLimit>();
    private List<float> lastCommands = new List<float>();

    public float facingUpReward = 100f;
    public float standingUpReward = 10f;
    public float horizontalMovementReward = 0.1f;
    public float jointSpeed = 0.1f;

    public float costOfEnergy = 0f;

    private void Start() {
        for (int i = 0; i < legs.Length; ++i) {
            legs[i] = new DogLeg();
        }
        transform.Find("Torso").gameObject.SetActive(false);
    }

    public override void Initialize() {
        if (!Academy.Instance.IsCommunicatorOn) {
            this.MaxStep = 0;
        }
    }

    public override void OnEpisodeBegin() {
        //Debug.Log("Episode " + this.CompletedEpisodes);
        if (torsoCopy != null) Destroy(torsoCopy);
        torsoCopy = Instantiate(
            transform.Find("Torso").gameObject,
            transform.position,
            transform.parent.transform.rotation,
            transform);
        
        torsoCopy.SetActive(true);

        Torso = torsoCopy.GetComponent<ArticulationBody>();
        if (Torso == null) Debug.LogError("no torso model found.");


        Torso.GetDriveTargets(jointTargets);
        //Debug.Log("list = " + string.Join(", ", jointTargets));
        Torso.GetDofStartIndices(jointIndexes);
        //Debug.Log("indexes = " + string.Join(", ", jointIndexes));

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

    internal void SetSelectedMaterial(Material selectedMaterial) {
        Torso.transform.Find("Torso Model").GetComponent<MeshRenderer>().material = selectedMaterial;
    }

    private void FindLimits() {
        limits.Clear();

        for (int i = 0; i < legs.Length; ++i) {
            FindLimits(legs[i].Shoulder);
            FindLimits(legs[i].Thigh);
            FindLimits(legs[i].Calf);
        }
    }

    private void FindLimits(ArticulationBody body) {
        DogJointLimit lim = new DogJointLimit();
        lim.lower = body.xDrive.lowerLimit;
        lim.upper = body.xDrive.upperLimit;
        lim.bodyIndex = body.index;
        lim.obj = body.gameObject;
        limits.Add(lim);
    }

    private float GetFacingUp() {
        return (1f+Torso.transform.up.y)/2f;
    }

    private float GetHeight() {
        return Torso.transform.position.y;
    }

    /**
     * Each 'observation' should be a value [-1,1]
     */
    public override void CollectObservations(VectorSensor sensor) {
        for(int i=0;i<limits.Count;++i) {
            Vector3 p2 = Torso.transform.InverseTransformPoint(limits[i].obj.transform.position);
            sensor.AddObservation(p2);
        }

        List<float> positions = new List<float>();
        Torso.GetJointPositions(positions);
        sensor.AddObservation(positions);

        List<float> velocities = new List<float>();
        Torso.GetJointVelocities(velocities);
        sensor.AddObservation(velocities);

        List<float> accelerations = new List<float>();
        Torso.GetJointAccelerations(accelerations);
        sensor.AddObservation(accelerations);

        //if(lastCommands.Count< velocities.Count) {
        //    for (int i = lastCommands.Count; i < velocities.Count; ++i) {
        //        lastCommands.Add(0);
        //    }
        //}
        //sensor.AddObservation(lastCommands);

        for (int i = 0; i < legs.Length; ++i) {
            sensor.AddObservation(legs[i].Foot.inContact ? 1 : 0);
        }

        sensor.AddObservation(GetHeight()/3.6f);

        Vector3 whichWayIsUp = Torso.transform.InverseTransformDirection(new Vector3(0,1,0));
        sensor.AddObservation(whichWayIsUp);


        Vector3 localVelocity = Torso.transform.InverseTransformDirection(Torso.velocity);
        sensor.AddObservation(localVelocity.normalized);
        sensor.AddObservation(Mathf.Clamp(localVelocity.magnitude,0f,10f)/10.0f);
        Vector3 localAngularVelocity = Torso.transform.InverseTransformDirection(Torso.angularVelocity).normalized * Torso.angularVelocity.magnitude;
        sensor.AddObservation(localAngularVelocity);
    }

    private void FixedUpdate() {
        // I have no idea how often reward is used to improve the network.
        // I try to keep it updated all the time.
        CalculateReward();
        // Instead of calculating reward here as some magic number, I'm now
        // using a system where I vote on who is doing best and everyone else
        // leaves the island.
    }

    /**
     * Reward should be a value [-1,1]
     */
    private void CalculateReward() {
        // is it facing up?
        float up = GetFacingUp() * facingUpReward;
        //Debug.Log(up);

        // is it standing?
        // range 0... almost 1.  dog floating off floor at 3.66
        float height = Mathf.Clamp(GetHeight() / 3.6f,0f,1f) * standingUpReward;

        if(costOfEnergy>0) MakeEnergyUseExpensive();

        // is it moving?
        //Vector3 v = //new Vector3(Torso.velocity.x, 0, Torso.velocity.z);
        //            transform.InverseTransformDirection(Torso.velocity);
        //float horizontalSpeed = (Mathf.Clamp(v.magnitude, 0f, 10f) / 10.0f);

        // punish fast vertical movements to prevent jumping?
        //float verticalSpeed = 1.0f - Mathf.Abs(Torso.velocity.y);
        float speedScore = 0;// horizontalMovementReward;// horizontalSpeed;

        // Debug.Log(up+"\t"+height+"\t"+horizontalSpeed);
        SetReward(up * height + speedScore);
    }

    // punish larg changes in acceleration.  make the creature lazy.
    private void MakeEnergyUseExpensive() {
        float energyUsed = 0;
        foreach( float acceleration in lastCommands) {
            energyUsed += Mathf.Abs(acceleration);
        }
        AddReward(-energyUsed * costOfEnergy);

    }

    public override void OnActionReceived(ActionBuffers actions) {
        Torso.GetDriveTargets(jointTargets);
        lastCommands.Clear();

        for (int i = 0; i < limits.Count; ++i) {
            DogJointLimit lim = limits[i];
            int ji = jointIndexes[lim.bodyIndex];
            float before = Mathf.Rad2Deg * jointTargets[ji];

            // adjust joint target incrementally
            // aka control joint target velocity.
            float ca = Mathf.Clamp(actions.ContinuousActions[i], -1, 1);

            // relative moves
            float after = before + ca * jointSpeed * Time.fixedDeltaTime;
            
            // absolute moves
            //float after = Mathf.Lerp(lim.lower,lim.upper,(1.0f + ca)/2f);

            jointTargets[ji] = Mathf.Deg2Rad * Mathf.Clamp(after, lim.lower, lim.upper);
            //jointTargets[ji] = Mathf.Lerp(lim.lower, lim.upper, ca);
            lastCommands.Add(ca);
        }

        //Debug.Log(String.Join(",",jointTargets));
        Torso.SetDriveTargets(jointTargets);
    }


    public override void Heuristic(in ActionBuffers actionsOut) {
        var continuousActionsOut = actionsOut.ContinuousActions;
        for (int i = 0; i < limits.Count; ++i) {
            continuousActionsOut[i] = 0;
        }

        if (Input.GetKey(KeyCode.Q)) LeftCalves(continuousActionsOut, +1);
        if (Input.GetKey(KeyCode.A)) LeftCalves(continuousActionsOut, -1);
        if (Input.GetKey(KeyCode.W)) LeftThighs(continuousActionsOut,+1);
        if (Input.GetKey(KeyCode.S)) LeftThighs(continuousActionsOut,-1);
        if (Input.GetKey(KeyCode.E)) LeftShoulders(continuousActionsOut,-1);
        if (Input.GetKey(KeyCode.D)) LeftShoulders(continuousActionsOut,+1);

        if (Input.GetKey(KeyCode.Y)) RightShoulders(continuousActionsOut, -1);
        if (Input.GetKey(KeyCode.H)) RightShoulders(continuousActionsOut, +1);
        if (Input.GetKey(KeyCode.U)) RightThighs(continuousActionsOut,+1);
        if (Input.GetKey(KeyCode.J)) RightThighs(continuousActionsOut,-1);
        if (Input.GetKey(KeyCode.I)) RightCalves(continuousActionsOut,+1);
        if (Input.GetKey(KeyCode.K)) RightCalves(continuousActionsOut,-1);
    }

    private void addTarget(ActionSegment<float> continuousActionsOut, ArticulationBody body, float angle) {
        for (int i = 0; i < limits.Count; ++i) {
            if(limits[i].bodyIndex == body.index) {
                continuousActionsOut[i] += angle;
            }
        }
    }

    private void LeftShoulders(ActionSegment<float> continuousActionsOut, float angle) {
        addTarget(continuousActionsOut, legs[2].Shoulder, angle);
        addTarget(continuousActionsOut, legs[3].Shoulder, angle);
    }

    private void LeftThighs(ActionSegment<float> continuousActionsOut,float angle) {
        addTarget(continuousActionsOut, legs[2].Thigh, angle);
        addTarget(continuousActionsOut, legs[3].Thigh, angle);
    }

    private void LeftCalves(ActionSegment<float> continuousActionsOut,float angle) {
        addTarget(continuousActionsOut, legs[2].Calf, angle);
        addTarget(continuousActionsOut, legs[3].Calf, angle);
    }

    private void RightShoulders(ActionSegment<float> continuousActionsOut, float angle) {
        addTarget(continuousActionsOut, legs[0].Shoulder, angle);
        addTarget(continuousActionsOut, legs[1].Shoulder, angle);
    }

    private void RightThighs(ActionSegment<float> continuousActionsOut,float angle) {
        addTarget(continuousActionsOut, legs[0].Thigh, angle);
        addTarget(continuousActionsOut, legs[1].Thigh, angle);
    }

    private void RightCalves(ActionSegment<float> continuousActionsOut,float angle) {
        addTarget(continuousActionsOut, legs[0].Calf, angle);
        addTarget(continuousActionsOut, legs[1].Calf, angle);
    }
}
