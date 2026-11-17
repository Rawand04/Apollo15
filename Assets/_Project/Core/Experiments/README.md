# Scientific Experiment Deployment

Open **Assets/Scenes/Apollo15.unity** and press Play. Starts on foot near the Central Station zone.

## Controls
- WASD / arrows / gamepad left stick: walk; Shift / stick press: sprint.
- Mouse / right stick: orbit the existing camera.
- E / gamepad north: deploy inside a cyan zone, board a nearby stopped rover, or safely exit when riding. One context prompt selects one action.
- Enter / gamepad south: finish typing, then advance deployment dialogue.
- 1: LM overview; 2: nearby boarding / resume rover; 3: on foot / safe exit. Remote rover driving while on foot is rejected.
- Deployment and dialogue temporarily lock actor switching. Existing rover controls remain unchanged.
- Stop the rover before exiting. Both side exits are checked for ground, slope, headroom and capsule clearance. Blocked exits keep the rider aboard and park the rover.
- The astronaut mesh is hidden while riding and restored on exit because no seated clip is imported. This is not a new sitting animation.

## Add another experiment without changing code
1. Create an **Experiment Definition** using Create > SpaceSeries > Experiments.
2. Fill Experiment Name, Prefab, Dialogue Sequence, Quest ID and Objective ID. Quest and objective IDs are distinct.
3. Add the corresponding ObjectiveDataSO to the active QuestDataSO. Use an open checklist for arbitrary deployment order, or sequential objectives for a required order.
4. Instantiate `ExperimentDeploymentZone.prefab`. Assign the definition and a ground-aligned SpawnAnchor. The trigger defaults to 4 x 3 x 4 metres; adjust it to your encounter. A lower Priority loses to a higher Priority in overlapping zones; equal priority uses nearest distance, then serialized zone order.
5. Add that zone to the player's ExperimentDeployer **Zones** array. This explicit registration avoids global scene searches.
6. Author a unit-scale prefab root with foot-aligned visuals and sensible colliders; original equipment wrappers demonstrate this. The anchor's rotation determines spawn orientation. Configure the clearance size/mask to cover the actual equipment footprint.
7. Keep the dialogue channel shared between deployer and DialogueRunner, and the quest trigger channel shared between deployer and MissionManager. Wire the runner's DialogueUI and the mission's starting quest.

## Sequencing and state
E reserves one eligible zone, pauses movement, plays the actual placement clip, then its animation completion event spawns the configured prefab. Only after spawning does it request dialogue and complete the exact mapped objective. Real objective state is checked after the quest event. Spam, duplicate completion and re-entry cannot spawn twice.

Definitions contain content only. Available/Deploying/Deployed and spawned instance references belong to scene components, never ScriptableObject assets. Each zone is one-shot per scene run; persistence/inventory/networking are not implemented.

Interruption, leaving the trigger, timeout, disable or focus loss cancels uncommitted placement and releases its reservation. Committed equipment remains if a downstream service fails, and LastFailure reports the incomplete side effect rather than duplicating equipment or auto-replaying rewards.

## Replace the placeholder animation
The shared `EquipmentPlacementRig` visibly lowers a cyan placement handle; it is a prototype mechanism, not an astronaut hand animation. Its non-looping Place clip ends with `DeploymentAnimationRelay.PlacementFinished(AnimationEvent)`. The adapter validates the active clip/state and run identity. Do not remove this event without supplying another IDeploymentAnimationPlayer implementation. The animation timeout only fails the operation; it never substitutes for completion.

An actor-specific animation adapter can implement IDeploymentAnimationPlayer and be assigned to ExperimentDeployer's Animation Player field. Keep start/cancel/completion semantics and per-run identity. Return false if no valid animation can play.

## Existing astronaut
The source stylized astronaut uses its authored Generic Avatar, not a Humanoid rig. `ExperimentAstronaut.prefab` is a project-owned copy preserving its skeleton/materials and reusing embedded Idle/Walk/Run. Vendor legacy movement and extra camera are excluded only from the copy. Lunar movement reuses the project's AstronautController/motor/ground detector with callback-based input and a compatible animator adapter.

## Example content
- Seismometer / `Deploy_Seismometer`
- ALSEP Central Station / `Deploy_Station`
- Parent Quest ID: `deploy_seismometer` (not the objective ID)
- Seismometer prefab: `Assets/_Project/Apollo15/Prefabs/Equipment/Seismometer.prefab`
- Seismometer dialogue: `Assets/_Project/Apollo15/Data/Dialogues/seismometer.asset`

A definition's Spawn Local Offset is transformed through the zone anchor. This aligns an imported prefab's visible base without changing its source scale/pivot. The Seismometer uses a measured approximately 0.31 m upward offset; normalized equipment wrappers use zero by default.

In Apollo15, AstronautInteractionRouter owns E and the prompt channel. ExperimentDeployer External Interaction is enabled and its own prompt channel is unassigned. Standalone deployers keep their existing input behavior by default. Assign scene-specific rover seat/selector references to the router; do not enable the unused vendor controller or a second input listener.

The project-owned Animator Controller uses imported Idle/Walk/Run states, Speed (float) transitions with walk/run hysteresis, and IsRiding (bool) for its hidden riding state. The source Generic Avatar and vendor assets are unchanged.

Dialogue is explicitly labelled prototype CAPCOM text and has no invented archival voice recording. Previously placed equipment is preserved in the inactive ExperimentAuthoringReferences group, not deleted.
