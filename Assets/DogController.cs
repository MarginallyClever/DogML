using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System;
using Random = UnityEngine.Random;

/**
 * Uses Unity ml-agents: https://github.com/Unity-Technologies/ml-agents
 * 1. Activate python virtual environment.  c:\Users\aggra\documents\github\dogml> venv\Scripts\activate
 * 2. Run learn script: (venv) c:\Users\aggra\documents\github\dogml> mlagents-learn
 * @author Dan Royer
 * @since 2022-06-15
 */
public class DogController : Agent {
    private GameObject torsoCopy;
    private GameObject walkTarget;

    private ArticulationBody Torso;
    private ArticulationBody Neck;
    private ArticulationBody Head;

    private DogLeg[] legs = new DogLeg[4];
    private ColorContact TorsoContact;

    private List<float> jointTargets = new List<float>();
    private List<int> jointIndexes = new List<int>();
    private List<DogJointLimit> limits = new List<DogJointLimit>();
    private List<float> lastCommands = new List<float>();

    public float facingUpReward = 100f;
    public float standingUpReward = 10f;
    public float horizontalMovementReward = 0.1f;
    public float jointSpeed = 0.1f;

    public float costOfEnergy = 0f;

    // actual standing height is ~3.55.  lowering this a little
    public float idealStandingHeight = 3.25f;

    public float upLowPass = 0;
    public float heightLowPass = 0.5f;
    public float targetEpsilon = 0.2f;

    public float startDistance = 0f;

    private void Start() {
        for (int i = 0; i < legs.Length; ++i) {
            legs[i] = new DogLeg();
        }
        transform.Find("Torso").gameObject.SetActive(false);

        walkTarget = transform.Find("WalkTarget").gameObject;
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

        walkTarget.transform.SetPositionAndRotation(transform.position, transform.parent.transform.rotation);

        Torso = torsoCopy.GetComponent<ArticulationBody>();
        if (Torso == null) Debug.LogError("no torso model found.");

        TorsoContact = Torso.GetComponent<ColorContact>();

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
            legs[i].ThighContact = legs[i].Thigh.GetComponent<ColorContact>();

            legs[i].Calf = legs[i].Thigh.transform.Find("Calf").GetComponent<ArticulationBody>();
            legs[i].FootContact = legs[i].Calf.GetComponent<ColorContact>();

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

        Torso.GetDriveTargets(jointTargets);
        //Debug.Log(String.Join(",", jointTargets));

        MoveWalkTarget();

        var diff = walkTarget.transform.position - Torso.transform.position;
        startDistance = diff.magnitude;
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
        lim.range = Mathf.Abs(lim.upper - lim.lower);
        lim.bodyIndex = body.index;
        lim.obj = body.gameObject;
        lim.body = body;
        limits.Add(lim);
        //Debug.Log(lim.bodyIndex);
    }

