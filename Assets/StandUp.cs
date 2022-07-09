using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StandUp : MonoBehaviour {
    private DogLeg[] legs = new DogLeg[4];

    public List<float> jointTargets;
    public List<int> jointIndexes;
    private List<DogJointLimit> limits = new List<DogJointLimit>();
    public List<float> positions;

    ArticulationBody Torso;

    // Start is called before the first frame update
    void Start() {
        for (int i = 0; i < legs.Length; ++i) {
            legs[i] = new DogLeg();
        }

        Torso = gameObject.transform.Find("Torso").GetComponent<ArticulationBody>();
        if (Torso == null) Debug.LogError("no torso model found.");

        legs[0].Shoulder = Torso.transform.Find("ShoulderRF").GetComponent<ArticulationBody>();
        legs[1].Shoulder = Torso.transform.Find("ShoulderRB").GetComponent<ArticulationBody>();
        legs[2].Shoulder = Torso.transform.Find("ShoulderLF").GetComponent<ArticulationBody>();
        legs[3].Shoulder = Torso.transform.Find("ShoulderLB").GetComponent<ArticulationBody>();

        for (int i = 0; i < legs.Length; ++i) {
            legs[i].Thigh = legs[i].Shoulder.transform.Find("Thigh").GetComponent<ArticulationBody>();
            legs[i].Calf = legs[i].Thigh.transform.Find("Calf").GetComponent<ArticulationBody>();
            legs[i].Foot = legs[i].Calf.transform.Find("Foot/Foot Model").GetComponent<FootContact>();

        }

        Torso.GetDriveTargets(jointTargets);
        //Debug.Log("jointTargets = " + string.Join(", ", jointTargets));
        Torso.GetDofStartIndices(jointIndexes);
        //Debug.Log("jointIndexes = " + string.Join(", ", jointIndexes));

        FindLimits();
        //SetStartingPose();
    }

    void SetStartingPose() {
        for (int i = 0; i < legs.Length; ++i) {
            //legs[i].Thigh.transform.Rotate(new Vector3(46, 0, 0));
            //legs[i].Calf.transform.Rotate(new Vector3(-117, 0, 0));

            jointTargets[jointIndexes[legs[i].Thigh.index]] = 46;
            jointTargets[jointIndexes[legs[i].Calf .index]] = -117;
        }

        Torso.SetDriveTargets(jointTargets);

        gameObject.transform.Translate(0,-2.08f,0);
    }

    private void FindLimits() {
        for (int i = 0; i < legs.Length; ++i) {
            //Debug.Log("Leg " + i + ":");
            FindLimits(legs[i].Shoulder);
            FindLimits(legs[i].Thigh);
            FindLimits(legs[i].Calf);
        }
    }

    private void FindLimits(ArticulationBody body) {
        DogJointLimit lim = new DogJointLimit();
        //Debug.Log("  dofCount="+body.dofCount);

        //bool rotateX = body.twistLock != ArticulationDofLock.LockedMotion;
        //bool rotateY = body.swingYLock != ArticulationDofLock.LockedMotion;
        //bool rotateZ = body.swingZLock != ArticulationDofLock.LockedMotion;

        lim.lower = body.xDrive.lowerLimit;
        lim.upper = body.xDrive.upperLimit;
        lim.bodyIndex = body.index;
        limits.Add(lim);
        //Debug.Log("  "+ lim.bodyIndex+" ("+jointIndexes[lim.bodyIndex]+") ="+lim.lower+"..."+lim.upper);
    }

    // Update is called once per frame
    void Update() {
        //jointTargets[0] = 180;
        //Torso.SetDriveTargets(jointTargets);

        Torso.GetJointPositions(positions);
        for(int i=0;i<positions.Count;++i) {
            positions[i] *= Mathf.Rad2Deg;
        }
    }
}
