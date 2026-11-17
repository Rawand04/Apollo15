using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SpaceSeries.Core.Player.Editor
{
    /// <summary>
    /// Editor automation tool to generate the production-ready Astronaut Animator Controller
    /// configured with Idle, Walk, Run, Wave, Point, and PickUp states, transitions, and parameters.
    /// </summary>
    public static class AstronautAnimatorControllerBuilder
    {
        private const string ANIMATIONS_FOLDER = "Assets/Animations";
        private const string TARGET_CONTROLLER_PATH = "Assets/Animations/AstronautAnimatorController.controller";

        [MenuItem("Tools/SpaceSeries/Build Astronaut Animator Controller")]
        public static void BuildControllerMenu()
        {
            BuildController();
        }

        public static AnimatorController BuildController()
        {
            // 1. Locate Animation Clips from FBX assets
            AnimationClip idleClip = LoadClipFromFbx(Path.Combine(ANIMATIONS_FOLDER, "idle.fbx"));
            AnimationClip walkClip = LoadClipFromFbx(Path.Combine(ANIMATIONS_FOLDER, "Walk.fbx"));
            AnimationClip runClip = LoadClipFromFbx(Path.Combine(ANIMATIONS_FOLDER, "Run.fbx"));
            AnimationClip waveClip = LoadClipFromFbx(Path.Combine(ANIMATIONS_FOLDER, "Wave.fbx"));
            AnimationClip pointClip = LoadClipFromFbx(Path.Combine(ANIMATIONS_FOLDER, "Point.fbx"));
            AnimationClip pickUpClip = LoadClipFromFbx(Path.Combine(ANIMATIONS_FOLDER, "PickUp.fbx"));

            // 2. Create or replace Animator Controller
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(TARGET_CONTROLLER_PATH);
            Debug.Log($"[AnimatorBuilder] Created AnimatorController at {TARGET_CONTROLLER_PATH}");

            // 3. Add Parameters
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsSprinting", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Wave", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Point", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("PickUp", AnimatorControllerParameterType.Trigger);

            // 4. Access Root State Machine
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            stateMachine.entryPosition = new Vector3(50, 100, 0);
            stateMachine.anyStatePosition = new Vector3(50, 0, 0);

            // 5. Create States
            // Locomotion States
            AnimatorState idleState = stateMachine.AddState("Idle", new Vector3(300, 100, 0));
            idleState.motion = idleClip;

            AnimatorState walkState = stateMachine.AddState("Walk", new Vector3(550, 50, 0));
            walkState.motion = walkClip;

            AnimatorState runState = stateMachine.AddState("Run", new Vector3(550, 150, 0));
            runState.motion = runClip;

            // Gesture & Interaction Action States
            AnimatorState waveState = stateMachine.AddState("Wave", new Vector3(300, -80, 0));
            waveState.motion = waveClip;

            AnimatorState pointState = stateMachine.AddState("Point", new Vector3(300, -20, 0));
            pointState.motion = pointClip;

            AnimatorState pickUpState = stateMachine.AddState("PickUp", new Vector3(300, 220, 0));
            pickUpState.motion = pickUpClip;

            // Set Idle as Default State
            stateMachine.defaultState = idleState;

            // 6. Build Locomotion Transitions (Smooth & responsive)
            // Idle -> Walk
            AnimatorStateTransition idleToWalk = idleState.AddTransition(walkState);
            idleToWalk.hasExitTime = false;
            idleToWalk.duration = 0.2f;
            idleToWalk.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
            idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

            // Walk -> Idle
            AnimatorStateTransition walkToIdle = walkState.AddTransition(idleState);
            walkToIdle.hasExitTime = false;
            walkToIdle.duration = 0.2f;
            walkToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");

            // Walk -> Run
            AnimatorStateTransition walkToRun = walkState.AddTransition(runState);
            walkToRun.hasExitTime = false;
            walkToRun.duration = 0.25f;
            walkToRun.AddCondition(AnimatorConditionMode.If, 0, "IsSprinting");

            // Run -> Walk
            AnimatorStateTransition runToWalk = runState.AddTransition(walkState);
            runToWalk.hasExitTime = false;
            runToWalk.duration = 0.25f;
            runToWalk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsSprinting");

            // Run -> Idle (Instant stop from sprint)
            AnimatorStateTransition runToIdle = runState.AddTransition(idleState);
            runToIdle.hasExitTime = false;
            runToIdle.duration = 0.25f;
            runToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");

            // 7. Build Action & Interaction Transitions
            // AnyState -> Wave
            AnimatorStateTransition anyToWave = stateMachine.AddAnyStateTransition(waveState);
            anyToWave.hasExitTime = false;
            anyToWave.duration = 0.2f;
            anyToWave.canTransitionToSelf = false;
            anyToWave.AddCondition(AnimatorConditionMode.If, 0, "Wave");

            // Wave -> Idle
            AnimatorStateTransition waveToIdle = waveState.AddTransition(idleState);
            waveToIdle.hasExitTime = true;
            waveToIdle.exitTime = 0.88f;
            waveToIdle.duration = 0.25f;

            // AnyState -> Point
            AnimatorStateTransition anyToPoint = stateMachine.AddAnyStateTransition(pointState);
            anyToPoint.hasExitTime = false;
            anyToPoint.duration = 0.2f;
            anyToPoint.canTransitionToSelf = false;
            anyToPoint.AddCondition(AnimatorConditionMode.If, 0, "Point");

            // Point -> Idle
            AnimatorStateTransition pointToIdle = pointState.AddTransition(idleState);
            pointToIdle.hasExitTime = true;
            pointToIdle.exitTime = 0.88f;
            pointToIdle.duration = 0.25f;

            // AnyState -> PickUp (Press E to pick up/interact)
            AnimatorStateTransition anyToPickUp = stateMachine.AddAnyStateTransition(pickUpState);
            anyToPickUp.hasExitTime = false;
            anyToPickUp.duration = 0.15f;
            anyToPickUp.canTransitionToSelf = false;
            anyToPickUp.AddCondition(AnimatorConditionMode.If, 0, "PickUp");

            // PickUp -> Idle
            AnimatorStateTransition pickUpToIdle = pickUpState.AddTransition(idleState);
            pickUpToIdle.hasExitTime = true;
            pickUpToIdle.exitTime = 0.88f;
            pickUpToIdle.duration = 0.25f;

            // 8. Save & Refresh
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[AnimatorBuilder] Astronaut Animator Controller successfully generated with all states and transitions!");
            return controller;
        }

        private static AnimationClip LoadClipFromFbx(string assetPath)
        {
            if (!File.Exists(assetPath))
            {
                // Try case-insensitive fallback search
                string directory = Path.GetDirectoryName(assetPath);
                string fileName = Path.GetFileName(assetPath);
                if (Directory.Exists(directory))
                {
                    string[] files = Directory.GetFiles(directory, "*.fbx");
                    foreach (string f in files)
                    {
                        if (string.Equals(Path.GetFileName(f), fileName, System.StringComparison.OrdinalIgnoreCase))
                        {
                            assetPath = f.Replace('\\', '/');
                            break;
                        }
                    }
                }
            }

            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (Object subAsset in subAssets)
            {
                if (subAsset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    return clip;
                }
            }

            Debug.LogWarning($"[AnimatorBuilder] No AnimationClip sub-asset found in {assetPath}");
            return null;
        }
    }
}