    private float GetFacingUp() {
        return Torso.transform.up.y;
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

        for (int i = 0; i < limits.Count; ++i) {
            var index = limits[i].bodyIndex;
            // normalize positions
            var p = positions[index] * Mathf.Rad2Deg;
            var p2 = (((p - limits[i].lower) / limits[i].range) * 2f ) - 1f;
            positions[index] = p2;
        }

        sensor.AddObservation(positions);

        //if(lastCommands.Count< velocities.Count) {
        //    for (int i = lastCommands.Count; i < velocities.Count; ++i) {
        //        lastCommands.Add(0);
        //    }
        //}
        //sensor.AddObservation(lastCommands);

        sensor.AddObservation(TorsoContact.inContact);
        for (int i = 0; i < legs.Length; ++i) {
            sensor.AddObservation(legs[i].ThighContact.inContact);
            sensor.AddObservation(legs[i].FootContact.inContact);
        }

        sensor.AddObservation(Mathf.Min(GetHeight(), idealStandingHeight) / idealStandingHeight);

        Vector3 whichWayIsUp = Torso.transform.InverseTransformDirection(new Vector3(0,1,0));
        sensor.AddObservation(whichWayIsUp);

        Vector3 localVelocity = Torso.transform.InverseTransformDirection(Torso.velocity);
        sensor.AddObservation(localVelocity.normalized);
        sensor.AddObservation(Mathf.Clamp(localVelocity.magnitude,0f,10f)/10.0f);

        Vector3 localAngularVelocity = Torso.transform.InverseTransformDirection(Torso.angularVelocity).normalized * Torso.angularVelocity.magnitude;
        sensor.AddObservation(localAngularVelocity);

        // direction to target
        var diff = walkTarget.transform.position - Torso.transform.position;
        var localDiff = Torso.transform.InverseTransformDirection(diff);
        var dn = diff.normalized;
        //sensor.AddObservation(diff);
        sensor.AddObservation(dn);

        //var vn = Torso.velocity.normalized;
        //float movingTowardsTarget = Vector3.Dot(vn, dn);
        //sensor.AddObservation(movingTowardsTarget);

        float m = diff.magnitude;
        sensor.AddObservation(m);
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
        float height = (Mathf.Min(GetHeight(), idealStandingHeight) / idealStandingHeight) * standingUpReward;
        SetReward(up * height);
        
        if (costOfEnergy>0) MakeEnergyUseExpensive();

        {
            // is it moving?
            //Vector3 v = transform.InverseTransformDirection(Torso.velocity);
            //v.y = 0;
            //float horizontalSpeed = v.magnitude;

            // punish fast vertical movements to prevent jumping?
            //float verticalSpeed = 1.0f - Mathf.Abs(Torso.velocity.y);
            //float speedScore = horizontalSpeed * horizontalMovementReward;

            // Debug.Log(up+"\t"+height+"\t"+horizontalSpeed);
            //AddReward(speedScore);
        }

        if(up> upLowPass && height> heightLowPass) 
        {
            var diff = walkTarget.transform.position - Torso.transform.position;
            var dn = diff.normalized;
            var vn = Torso.velocity.normalized;
            var fn = Torso.transform.forward;

            float facingTarget = Vector3.Dot(fn, dn);
            AddReward(facingTarget);

            float movingTowardsTarget = Vector3.Dot(vn, dn);
            AddReward(movingTowardsTarget);

            float m = diff.magnitude;
            if (startDistance > targetEpsilon) {
                AddReward(Mathf.Max(0,startDistance - m));  // hotter the closer you get!
            } else {
                MoveWalkTarget();
                return;
            }

            if (diff.magnitude < targetEpsilon) {
                // reached target!
                AddReward(100);
                EndEpisode();
            }
        }
    }

    // at 40 units apart they should never bump into each other.
    private void MoveWalkTarget() {
        walkTarget.transform.position = new Vector3(
        Torso.transform.position.x + Random.Range(-15f, 15f),
        walkTarget.transform.position.y,
        Torso.transform.position.z + Random.Range(-15f, 15f)
        );
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
        lastCommands.Clear();

        for (int i = 0; i < limits.Count; ++i) {
            DogJointLimit lim = limits[i];
            int ji = jointIndexes[lim.bodyIndex];

            // "By default the output from our provided PPO algorithm pre-clamps the values of
            // ActionBuffers.ContinuousActions into the [-1, 1] range. It is a best practice
            // to manually clip these as well, if you plan to use a 3rd party algorithm with
            // your environment. As shown above, you can scale the control values as needed
            // after clamping them."
            float ca = Mathf.Clamp(actions.ContinuousActions[i], -1, 1);  //already clamped to [-1,1] but best practice.

            // relative moves don't train as good as absolute moves.
            // relative moves
            //float before = Mathf.Rad2Deg * jointTargets[ji];
            //float after = before + ca * jointSpeed * Time.fixedDeltaTime;
            // absolute moves
            float after = Mathf.Lerp(lim.lower,lim.upper,(1f + ca)*0.5f);

            //jointTargets[ji] = Mathf.Deg2Rad * Mathf.Clamp(after, lim.lower, lim.upper);
            jointTargets[ji] = Mathf.Deg2Rad * after;
            lastCommands.Add(ca);
        }

        //Debug.Log(String.Join(",",jointTargets));
        Torso.SetDriveTargets(jointTargets);
    }


    public override void Heuristic(in ActionBuffers actionsOut) {
        var continuousActionsOut = actionsOut.ContinuousActions;

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
                continuousActionsOut[i] += angle * 0.01f;
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
