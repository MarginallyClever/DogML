using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class DogController : Agent {
    public GameObject dog;

    private GameObject dogCopy;
    private Rigidbody Torso;
    private CharacterJoint RightFrontShoulder;
    private CharacterJoint RightFrontThigh;
    private CharacterJoint RightFrontCalf;
    private FootContact RightFrontFoot;
    private CharacterJoint RightBackShoulder;
    private CharacterJoint RightBackThigh;
    private CharacterJoint RightBackCalf;
    private FootContact RightBackFoot;
    private CharacterJoint LeftFrontShoulder;
    private CharacterJoint LeftFrontThigh;
    private CharacterJoint LeftFrontCalf;
    private FootContact LeftFrontFoot;
    private CharacterJoint LeftBackShoulder;
    private CharacterJoint LeftBackThigh;
    private CharacterJoint LeftBackCalf;
    private FootContact LeftBackFoot;
    private CharacterJoint Neck;
    private CharacterJoint Head;

    private float startTime;

    private void Start() {}

    public override void OnEpisodeBegin() {
        if (dogCopy != null) Destroy(dogCopy);
        dogCopy = GameObject.Instantiate(dog);

        Torso = GameObject.Find("Torso").GetComponent<Rigidbody>();
        
        RightFrontShoulder = Torso.transform.Find("ShoulderRF").GetComponent<CharacterJoint>();
        RightFrontThigh = RightFrontShoulder.transform.Find("Thigh Model").GetComponent<CharacterJoint>();
        RightFrontCalf = RightFrontThigh.transform.Find("Calf Model").GetComponent<CharacterJoint>();
        RightFrontFoot = RightFrontCalf.transform.Find("Foot Model").GetComponent<FootContact>();

        RightBackShoulder = Torso.transform.Find("ShoulderRB").GetComponent<CharacterJoint>();
        RightBackThigh = RightBackShoulder.transform.Find("Thigh Model").GetComponent<CharacterJoint>();
        RightBackCalf = RightBackThigh.transform.Find("Calf Model").GetComponent<CharacterJoint>();
        RightBackFoot = RightBackCalf.transform.Find("Foot Model").GetComponent<FootContact>();

        LeftFrontShoulder = Torso.transform.Find("ShoulderLF").GetComponent<CharacterJoint>();
        LeftFrontThigh = LeftFrontShoulder.transform.Find("Thigh Model").GetComponent<CharacterJoint>();
        LeftFrontCalf = LeftFrontThigh.transform.Find("Calf Model").GetComponent<CharacterJoint>();
        LeftFrontFoot = LeftFrontCalf.transform.Find("Foot Model").GetComponent<FootContact>();

        LeftBackShoulder = Torso.transform.Find("ShoulderLB").GetComponent<CharacterJoint>();
        LeftBackThigh = LeftBackShoulder.transform.Find("Thigh Model").GetComponent<CharacterJoint>();
        LeftBackCalf = LeftBackThigh.transform.Find("Calf Model").GetComponent<CharacterJoint>();
        LeftBackFoot = LeftBackCalf.transform.Find("Foot Model").GetComponent<FootContact>();

        Neck = Torso.transform.Find("Neck").GetComponent<CharacterJoint>();
        Head = Neck.transform.Find("Head").GetComponent<CharacterJoint>();
        startTime = Time.time;

        base.OnEpisodeBegin();
    }

    public override void CollectObservations(VectorSensor sensor) {
        // range 0...1
        float facingUp = (1 + Vector3.Dot(Torso.transform.up, new Vector3(0, 1, 0))) / 2.0f;

        // range 0... almost 1.  dog floating off floor at 3.66
        float height = Torso.position.y;
        if (height > 3.5) EndEpisode();
        if (Time.time - startTime > 5) EndEpisode();

        float rewardAmount = facingUp * height;
        SetReward(rewardAmount);

        sensor.AddObservation(RightFrontShoulder.transform.eulerAngles);
        sensor.AddObservation(RightFrontThigh.transform.eulerAngles);
        sensor.AddObservation(RightFrontCalf.transform.eulerAngles);
        sensor.AddObservation(RightFrontFoot.inContact ? 1 : 0);

        sensor.AddObservation(LeftFrontShoulder.transform.eulerAngles);
        sensor.AddObservation(LeftFrontThigh.transform.eulerAngles);
        sensor.AddObservation(LeftFrontCalf.transform.eulerAngles);
        sensor.AddObservation(LeftFrontFoot.inContact ? 1 : 0);

        sensor.AddObservation(RightBackShoulder.transform.eulerAngles);
        sensor.AddObservation(RightBackThigh.transform.eulerAngles);
        sensor.AddObservation(RightBackCalf.transform.eulerAngles);
        sensor.AddObservation(RightBackFoot.inContact ? 1 : 0);

        sensor.AddObservation(LeftBackShoulder.transform.eulerAngles);
        sensor.AddObservation(LeftBackThigh.transform.eulerAngles);
        sensor.AddObservation(LeftBackCalf.transform.eulerAngles);
        sensor.AddObservation(LeftBackFoot.inContact? 1 : 0);

        sensor.AddObservation(rewardAmount);
        sensor.AddObservation(facingUp);
        sensor.AddObservation(height);

        base.CollectObservations(sensor);
    }
        
    public override void OnActionReceived(ActionBuffers actions) {

        int index = 0;
        AddRelativeForce(RightFrontShoulder, new Vector3(actions.ContinuousActions[index++]*2-1, 0, 0), ForceMode.Force);
        AddRelativeForce(RightFrontThigh   , new Vector3(actions.ContinuousActions[index++]*2-1, 0, 0), ForceMode.Force);
        AddRelativeForce(RightFrontCalf    , new Vector3(actions.ContinuousActions[index++]*2-1, 0, 0), ForceMode.Force);
        AddRelativeForce(LeftFrontShoulder , new Vector3(actions.ContinuousActions[index++]*2-1, 0, 0), ForceMode.Force);
        AddRelativeForce(LeftFrontThigh    , new Vector3(actions.ContinuousActions[index++]*2-1, 0, 0), ForceMode.Force);
        AddRelativeForce(LeftFrontCalf     , new Vector3(actions.ContinuousActions[index++]*2-1, 0, 0), ForceMode.Force);
        AddRelativeForce(RightBackShoulder , new Vector3(actions.ContinuousActions[index++]*2-1, 0, 0), ForceMode.Force);
        AddRelativeForce(RightBackThigh    , new Vector3(actions.ContinuousActions[index++]*2-1, 0, 0), ForceMode.Force);
        AddRelativeForce(RightBackCalf     , new Vector3(actions.ContinuousActions[index++]*2-1, 0, 0), ForceMode.Force);
        AddRelativeForce(LeftBackShoulder  , new Vector3(actions.ContinuousActions[index++]*2-1, 0, 0), ForceMode.Force);
        AddRelativeForce(LeftBackThigh     , new Vector3(actions.ContinuousActions[index++]*2-1, 0, 0), ForceMode.Force);
        AddRelativeForce(LeftBackCalf      , new Vector3(actions.ContinuousActions[index++]*2-1, 0, 0), ForceMode.Force);

        base.OnActionReceived(actions);
    }

    // Update is called once per frame
    void FixedUpdate() {
        // (GameObject.Find("Torso Model")).GetComponent<Rigidbody>().AddForce(new Vector3(0,5,0), ForceMode.Impulse);
    }

    private void AddRelativeForce(CharacterJoint joint,Vector3 force,ForceMode mode = default) {
        Rigidbody rb = joint.GetComponent<Rigidbody>();
        rb.AddRelativeTorque(force, mode);
    }

    private void AddForce(CharacterJoint joint, Vector3 force, ForceMode mode = default) {
        Rigidbody rb = joint.GetComponent<Rigidbody>();
        rb.AddForce(force, mode);
    }
}
