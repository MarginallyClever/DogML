# UpDog

Unity + machine learning + quadruped

![v1 with no neck](2022-06-20-01.PNG)

Vote on which robots you think do the best job of rolling over and standing up.
When you've picked all you like hit enter or return to start the next round.
Dogs with no vote get replaced.  You can unselect a dog if you didn't mean to select it.

## Why

I've made walking robots before that used predefined patterns for leg movements.
I've always simulated my robots before building them to test my ideas without spending cash.
This is an experiment to see if ML can do a better job than I.

I'm using Unity 2021.3f.  I followed the Youtube guide at https://www.youtube.com/watch?v=zPFU30tbyKs
Please familiarize yourself with it before proceeding.
to setup https://github.com/Unity-Technologies/ml-agents.  Once you have the python setup installed
you can open the sample scene in Unity.

I used a thin/tall body instead of a wide/flat body because I believe it will be better at self-righting when it has a lower center of gravity.

## To run

- Force Restart learning script: click on ~/venv/learn.bat
- Resume existing learning script: click on ~/venv/learnResume.bat
- Run the results graph: click on ~/venv/graph.bat

or 

- Enter the python virtual environment: `~\dogml> venv\Scripts\activate`
- Run the learning script: `(venv) ~\dogml> mlagents-learn --run-id=1 --force`
- Hit "play" in Unity.
- results will be in a new folder called `./results/1`
- To visualize the training data, `(venv) ~\dogml>tensorboard --logdir results` and then open a browser to http://localhost:6006
- mlagents-learn, tensorboard, and website need to be reloaded after every stop of mlagents-learn.

## ML options

- `--resume` continues the default training
- `--force` restarts the default training 
- `--run-id=name` each 'name' is a separate training, so you can try different things in each
- `--initialize-from=name` start this training with values from 'name'.

## The environment

The environment is a "prefab" Dog with a DogController (a stub that controls the Machine Learning).

Dog is made of ArticulatedBody in a heirarchy.  Dog has Torso has hips has thighs has calves has feet.
Each part of Dog also contains a box to represent the mesh and a collision boundary.  ArticulatedBody joints have been limited to one axis each and given a reasonable range of motion.

#### OnEpisodeBegin
in *OnEpisodeBegin* it instantiates (clones) the original dog and rotates it a bit so the system doesn't over-specialize.  Cloning was much easier than resetting values.

#### CollectObservations
in *CollectObservations* it measures 

- every joint position (the one angle that can change in each joint), 
- is the foot in touch with the floor,
- how much is the torso facing upward
- how high is the torso off the ground

#### OnActionReceived
in *OnActionReceived* it applies the ContinuousActions received from the network to the joints.
Because they are ArticulatedBody type I can use `SetDriveTargets` to set a target angle for each joint.

![with neck, head, and larger back legs](2022-06-20-02.PNG)

## Challenges

#### OnActionReceived driving joints
I have tried a few strategies in `OnActionReceived`:

1. newTarget = action * (upperAngle - lowerAngle) + lowerAngle;
2. newTarget = oldTarget+(action\*10)-5;
3. newTarget = action;

all of which are then bounded by `newTarget = Mathf.Clamp( newTarget, lowerAngle, upperAngle);`

#### SetDriveTargets API is not well described.

> The exact location of the data in resulting list for the specific articulation body can be found by calling `ArticulationBody.GetDofStartIndices` and indexing returned `dofStartIndices` list by the particular body index via `ArticulationBody.index`.

-- https://docs.unity3d.com/ScriptReference/ArticulationBody.SetDriveTargets.html

So you have to `GetDofStartIndices(startIndexes)`, then `targets[startIndexes[body.index]+dof]`, then you can `SetDriveTargets(targets)`.

#### GetJointPositions returns radians, not degrees.

Because consistency is for amateurs?

#### GetJointPositions returns zeros on the first frame, regardless of pose

Build a prefab; place it in the world; pose it; all joints will still report zero.  It initializes to whatever was available at the first frame.

## Further reading

- [Hierarchical Reinforcement Learning for Quadruped Locomotion](https://arxiv.org/abs/1905.08926)
- [Learning fast and agile quadrupedal locomotion over complex terrain](https://arxiv.org/abs/2207.00797)
- [Safe Reinforcement Learning for Legged Locomotion](https://arxiv.org/abs/2203.02638)