# Project Overview
- Game Title: Apollo 15 (Lunar & Martian Exploration Series).
- High-Level Concept: A historically informed lunar exploration experience built on reusable Moon/Mars systems. This milestone establishes project organization, service composition and additive scene startup, not a playable EVA simulation.
- Players: Not specified; networking and multiplayer are out of scope for initial setup.
- Inspiration / Reference Games: Apollo 15 NASA mission documentation; no reference games specified.
- Tone / Art Direction: Historical space exploration and high-contrast lunar visuals; no terrain, character generation or lighting art pass in this milestone.
- Target Platform: Current editor target is StandaloneWindows64; broader release targets remain unspecified.
- Screen Orientation / Resolution: Landscape; new Canvas reference resolution 1920x1080, matching the existing Apollo15 Canvas. Not a fixed output resolution.
- Render Pipeline: Universal Render Pipeline, PC_RPAsset; Unity 6000.6.9f1, URP 17.6.0, Input System 1.20.1, uGUI 2.6.0.

## Scope and selected approach
User selected **Initial setup, preserve Apollo15**. Implement Section 9 of the supplied design as a small, testable foundation. Preserve Assets/Scenes/Apollo15.unity, SampleScene.unity, their volume profiles, existing input actions and rendering settings. Do not implement the complete framework yet.

No existing Assets/Plans directory or approved plan file was found. This is the initial plan for this milestone.

Alternatives considered:
- Preserve Apollo15 and create modular scenes (selected): safe reference baseline, minimal migration risk; some intentional duplicate authoring outside the runtime scene set.
- Migrate existing Apollo15 objects: less duplicate authoring, but changes the user's newly created scene and increases regression risk.

# Game Mechanics
## Core Gameplay Loop
Future intended loop: explore on foot or by rover, interact with equipment, deploy experiments and collect samples, receive radio feedback, complete EVA objectives and return to the lander. This milestone only boots the modular scene set and reaches a Ready state. No mission progression, movement, experiments or rover mechanics are claimed as implemented.

## Controls and Input Methods
The active backend was verified as New Input System (activeInputHandler=1). Preserve Assets/InputSystem_Actions.inputactions, including its Player actions and Hold interaction behavior. Connect the new UI EventSystem to the existing UI map after inspecting its exact actions and references. No raw Keyboard/Gamepad polling, new gameplay input reader, bindings or control changes in this milestone.

# UI
Create only a blank UI infrastructure scene, not menus, telemetry gauges or subtitles.

```text
UI_Core
└── UI
    ├── VisorCanvas [Canvas + CanvasScaler + GraphicRaycaster]
    │   └── [future HUD content; empty now]
    └── EventSystem [EventSystem + InputSystemUIInputModule]
```

VisorCanvas: Screen Space Overlay, UI layer, Scale With Screen Size, reference 1920x1080, width/height match 0.5. Exactly one EventSystem across the modular runtime scene set. No TMP assets or visible UI content are required.

# Key Asset & Context
## Verified context
- Apollo15 currently contains Environment (Directional Light, Global Volume), Gameplay (empty Player Spawn, empty GameManager, Main Camera), and UI Canvas (EventSystem).
- Main Camera already uses URP additional camera data; lighting uses URP additional light data; volume has its own empty profile.
- Build scene list currently contains only enabled Assets/Scenes/SampleScene.unity.
- Starter script Assets/NewMonoBehaviourScript.cs is absent; cleanup must safely skip it.
- Assets/TutorialInfo exists; Assets/Readme.asset directly references its Readme.cs, icon and stylesheets. Deleting only TutorialInfo would leave a broken template Readme asset.
- No game assembly definitions or custom gameplay code were discovered in the AssetDatabase inventory. Cinemachine and Test Framework are not declared in the package manifest.
- Dependency discovery used AssetDatabase.GetDependencies and a reverse-reference scan because a dedicated dependency-query tool is unavailable in this session. Repeat impact analysis before implementation edits/deletions, including checking dependents of Readme.asset.

## Folder organization (create missing directories; do not move existing assets)
- Assets/_Project/Foundation/{Audio,EventBus,Extensions,ServiceLocator,StateMachine,Utilities,Persistence}
- Assets/_Project/Core/{Cameras,Dialogue,Environment,Interaction,Inventory,Missions,Player,UI,Vehicles,SceneLoading,Application}
- Assets/_Project/Apollo15/{Animations,Audio,Data,Experiments,Materials,Prefabs,Scenes,Shaders,Bootstrap}
- Data subfolders: Dialogues, Experiments, Missions, Vehicles.
- Prefabs subfolders: Environment, Equipment, Characters, Vehicles.
- Assets/_Project/Settings and Assets/_Project/Settings/VolumeProfiles.
- Assets/Plugins and Assets/ThirdParty, only if missing.

## New assemblies and scripts
1. Foundation/SpaceSeries.Foundation.asmdef: root namespace SpaceSeries.Foundation; noEngineReferences=true; no Unity or mission-domain dependencies.
2. Foundation/ServiceLocator/IServiceRegistry.cs and ServiceRegistry.cs: explicit instance-scoped typed registration, resolution/TryResolve, duplicate-registration rejection and teardown. No static singleton or Unity calls.
3. Core/SpaceSeries.Core.asmdef: references SpaceSeries.Foundation and Unity.InputSystem. Add actual uGUI assembly reference only if Core code directly needs its types; scene components alone do not require it.
4. Core/SceneLoading/ISceneLoader.cs and UnitySceneLoader.cs: UnityEngine.Awaitable additive loading, scene-path validation, already-loaded handling and operation serialization. Keep this Unity adapter out of engine-agnostic Foundation.
5. Core/Application/IGameManager.cs, GameManager.cs and GameState.cs: minimal Booting, Ready, Failed lifecycle; explicit initialization, no singleton, no unimplemented gameplay responsibilities.
6. Apollo15/SpaceSeries.Apollo15.asmdef: references SpaceSeries.Core and SpaceSeries.Foundation. Mission-level composition may reference these lower tiers; never reverse that relationship.
7. Apollo15/Bootstrap/BootstrapConfigurationSO.cs: private serialized ordered scene paths with read-only access. A ScriptableObject belongs in the Unity-dependent mission composition tier, not Foundation.
8. Apollo15/Bootstrap/BootstrapRunner.cs: creates registry, registers ISceneLoader and IGameManager before consumers can resolve them, loads configured scenes sequentially, establishes active mission scene and reports Ready/Failed. Catch/log initialization failures at the Unity lifecycle entry point; prevent duplicate runners from loading twice; clean registry on destruction and reset per run when domain reload is disabled.
9. Apollo15/Data/BootstrapConfiguration.asset: full paths to UI_Core, Env_HadleyRille, Mission_EVA1.
10. Settings/VolumeProfiles/HadleyRilleVolumeProfile.asset: new empty independent profile, no artistic overrides.

Async requirements: use main-thread Awaitable workflows; never await the same Awaitable instance twice. Do not describe the entire loading pipeline as allocation-free: scene loading and initialization allocate. Handle teardown with destroyCancellationToken where supported. Unity AsyncOperation loading is not inherently cancellable; an operation already started must be tracked through completion and cleaned up if its owning startup was cancelled. Do not continue loading new scenes after cancellation. Track which scenes were loaded by this startup so failure cleanup cannot unload pre-existing scenes.

## New scene hierarchy
```text
Bootstrapper.unity [application lifetime; remains loaded]
└── Systems
    ├── BootstrapRunner [configuration reference; owns ServiceRegistry and scene loader]
    └── GameManager [Core GameManager component; registered by runner]

UI_Core.unity [additive]
└── UI
    ├── VisorCanvas
    └── EventSystem

Env_HadleyRille.unity [additive]
└── Environment
    ├── Directional Light
    ├── Global Volume
    └── Terrain [empty organizational placeholder]

Mission_EVA1.unity [additive]
└── Gameplay
    ├── Main Camera
    ├── Player Spawn [empty marker at origin]
    └── MissionTriggers_EVA1 [empty organizational placeholder]
```

All four scene files live under Assets/_Project/Apollo15/Scenes/. Retain Bootstrapper as a loaded additive root rather than marking its hierarchy DontDestroyOnLoad unnecessarily. Provide exactly one camera and AudioListener across these scenes. Reproduce basic existing light/camera defaults, not a lunar lighting art pass. Camera uses URP additional camera data, post-processing enabled and a volume mask that includes the Global Volume layer. No real terrain, Falcon LM, LRV, ALSEP or player is created.

## Existing assets/settings touched
- Delete Assets/TutorialInfo with its metadata only after reverse-dependency validation.
- Delete the template Assets/Readme.asset and metadata as part of the same cleanup, provided no non-template dependents are discovered. If unexpected dependencies exist, pause deletion and report them rather than removing user content.
- Skip absent Assets/NewMonoBehaviourScript.cs; if it appears before implementation, inspect dependencies first.
- Update enabled build scenes to Bootstrapper first, then UI_Core, Env_HadleyRille, Mission_EVA1. Preserve SampleScene/Apollo15 entries if present, but disable them as runtime startup entries. Do not delete their files.
- No package installations, package-source edits, generated scripting defines, URP/input settings changes or relocations of existing settings assets.

# Implementation Steps
1. **Revalidate baseline and impact** — Inventory actual assets, current scene dirty state, input action UI map, build profile scene-list override and reverse dependencies for starter cleanup. Confirm target paths are not occupied. Preserve unsaved scenes; do not silently overwrite existing files. Dependencies: approved plan.
2. **Create directories and assemblies** — Create the listed domain folders and three asmdefs. Foundation must have no Unity references. Allow Unity to import/compile before creating serialized assets depending on these types. Dependencies: Step 1.
3. **Implement engine-independent registry** — Add IServiceRegistry/ServiceRegistry with typed lookup, duplicate detection and explicit lifetime. Verify Foundation builds without UnityEngine. Dependencies: Step 2.
4. **Implement Core startup contracts/adapters** — Add Awaitable scene loader and minimal game-state manager with controlled initialization, serialized loading, failure and teardown handling. Do not add future managers as hollow production APIs. Dependencies: Steps 2–3.
5. **Implement mission composition/configuration** — Add BootstrapConfigurationSO and BootstrapRunner; wire explicit registration and ordered startup. Enforce no duplicate bootstrap work and do not rely on incidental Awake order. Create configuration asset after successful compilation. Dependencies: Steps 3–4.
6. **Create modular scenes and profile** — Create four scenes at specified paths with the hierarchy above. Configure Canvas/EventSystem using verified UI action names, URP light/camera, independent empty volume profile, GameManager and runner references. Save scenes; leave reference Apollo15 and SampleScene unchanged. Dependencies: Step 5.
7. **Cleanup template assets** — Delete TutorialInfo and the dependent template Readme together only after repeating impact analysis; safely skip absent script. Verify no remaining missing script references. Dependencies: Steps 1 and 6.
8. **Configure startup build scenes** — Set the new enabled scene order in the effective build scene list/profile without destroying unrelated platform settings. Open Bootstrapper as the entry scene for runtime checks, preserve prior editor scene setup for recovery. Dependencies: Steps 6–7.
9. **Validate and hand off** — Run the checks below and fix only identified issues. Document remaining placeholders and deferred architecture. Dependencies: Steps 2–8.

# Verification & Testing
- Compilation: no Console compile errors; every new MonoBehaviour and ScriptableObject resolves correctly after reimport.
- Assembly graph: Apollo15 -> Core -> Foundation (Apollo15 may also reference Foundation). Foundation has no UnityEngine dependencies; no cycles or mission references from lower tiers.
- Registry checks using a temporary read-only/editor execution harness rather than adding an unapproved test package: successful typed registration/resolution; duplicate registration fails clearly; TryResolve returns false when absent; clearing/disposal removes registrations; new registry is isolated from old one.
- Runtime startup from Bootstrapper: exactly four intended scenes loaded; UI then environment then mission; mission set active; state reaches Ready; Bootstrapper remains loaded.
- Repeat Play Mode with normal domain reload and with it disabled: no stale services, duplicate loads, cameras, lights, volumes or EventSystems. Restore any temporary editor testing settings.
- Failure cases: invalid/missing scene path reports Failed, no unhandled exception; no Ready state on incomplete load; empty scene configuration fails validation; stop Play Mode during loading causes no subsequent startup activity or leaked owned scenes.
- Already-loaded scene case: skip duplicate loading without taking ownership of that scene; failure cleanup does not unload pre-existing scenes.
- Hierarchy/component checks: one MainCamera-tagged enabled camera, one AudioListener, one global Volume, one Directional Light and one EventSystem in the modular runtime set. Canvas has scaler/raycaster and valid UI action references. No missing scripts or dangling template references.
- Preservation: compare reference Apollo15/SampleScene and their existing profiles/settings before/after; no content changes. Starter Readme removed only under the safe cleanup condition.
- Build configuration: all four scenes enabled, Bootstrapper first, SampleScene not enabled as startup. If a supported Windows build toolchain is available, build and launch a development player to confirm additive loading works outside the Editor; otherwise report the build check as unperformed.
- Visual validation: inspect Scene View captures of the created scenes and Game View during startup. Empty terrain/HUD/spawn placeholders are intentional; do not generate geometry solely to fill screenshots. Limit capture/fix iteration to three rounds.

## Deferred work / architecture caveats
- No character controller, rover, mission SO schemas, event channels, radio/audio/save managers, menu, navigation, terrain streaming or custom Render Graph passes in this milestone.
- Cinemachine 3 installation and camera rig wiring require a later approved milestone and version verification.
- Service location alone does not prevent execution-order bugs; explicit bootstrap ownership and registration ordering are required.
- If typed generic SO event channels are added later, create concrete serializable channel asset types; do not assume open generic ScriptableObjects can be authored as assets.
- Verify historical experiment nomenclature in later content work: Apollo 15's ALSEP Passive Seismic Experiment should not be conflated with Apollo 11's PSEP terminology. No placeholder NASA transcript is represented as an authenticated recording.

---

# Current Milestone: MoonTile Surface and Vehicle Setup

This revision extends the existing plan rather than creating a new plan. The sections above are historical foundation scope, NOT instructions to rerun starter cleanup or create modular scenes during this task. Where they conflict, this milestone takes precedence. In particular, the user explicitly selected editing Apollo15, overriding the earlier preservation restriction for that scene only.

# Project Overview
- Game Title: Apollo 15.
- High-Level Concept: A lunar surface exploration prototype using the supplied MoonTile, LM and Rover models. This milestone provides a driveable rover and stationary lunar mission base within the current Apollo15 scene, not a flight or complete EVA simulator.
- Players: One locally controlled rover/base view; multiplayer is out of scope.
- Inspiration / Reference Games: Apollo 15 lunar exploration; no reference game requested.
- Tone / Art Direction: Preserve existing lunar/vehicle art and URP presentation. No new art generation or lighting overhaul.
- Target Platform: StandaloneWindows64, current project target.
- Screen Orientation / Resolution: Desktop landscape, native resolution; preserve existing Canvas scaling and display settings.
- Render Pipeline: URP PC_RPAsset; Unity 6000.6.9f1, URP 17.6.0, Input System 1.20.1.

## Selected approach and alternatives
User selected: **current Apollo15 scene**, **stationary mission-base LM**, **direct vehicle switching**, **larger exploration area**.
- Surface: finite, editor-generated 7×7 prefab grid with configurable dimensions and tile scale. Deterministic placement keeps iteration and collision inspection straightforward. Streaming is an alternative for much larger worlds but unnecessary for 49 tiles.
- Rover: reuse existing Rigidbody/WheelCollider RoverController rather than implement a competing transform-based motor. Physics supports slopes and braking, but requires wheel mapping and suspension tuning. Simple kinematic driving would be easier to tune but would discard current architecture and believable terrain contact.
- Control: direct rover driving plus LM overview selection provides immediate feedback without generating an astronaut. Astronaut boarding would enrich exploration immersion but needs an on-foot player/camera not currently configured.
- LM: stationary collidable mission-base component and inspection hook; no thrust, takeoff, moving lander or staged ascent.

# Game Mechanics
## Core Gameplay Loop
Start in rover control, drive across the finite lunar surface, brake and inspect the stationary LM through a base overview, then return to rover driving. Clear control feedback and responsive traversal support exploration and player competence; no quests or sample-collection objectives are added.

## Controls and Input Methods
- Verified PlayerSettings.activeInputHandler=1: New Input System only.
- Create a dedicated mission action asset to avoid changing existing Player/UI maps or their Hold interactions. Use performed/canceled callbacks, cached movement/braking state, and explicit action-map ownership. No legacy Input calls or raw Keyboard/Gamepad polling.
- Rover map: WASD/arrows or gamepad left stick = steering/throttle; Space or gamepad left trigger = brake.
- Selection: 1 = LM overview, 2 = Rover; gamepad shoulder buttons cycle the explicit registered targets. The LM is a view/inspection target, never a moving vehicle.
- E or gamepad north button = inspect the LM when selected, invoking its public inspection event/hook. A lightweight scene control component routes this through the existing IInteractable contract with an explicitly configured inspection origin; no invented mission rewards.
- Disable/scene unload/focus loss: clear cached inputs and apply rover parking brake. Selecting LM also clears and parks the rover. Only one driver may control the rover; failed Board calls must not take control or switch cameras.
- Use one existing main camera and AudioListener. Add a dependency-free follow/orbit component with configured rover and LM view anchors, collision avoidance if necessary, and mouse/right-stick look callbacks. No Cinemachine package installation.

# UI
Preserve the existing empty Canvas and EventSystem; no new visible HUD, menus or fonts in this milestone. Controls are documented in the handoff; switching visibly changes the camera between rover-follow and LM overview.

```text
Game View
+------------------------------------------+
| Lunar surface / rover follow or LM view   |
|                                          |
|         No new HUD content               |
+------------------------------------------+
Existing UI Canvas + EventSystem unchanged
```

# Key Asset & Context
## Verified current state
- Assets/Scenes/Apollo15.unity is loaded and clean. It contains Environment light/volume, empty Systems/GameManager, Gameplay/Player Spawn, Cameras/Main Camera and UI infrastructure.
- Scene LM instance is named Meshy_AI_model 2, referencing Assets/_Project/Apollo15/Prefabs/Vehicles/LM.prefab. Rover instance references Assets/_Project/Apollo15/Prefabs/Vehicles/Rover.prefab. Neither has controller scripts or colliders.
- LM: 408 renderers, approximately 0.92×0.96×0.90 units. Rover: 737 renderers, approximately 0.20×0.12×0.10 units. Both root scale values are 1, but models are undersized and prefab root translations contain scene-style offsets. Preserve materials and mesh content; normalize visual pivots/scale using child wrappers, not destructive FBX edits.
- Moon source is Assets/_Project/Apollo15/Prefabs/Vehicles/MoonTile.fbx, an imported model prefab, NOT a standalone .prefab. Approximately 0.96×0.18×0.96 units at imported scale 100 and X rotation 270 degrees; 7,336 vertices; no collider. Preserve required imported rotation/scale inside a wrapper. It reports URP/Lit Material.001; inspect actual material/texture assignment before assuming a textured terrain appearance.
- RoverController.cs already implements IVehicle with Board, Dismount, SetDriveInput, ApplyHandbrake and ReleaseHandbrake, custom gravity 1.62 m/s² and four-wheel counter-steering. Despite its summary calling it kinematic, implementation uses Rigidbody forces and WheelColliders.
- RoverWheelPair is a nullable reference class and Update currently dereferences all four pairs unconditionally: configuration validation/null safety is required.
- RoverSeat exists but requires AstronautController and uses polled actions; do NOT attach or rewrite it for this direct-control scope.
- No RoverConfigSO asset was found. No current Console errors were reported.
- Actual project has Foundation/Core/Apollo15 assemblies and gameplay scripts, but no modular startup scenes were found. Do not assume prior foundation plan was fully executed.
- AssetDatabase.GetDependencies plus a reverse-reference scan was used because no dedicated dependency query tool is available. Apollo15 directly depends on LM and Rover prefabs; no other prefab dependents were found. Repeat impact analysis for every existing edited asset before implementation, including scene, scripts and any reused materials.

## Assets to create
- Assets/_Project/Apollo15/Prefabs/Environment/MoonTile.prefab: wrapper referencing supplied MoonTile.fbx, unit-scale wrapper, configured visible scale and static non-convex MeshCollider(s).
- Assets/_Project/Core/Environment/MoonSurfaceLayout.cs: serialized tile reference, rows/columns, placement spacing/scale and generated-root reference. Configuration only at runtime; no automatic regeneration in Awake/OnValidate.
- Assets/_Project/Apollo15/Editor/MoonSurfaceBuilder.cs and editor-only assembly definition if needed: Undo-aware explicit surface generation, deterministic child naming, regeneration limited to its owned terrain hierarchy.
- Assets/_Project/Core/Vehicles/Components/LunarModuleController.cs: stationary base identity, inspection anchor/event and IInteractable hook; no flight input or unnecessary dynamic Rigidbody.
- Assets/_Project/Core/Vehicles/Components/DirectRoverDriver.cs: IVehicleDriver implementation, event-driven input routing, Board/Dismount ownership and input cleanup.
- Assets/_Project/Core/Vehicles/Components/VehicleControlSelector.cs: explicit rover/base registration and selection, no global scene searches or service singleton requirement.
- Assets/_Project/Core/Cameras/VehicleFollowCamera.cs: one camera, serialized target anchors, follow smoothing, safe target changes and event-driven look.
- Assets/_Project/Apollo15/Data/Vehicles/RoverConfig_LRV.asset: reuse RoverConfigSO, lunar gravity 1.62; starting forward/reverse limits from existing defaults, tuned wheel suspension/friction and mass for prototype handling, not certified historical simulation.
- Assets/_Project/Apollo15/Data/Vehicles/VehicleControls.inputactions: dedicated Rover/Selection/Camera actions and keyboard/gamepad bindings above; create only required persistent action references.

## Existing assets to modify
- Assets/_Project/Core/Vehicles/Components/RoverController.cs: narrow changes for configuration validation, null-safe wheel pairs, initial unoccupied parking brake and clearing brake/input state on lifecycle/control transitions. Retain IVehicle API and existing physics model.
- Assets/_Project/Apollo15/Prefabs/Vehicles/Rover.prefab: add Rigidbody, simple compound chassis colliders, four correctly positioned WheelColliders, controller/config reference, normalized visual child wrapper, mount/view anchors and direct-driver component where prefab-safe. Scene-only selection/camera references stay in the scene.
- Assets/_Project/Apollo15/Prefabs/Vehicles/LM.prefab: normalized visual child wrapper, simple static collision shapes, LunarModuleController and base/inspection anchors.
- Assets/Scenes/Apollo15.unity: Environment/MoonSurface 7×7 grid; relocate existing LM and Rover instances into Gameplay/Vehicles without duplicating them; add selection/input composition to Systems and configure existing main camera.
- Leave source FBXs/textures, existing input assets, UI, light/volume, unrelated scripts/scenes, packages and build scene settings unchanged. If an existing material is unusable, make a dedicated terrain material with existing available textures rather than altering unrelated assets.

# Implementation Steps
1. **Revalidate and inspect geometry.** Repeat dependency/reverse-dependency checks and confirm scene dirty state, prefab overrides and target-path availability. Capture model geometry from multiple angles to identify real wheel groups, forward axis and terrain boundary compatibility. Measure exact bounds/vertices at sufficient precision. Dependencies: approval of this revision.
2. **Prepare normalized prefab geometry.** Use unit-scale physics roots and child visual scaling/offsets. Use starting visual proportions of roughly 9 m LM footprint and 3.1 m Rover length, adjusted from measured dominant axes. Keep existing prefab identities and materials. Identify complete wheel mesh groups; do not rotate guessed model_part indices. If wheels cannot be isolated safely, retain static wheel artwork with physical wheels and clearly report visual wheel animation limitation. Dependencies: Step 1.
3. **Author tile wrapper and surface generator.** Make terrain tile footprint approximately 20×20 m while retaining its existing relief and imported orientation. Derive spacing from exact transformed mesh bounds, not rounded logs. Generate 7×7 (~140×140 m) under Environment/MoonSurface with fixed orientation and consistent elevation; no independent random rotation or height offsets. Sample boundary heights to verify repeatability. If source edges are not tileable, pause and report the seam issue before claiming a seamless surface; do not conceal gaps with arbitrary overlap or deform source meshes without further approval. Add static colliders and optional unobtrusive owned perimeter collision barriers to prevent vehicles dropping beyond the finite test area. Dependencies: Steps 1–2.
4. **Configure rover physics.** Create RoverConfig_LRV, add Rigidbody and simple compound chassis colliders that do not intersect tire contact patches. Add four WheelColliders at measured wheel centers with correct local Y suspension and Z forward axes, unit-scale parents, radius/suspension tuned to lunar gravity. Assign all pairs and config; implement the narrow RoverController safeguards listed above. No per-renderer 737-collider generation. Dependencies: Steps 1–2.
5. **Implement stationary LM.** Add simple static collision shapes without blocking the future ladder/entry path unnecessarily. Add LunarModuleController, inspection event and view anchors. No thrust actions, rigidbody gravity or flight controls. Dependencies: Step 2.
6. **Implement input, direct selection and camera.** Create dedicated action asset and components; event-driven caches feed SetDriveInput. Register explicit rover/base targets, default to rover, park on base selection, reject ownership conflicts, retain one camera/listener and cleanup actions on disable. Scene inspection origin bridges LM IInteractable inspection without an astronaut. Dependencies: Steps 4–5 and successful script compilation.
7. **Compose Apollo15 scene.** Generate terrain, reposition the existing linked prefab instances with safe separation, raycast real collision surface to place rover wheel contacts and LM footpads, and move Player Spawn onto safe ground. Align prefab overrides so physics receives intended scales. Add scene control references and camera targets. Preserve light/volume/UI and save only intended assets. Dependencies: Steps 3–6.
8. **Verify and hand off.** Test controls, contact, switching and finite-area edges; visually validate fresh multi-angle Scene View captures and Game View. Reuse the same construction script for fixes; at most three capture/fix rounds. Provide controls and any mesh/seam/visual-animation limitations. Dependencies: Step 7.

# Verification & Testing
- Zero new compile/Console errors and missing script/reference warnings. Core remains mission-independent and editor assembly does not enter player builds.
- Terrain count exactly 49, shared model/material references, deterministic names/spacing and no duplicate surface after explicit regeneration. Re-running generation does not destroy unrelated scene children.
- Boundary checks: adjacent tile geometry/colliders continuous within measured tolerance, no visible gaps/z-fighting; drive across row/column boundaries in both directions without falling or catching tires. If not repeatable, report blocked seam validation.
- Rover: all wheel pairs/config assigned, Rigidbody dynamic with Earth gravity disabled, custom 1.62 m/s² applied once, unit-scale physics root, stable rest contact, no chassis penetration, usable steering/reverse/braking and sensible speed limits. Test slopes and emergency parking; do not equate tuned gameplay handling with scientifically exact tire simulation.
- LM: stays stationary, rests on footpads without floating/sinking; static collision blocks rover appropriately; inspection hook executes exactly once per performed button action and supplies no fake mission progress.
- Selection: initial rover control, 1/2 and gamepad cycling select intended targets, LM selection parks rover, returning resumes only after input routing is restored. Test repeated switching, canceled input, focus loss, disabling driver, missing targets and Board rejection.
- Camera: target switches correctly; one enabled main camera and one AudioListener; no ground/model clipping during representative follow/orbit views.
- Persistence: save/reopen Apollo15, repeated Play Mode and domain reload disabled retain wired references without duplicate input subscriptions or stale movement.
- Preservation: unchanged source FBXs, existing input/UI assets, lighting profiles/settings, packages, SampleScene and build configuration; inspect intended prefab override differences.
- Visual/performance sanity: compare LM/Rover scale, separated placement, terrain coverage and materials in current URP. Existing vehicle meshes have hundreds of renderers, so report rendering cost as a known limitation rather than promise optimized performance or undertake unapproved mesh merging.
- Windows player build/run only if the supported toolchain is available and build validation can use a temporary build scene list without permanently changing project settings; otherwise explicitly mark unperformed.

---

# Current Milestone: Generic Scientific Experiment Deployment

This revision is the active scope. Previous milestones are context only: do not rerun terrain generation, starter cleanup, or modular startup creation. User selected integration into Apollo15, an equipment-placement animation, a new on-foot player, and the existing character at `Assets/Stylized_Astronaut/Stylized Astronaut.prefab`.

# Project Overview
- Game Title: Apollo 15.
- High-Level Concept: Explore the lunar surface on foot, approach a scientific deployment zone, and deploy its configured experiment. Reusable data-driven components coordinate placement animation, spawning, radio dialogue and mission objective completion without experiment-specific code.
- Players: Single local player; networking is outside this milestone.
- Inspiration / Reference Games: Apollo 15 lunar science activities; no reference game specified.
- Tone / Art Direction: Preserve current lunar art. Use the supplied stylized astronaut and existing equipment models, with a simple clearly identified placeholder equipment-placement animation. Placeholder dialogue is authored prototype text, not an authenticated NASA transcript.
- Target Platform: StandaloneWindows64.
- Screen Orientation / Resolution: Desktop landscape; preserve existing Canvas scaler/display configuration and validate at 1920x1080 and 1280x720.
- Render Pipeline: Existing URP PC_RPAsset, Unity 6000.6.9f1. Active input backend verified as New Input System only (`activeInputHandler=1`). No package, input-backend, rendering or build-settings changes.

## Selected approach and alternatives
- Selected: reusable Core components plus Apollo15 scene wiring and two data-only examples. Preserve existing dialogue, mission, input and player contracts rather than building competing managers.
- Animation sequencing: an actual Animator-driven placement clip with a completion relay gates spawning. This gives clear cause/effect feedback (MDA) and avoids completing an objective at the initial key press. A fixed delay is simpler but can desynchronize from animation speed/interruption; not selected.
- Detection: trigger-based zone eligibility and a single player deployer selecting the nearest eligible overlapping zone. This fulfills “inside zone + E” without requiring a camera aim ray. The zone also implements IInteractable for future reuse, but no second E listener/interactor is added to this player.
- Content extensibility: adding an experiment means creating an ExperimentDefinitionSO and assigning it to another deployment-zone prefab instance. No name switch, prefab-specific branch or per-experiment script is permitted.

# Game Mechanics
## Core Gameplay Loop
Select on-foot control, walk to a marked deployment zone, see `[E] Deploy <Experiment name>`, press E, watch placement animation, then see the equipment appear and receive dialogue/objective feedback. Repeat at a second zone. Clear prompts and visible consequences support player competence (SDT); data-authored independent zones support exploration. Each zone deploys once per scene run; save persistence, equipment inventory requirements and multiplayer synchronization are excluded.

## Controls and Input Methods
- Preserve existing 1 = LM overview, 2 = rover, cycling, driving, braking and camera look controls.
- Add 3 = on-foot control through a new `Selection/OnFoot` action in VehicleControls; extend existing target cycling to include on-foot mode.
- On foot: WASD/arrows or gamepad left stick = camera-relative movement; Shift/gamepad stick press = sprint; mouse/right stick = camera look; E/gamepad north button = deploy while inside an eligible zone.
- Dialogue advance uses Enter/gamepad south button, not E, preventing the deployment key from skipping its newly started dialogue.
- Start in on-foot mode for this milestone. Selecting another mode disables on-foot movement/deployment and parks the rover when appropriate. Only one gameplay actor/input owner and one camera/listener may be active.
- Use dedicated `ExperimentControls.inputactions` for on-foot Move, Sprint and Deploy plus Dialogue/Advance, with event-driven callbacks and explicit ownership. Preserve existing Player/UI action maps and Hold interactions.
- While placement or deployment dialogue is active, reject repeat deployment and control switching. Restore previous control/locomotion state on normal completion; cancel safely on disable, focus loss or unload without reviving a disabled player or overriding another control owner.

# UI
Use the existing Canvas and EventSystem, with a new scene-owned child HUD prefab rather than changing shared UICanvas/EventSystem prefabs. Reuse InteractionPromptUI, CurrentObjectiveUI, DialogueUI and TypewriterEffect; inspect their serialized fields before wiring. Keep event-listener components active while hiding their visual panel children, so a hidden prompt can receive later events.

```text
+----------------------------------------------------------+
| Objective: Deploy scientific equipment                    |
|                                                          |
|                    Lunar scene / astronaut                |
|                                                          |
|             [E] Deploy Passive Seismic Experiment         |
| +------------------------------------------------------+ |
| | CAPCOM                                               | |
| | Prototype deployment confirmation text...            | |
| |                               [Enter] Continue        | |
| +------------------------------------------------------+ |
| 1 Base   2 Rover   3 On foot                              |
+----------------------------------------------------------+
```
- High-contrast readable text on a subdued dark panel; hide dialogue until requested and prompt outside eligible zones. Non-interactive HUD graphics must not intercept pointer input.
- Reuse an existing suitable TMP font/material; inventory it before implementation, verify glyph coverage, fixed font size and disabled runtime AutoSize. Do not duplicate font atlases or import example packages solely for this HUD.
- After deployment, prompt disappears for that zone. Objective UI reflects actual MissionManager state, not a separate fake completion flag. Equipment placement is a real visible animation, not only an emitted animation event.

# Key Asset & Context
## Verified existing systems and constraints
- Apollo15 is loaded and clean. VehicleControlSelector is the active gameplay input owner, using a cloned VehicleControls asset. It implements IInteractor for LM inspection, but TriggerInteractionAnimation currently does nothing. No astronaut, mission manager, dialogue runner or authored quest/dialogue/channel assets are wired in this scene.
- Core assembly references Foundation, Unity.InputSystem, Unity.TextMeshPro and Unity.ugui. Keep generic experiment scripts in Core and Apollo15-specific composition/data in Apollo15; do not add reverse assembly dependencies.
- AstronautController coordinates IPlayerInputProvider, IAstronautMotor, IGroundDetector and IAstronautAnimator. Reuse existing motor/ground detection rather than writing a parallel controller. Add a dedicated animator adapter for the supplied rig rather than feeding its controller nonexistent parameters from AstronautAnimator.
- Supplied astronaut prefab has a CharacterController, valid Generic Avatar (not Humanoid), a 15-bone simplified skeleton, legacy AstronautPlayer movement, and an enabled Player_Camera with AudioListener and legacy camera script. Preserve its authored rig and Generic-compatible embedded Idle/Walk/Run clips. Do not convert to Humanoid, regenerate mesh/rig or use Humanoid-only retargeting; no such conversion is required for equipment animation.
- Create a project-owned player wrapper from the supplied visual/skeleton hierarchy, retaining original scale relationships and Avatar. Remove legacy movement/camera behavior and the extra camera/listener only from the project-owned output, never from vendor source assets. Core must not reference vendor Assembly-CSharp types.
- DialogueSequenceSO is the existing dialogue definition type. DialogueChannelSO.RequestDialogue(sequence) reaches DialogueRunner when wired. DialogueRunner currently requires a non-null DialogueUI in ShowCurrentLine; wiring and preflight validation must prevent null UI playback.
- MissionManager listens to QuestTriggerEventChannelSO. QuestTriggerPayload has distinct QuestId and ObjectiveId, and HasContent requires ObjectiveId. Therefore ExperimentDefinitionSO includes both IDs; never interpret Quest ID as Objective ID or complete the entire mission implicitly.
- MissionManager can accept objectives outside the current sequential step through its fallback lookup. Deployment must independently check eligibility against the active step when sequential mode is used. The two-example quest uses an open checklist so either experiment can be deployed first.
- AstronautInteractor currently dispatches quest/dialogue immediately after Interact succeeds. Do not route deferred deployment through these immediate payloads. Experiment zone returns accepted interaction with empty immediate payloads; the deployment coordinator alone owns delayed completion.
- Existing equipment prefabs: Seismometer.prefab and Station.prefab under Apollo15/Prefabs/Equipment. Both have root scale 100, no colliders/Animator, and scene-style root positions. Their measured visible sizes are approximately 0.94x0.66x0.64 m and 0.96x0.84x0.66 m. Use wrapper prefabs with normalized physics roots and foot-aligned visual children; preserve source FBXs/materials/prefabs.
- Apollo15 already directly references both equipment prefabs. Before placing zones, inspect existing equipment scene instances and their purpose; reuse their positions as zone anchors when suitable, do not leave a pre-deployed duplicate at a demonstration zone. Move existing demonstration instances into an inactive scene-owned authoring-reference group if necessary, preserving prefab links and transforms.
- Dependency and reverse-reference analysis used AssetDatabase.GetDependencies and an Assets reverse scan because a dedicated dependency tool is unavailable. Repeat before every existing-asset edit, including scripts, input asset and scene; code references require repository search as well as serialized-asset dependencies.

## New generic Core assets
Under `Assets/_Project/Core/Experiments/`:
- `Data/ExperimentDefinitionSO.cs`: CreateAssetMenu, private serialized experiment name, GameObject prefab, DialogueSequenceSO dialogue, string quest ID, string objective ID; read-only properties and authoring validation. Definition assets never store deployed/busy/runtime state.
- `Components/ExperimentDeploymentZone.cs`: IInteractable, trigger Collider, definition, spawn anchor, zone selection distance/priority, scene-only state Available/Deploying/Deployed, spawned-instance reference and deployment reservation. Track eligible actors across multiple colliders without premature exits; reject non-player colliders. Clear occupancy safely on disable/destroy.
- `Components/ExperimentDeployer.cs`: single player-side E handler and zone selection, reservation/reentrancy guard, preflight, orchestration, cancellation, prompt updates and explicit scene service references. Public TryDeploy(zone) permits tests and future input integration. No global Find/service-singleton reliance.
- `Interfaces/IDeploymentAnimationPlayer.cs`: start/cancel/completion contract with per-run identity; serialized MonoBehaviour adapter validated against the interface.
- `Components/EquipmentPlacementAnimationPlayer.cs` and `DeploymentAnimationRelay.cs`: own placement visual/Animator, validate expected trigger/state/completion relay, reposition at selected zone, complete once only for the active run. Stop/reset on cancellation; stale or duplicate completion callbacks cannot spawn equipment. Use a maximum timeout as a failure guard, never as the normal completion mechanism.
- `README.md`: authoring/wiring guide and steps to add another experiment without code edits, including quest/objective mapping, zone size, anchor orientation and animation replacement.

Other new project-owned Core scripts:
- `Player/Components/ExperimentPlayerInputProvider.cs`: IPlayerInputProvider backed by the owned on-foot action map; callback caches, input cleanup and explicit enable/disable ownership.
- `Player/Components/StylizedAstronautAnimatorAdapter.cs`: IAstronautAnimator implementation using only validated controller parameters, with project-owned Speed-driven locomotion and safe unsupported-gesture behavior. Equipment-placement animation remains separate from the astronaut rig.

## New Apollo15 assets
- `Prefabs/Characters/ExperimentAstronaut.prefab`: project-owned wrapper with supplied visuals/Generic Avatar, fitted unit-scale CharacterController, existing AstronautController/motor/ground detector, new input/animation adapters, deployer and a view anchor. No vendor movement scripts, second camera/listener or AstronautInteractor.
- `Animations/ExperimentAstronautLocomotion.controller`: uses exact embedded Idle, Walk and Run clips from supplied Astronaut.fbx with Generic Avatar and compatible Speed parameters. Do not mutate vendor controller, importer or clip loop settings. Validate imported clip events and add minimal receiver only if actually required.
- `Data/Experiments/AstronautMovement_Deployment.asset`: reuse AstronautMovementSettingsSO; gravity 1.62 m/s² and conservative walking/sprinting defaults, ground masks validated against actual MoonSurface collision.
- `Data/Experiments/ExperimentControls.inputactions`: on-foot/Dialogue maps described above; only required persistent references.
- `Prefabs/Equipment/DeployableSeismometer.prefab`, `DeployableStation.prefab`: wrapper roots with foot-aligned original visuals, appropriate simple static colliders and no destructive model changes.
- `Prefabs/Equipment/ExperimentDeploymentZone.prefab`: generic trigger, marker and spawn anchor; scene-only service/player references stay out of the prefab.
- `Animations/EquipmentPlacement.anim`, `EquipmentPlacement.controller`, `Prefabs/Equipment/EquipmentPlacementRig.prefab`: a simple procedural placeholder placement indicator/mechanism lowers to the anchor and finishes; the experiment itself is instantiated only after animation completion. Shared rig can serve every experiment; replaceable with a real actor animation adapter later.
- `Data/Experiments/Experiment_Seismometer.asset`, `Experiment_Station.asset`: two different names/prefabs/dialogues/objective IDs demonstrating no-code extensibility.
- `Data/Dialogues/Deployment_Seismometer.asset`, `Deployment_Station.asset` and optional prototype CAPCOM speaker asset: short placeholder confirmations, no fabricated voice recording and no quest actions on sequence end that duplicate deployment completion.
- `Data/Missions/Objective_DeploySeismometer.asset`, `Objective_DeployStation.asset`, `Quest_ExperimentDeploymentDemo.asset`: use existing schemas, distinct objective IDs, matching quest ID and non-sequential checklist.
- `Data/Experiments/Channels/`: prompt, dialogue, incoming quest and quest-system channel instances using existing concrete SO types.
- `Prefabs/UI/ExperimentDeploymentHUD.prefab`: minimal prompt/objective/dialogue presentation under the existing Canvas, using existing UI components.

## Existing assets to modify
- `Assets/Scenes/Apollo15.unity`: add one canonical project-owned astronaut at safe Player Spawn, two separate deployment zones on the existing collision surface, placement rig, MissionManager/DialogueRunner, explicit references and HUD. Preserve terrain, light/volume and unrelated scene content.
- `Core/Vehicles/Components/VehicleControlSelector.cs`: add explicit on-foot target registration/mode, 3/cycle selection, default on-foot configuration, camera handoff and temporary activity lock. Keep existing public rover/base entry points and Board rejection behavior. E remains LM-only in base mode; deploy E is enabled only in on-foot mode. Do not control a hidden/inactive astronaut from rover mode.
- `Apollo15/Data/Vehicles/VehicleControls.inputactions`: additive Selection/OnFoot action only; preserve existing actions, IDs and bindings. Selection/Cycle includes the third registered target in code.
- Only if verification shows it necessary: narrow lifecycle/configuration guards in DialogueRunner for disable/unload cleanup and missing UI; separate cancellation from normal dialogue completion so unload cannot run quest end actions or re-enable an inactive player. Do not refactor unrelated dialogue behavior.
- Leave vendor astronaut assets, original equipment assets, source FBXs/materials, existing UI prefabs, physics/render settings, packages and build scene list unchanged.

## Deployment transaction and failure behavior
1. On performed E, refresh selected-zone eligibility and choose one nearest eligible zone (stable priority/instance tie-break). Player must be on foot, enabled, inside the zone and not already deploying/in dialogue.
2. Validate non-null definition, name/prefab, valid spawn anchor/placement space, active animation adapter, playable nonempty dialogue and wired active runner/UI, matching active QuestId and valid incomplete ObjectiveId. Enforce current objective for sequential quests. Report invalid wiring once and do not begin animation/spawn/progress.
3. Reserve zone and deployer synchronously before animation starts; cache original control state, lock switching/movement and hide prompt. Runtime reservation is not stored on the shared SO.
4. Play the actual placement animation at the zone spawn anchor. Cancellation/interruption/timeout releases uncommitted reservation, resets visuals and does not spawn, request dialogue or complete an objective.
5. On matching animation completion, revalidate zone/player/services/objective, instantiate the configured prefab at exact anchor position/rotation under a scene-owned deployed-experiments root, and record it immediately. If instantiation fails, release reservation and report failure without awarding progress.
6. Mark zone Deployed once the physical spawn succeeds, request the configured DialogueSequenceSO through the wired dialogue integration, then raise a completing QuestTriggerPayload with QuestId and ObjectiveId. Objective completion occurs after dialogue is triggered, not after the dialogue finishes.
7. Confirm actual mission objective state changed; never claim completion based solely on channel emission. Any late service exception/failure is reported honestly; retain the spawned equipment and committed zone to prevent duplicates. Do not silently roll back or auto-repeat external dialogue/quest effects.
8. Release deployment-only lock after commit while respecting dialogue's own input/locomotion lock. Cancellation/disabling during post-spawn dialogue never deletes committed equipment or repeats quest awards. Restore input and switching only when the owning mode remains valid.

# Implementation Steps
1. **Revalidate baseline/impact.** Inspect scene objects/overrides, actual vendor rig/clips/events, input actions, quest/UI signatures, fonts and planned output paths. Repeat dependency/reverse/code-reference checks. Preserve unsaved edits and do not overwrite unrelated assets. Dependencies: approval of this revision.
2. **Implement generic deployment contracts/data/state.** Add SO, zone, deployer and animation interface/relay under Core; validate serialized references and separate per-zone state from SO content. Implement transaction and cancellation rules above. Compile before authoring assets. Dependencies: Step 1.
3. **Author actual placement animation.** Build project-owned generic placement rig/controller/clip and completion event/relay. Verify visible playback, completion identity, timeout, speed changes, cancel/reset and no premature spawn. Dependencies: Step 2.
4. **Prepare character animation-ready output.** Build non-destructive project-owned astronaut wrapper using its already usable Generic rig/Avatar; exclude legacy movement and camera. Create and validate compatible Idle/Walk/Run controller before adding gameplay adapters. Fit CharacterController to measured local bounds and keep imported visual scale inside child hierarchy. No Humanoid conversion or generated rig/animation assets. Dependencies: Step 1.
5. **Wire on-foot movement/input.** Implement input provider/animator adapter and attach existing motor/controller/ground detector. Create dedicated action asset and lunar settings; assign existing main-camera transform explicitly before initialization. Spawn exactly one canonical player on actual surface via collision raycast, not world Y=0. Dependencies: Steps 2 and 4; character controller animation wiring validated first.
6. **Extend control selection.** Add on-foot mode and action, default mode setting, camera anchor and deployment/dialogue lock. Reuse existing VehicleFollowCamera with on-foot follow distance and existing look route; park/disconnect rover when on foot. Validate independent input ownership and single camera/listener. Dependencies: Steps 3 and 5.
7. **Author experiment/mission/dialogue data.** Create normalized equipment wrappers, generic zone prefab, two definitions, distinct objectives, demo quest, two short dialogue sequences and channels. Preflight IDs/sequence content; do not use one experiment asset's runtime state to disable another zone. Dependencies: Steps 2–3 and successful compilation.
8. **Compose scene services and minimal HUD.** Add mission/dialogue components with explicit references, HUD under existing Canvas, placement rig and two zone instances. Reuse existing equipment positions when suitable; preserve/deactivate authoring references rather than duplicate pre-deployed equipment. Wire deployer/services/selection and place anchors/markers on safe non-overlapping surface patches. Apply only necessary dialogue teardown guards after impact analysis. Dependencies: Steps 5–7.
9. **Verify, save and document.** Run transaction, multi-experiment, movement, lifecycle and regression checks below. Save intended assets/scene, reopen and repeat. Provide controls and README showing authoring of a third experiment with no code edits. Dependencies: Steps 2–8.

# Verification & Testing
- Compilation/architecture: zero new compile errors or missing references, Core remains independent of Apollo15 and vendor Assembly-CSharp types; no package source/define/version-guard changes.
- Data-only extensibility: use two different definitions in two zones and deploy both without code edits. A temporary third definition assigned to a zone must work through the same pipeline. Two zones sharing one definition maintain independent runtime state; SO assets remain unmodified after Play Mode.
- Ordering: record animation-start, animation-complete, spawn, dialogue-start and objective-complete timestamps/call counts. Assert no prefab or objective completion before matching animation-complete, dialogue starts only after spawn, then the exact objective completes once.
- Zone/input: outside-zone E does nothing; enter/exit updates prompt; multiple player colliders do not cause premature loss; non-player colliders cannot deploy; overlapping zones select only one deterministic target; refresh eligibility at E avoids stale focus.
- Reentrancy: spam/hold E, simultaneous TryDeploy calls, duplicate/stale animation callbacks and reentering a deployed zone yield exactly one spawn and one dialogue/quest dispatch per zone.
- Validation/failure: missing SO/prefab/anchor/animation/state/relay/UI/runner/channel, empty dialogue, wrong QuestId, nonexistent/already completed objective, blocked spawn space and sequential out-of-order objective fail clearly without awarding progress. Test maximum timeout and stopped/disabled Animator; never let fallback timing masquerade as animation success.
- Cancellation: leave/disable/unload the actor or zone during placement, lose focus or stop Play Mode. No uncommitted spawn/reward, stale callback or leaked lock. Define leaving a zone during placement as cancellation; normal movement is locked, but external relocation/destruction must still be handled.
- Post-commit failure: inject late dialogue/quest failure; preserve one committed equipment instance, report incomplete side effect accurately, do not automatically duplicate equipment/dialogue/reward. Confirm actual mission state rather than trusting an event alone.
- Missions: matching QuestId + ObjectiveId completes only intended objective; either example order works; both complete the demo quest. No dialogue end action duplicates objective completion.
- Character: supplied Generic Avatar remains valid, Idle/Walk/Run playback works with chosen skeleton, no missing-parameter warnings, no legacy Input exceptions. Stable CharacterController grounding, lunar gravity, sprint and camera-relative movement over existing tile boundaries; source vendor files unchanged.
- Control regressions: 1/2/3 and cycling select expected targets; rover parks on foot/base; E deploys only on foot and inspects LM only in base mode; no rover motion from on-foot WASD; switching is rejected while deployment/dialogue holds lock and restored afterwards. Board failure never transfers ownership.
- Camera/UI: exactly one enabled gameplay camera and AudioListener; no vendor player-camera interference; readable prompt/objectives/subtitles at two resolutions; prompt listener survives hidden panel state, dialogue advance uses separate action, inactive panels can reopen repeatedly.
- Lifecycle: disable/re-enable components, load/reopen scene and repeated Play Mode with domain reload enabled/disabled create no duplicate subscriptions, stale occupancies or static state. Event channels are existing concrete types and new runtime data is instance-owned.
- Automated coverage: use existing Test Framework if available; do not install a new test package without approval. Otherwise use a scoped editor validation harness/manual runtime checks and state explicitly which tests were performed. Pure state tests cover reservation, one-shot commit, cancellation and callback identity; Play Mode tests cover Animator/trigger/input/services together.
- Preservation: compare original astronaut/equipment assets, lighting/terrain, existing Player/UI actions, unrelated prefabs/scenes/packages/settings before/after. Confirm only intended additive VehicleControls action and selector/scene changes.
- Visual validation: inspect fresh Game View and multi-angle scene captures for grounded player/equipment, unobstructed zone anchors and visible placement animation; at most three capture/fix rounds. Do not claim historical animation authenticity.
- Optional Windows development build/run using temporary scene-list override only if toolchain available; otherwise mark build check unperformed.

---

# Current Milestone: Astronaut Animator, Seismometer Configuration and Rover Boarding

This revision supersedes conflicting earlier milestone instructions. Refine the existing implementation; do not rebuild the scene, terrain, player or UI. The user selected **nearby boarding with E**, **deploy_seismometer as the parent quest ID**, and **hidden rider mesh while driving** because the supplied character has no seated clip.

# Project Overview
- Game Title: Apollo 15.
- High-Level Concept: A lunar EVA prototype where the existing astronaut walks between scientific experiments, deploys equipment, boards the rover locally, drives, and returns safely to on-foot exploration. This milestone configures actual imported character animation and reconciles boarding with the existing deployment/input architecture.
- Players: Single locally controlled astronaut/rover; no networking.
- Inspiration / Reference Games: Apollo 15 surface exploration; no specific game reference supplied.
- Tone / Art Direction: Preserve lunar scene and supplied stylized astronaut. No mesh generation, rerigging, new sitting animation or lighting redesign.
- Target Platform: StandaloneWindows64.
- Screen Orientation / Resolution: Desktop landscape; preserve Canvas scaling. Check 1920x1080 and 1280x720 when available.
- Render Pipeline: Existing URP PC_RPAsset; Unity 6000.6.9f1. Verify New Input System remains active before edits; no package-source edits, compatibility-guard bypasses, generated scripting defines or backend changes.

# Game Mechanics
## Core Gameplay Loop
Walk or sprint with correctly transitioning animation, enter a deployment zone and deploy the Seismometer, then approach the rover and board it. Drive to another location, stop and exit onto safe ground, and resume walking. Proximity-based boarding supports spatial exploration and believable cause/effect (MDA); consistent E prompts support competence (SDT).

## Controls and Input Methods
- On foot: existing WASD/arrows/gamepad movement, Shift/stick-press sprint and mouse/right-stick camera look remain.
- E/gamepad north becomes a **single context-sensitive interaction route**: deploy the selected eligible experiment, board the nearby rover, or exit when riding. One performed input invokes at most one action; the same press must never board and immediately exit.
- When rover and deployment zone both qualify, choose the closest eligible interaction position, using serialized priority/order for stable ties. Display the selected action before E is pressed. While deploying/in dialogue, hide interaction prompts and reject boarding/exiting/control switches.
- Driving: preserve existing rover steering/throttle and brake actions. Disable astronaut movement/deployment while attached. No second drive polling loop, competing IVehicleDriver proxy or additional camera.
- 2/cycling into rover mode on foot must enforce nearby boarding; they cannot grant remote driving. 3 requests safe exit when riding, not remote teleportation. E is the primary advertised boarding/exit control.
- 1 may show LM overview while the astronaut remains riding, with rover parked; 2 resumes the existing occupied rover. Returning to on-foot mode requires safe dismount. If on foot, base overview must not create a rider or acquire rover ownership.
- Require rover speed below a serialized low threshold before boarding/exiting. If moving or no safe exit exists, park/reject the exit with clear feedback; do not place the astronaut in traffic, through terrain or inside colliders.
- Input ownership must respect focus, enable/disable, dialogue and seat lifetime; release owned actions/subscriptions without disabling unrelated UI/shared actions.

# UI
Reuse the existing ExperimentDeploymentHUD and prompt channel. Do not add a menu, another Canvas, EventSystem, font stack or separate interaction prompt system.

```text
On foot near zone:    [E] Deploy Seismometer
On foot near rover:   [E] Board Lunar Roving Vehicle
Riding, stopped:      [E] Exit Rover
Unsafe exit:          Stop rover / Exit area blocked
During deployment:   interaction prompt hidden
```
Update the existing control hint to communicate nearby boarding and safe exit. Keep prompt listener objects enabled when their visual panels are hidden. Dialogue advance stays Enter/gamepad south, never E. Objective text must follow real mission state.

# Key Asset & Context
## Verified current state
- Loaded Apollo15 scene is clean. One project-owned ExperimentAstronaut, two deployment zones, mission/dialogue systems and HUD already exist. No active RoverSeat is wired.
- `Assets/_Project/Apollo15/Prefabs/Characters/ExperimentAstronaut.prefab` is the playable character. Its child AstronautVisual uses a valid Generic Avatar, not Humanoid. Keep the original simplified rig and clips; do not convert to Humanoid or use incompatible Humanoid-only animation generation/retargeting.
- Imported clips in `Assets/Stylized_Astronaut/Character/Astronaut.fbx`: Idle, Walk, Run, Suprise, Jump_start, Jump_loop, Jump_end, Float, Flip and Animation. Idle/Walk/Run loop and have no animation events. Preview clip is excluded. This milestone uses the clips corresponding to supported locomotion; jump/flip/float controls are not requested and must not be invented.
- Existing `ExperimentAstronautLocomotion.controller` has float Speed and one Locomotion state containing IdleWalkRun. It has zero state transitions. The controller is already assigned to the project-owned prefab and scene instance; update this controller rather than create a competing asset.
- `StylizedAstronautAnimatorAdapter` implements IAstronautAnimator, currently updating validated Speed only. AstronautController/motor/ground detector already provide movement; retain these systems.
- `RoverSeat.cs` is currently unused and unsafe to attach unchanged: it polls actions in Update, creates a separate DriverProxy, ignores Rover.Board failure, switches separate camera objects and can award boarding twice. Replace its boarding orchestration with the existing selector/direct-driver architecture.
- `DirectRoverDriver.BeginDriving()` already respects Board failure and is the current rover input/ownership bridge. Keep it as the single driver adapter; boarding attaches the astronaut only after this adapter successfully acquires rover ownership.
- VehicleControlSelector owns cloned VehicleControls actions and the single VehicleFollowCamera. It currently allows remote rover selection. Refine its public methods so all on-foot-to-rover paths obey seat proximity and all riding-to-foot paths perform safe exit.
- Rover prefab currently exposes RoverViewAnchor but no seat/dismount anchor. Measure actual rover geometry before adding correctly positioned markers; do not guess transform indices or remodel source FBXs.
- ExperimentDeployer currently owns an independent E action and updates the prompt channel. Its public TryDeploy(zone) can be reused by a unified router, but the old E binding/prompt selection must be disabled in the composed scene to prevent duplicate handlers.
- `Experiment_Seismometer.asset` currently says Passive Seismic Experiment, references DeployableSeismometer.prefab and Deployment_Seismometer.asset, has parent QuestId ExperimentDeploymentDemo and ObjectiveId Deploy_Seismometer.
- The exact requested `seismometer.asset` dialogue was not found in the asset inventory. Create a project-owned dialogue with this filename by copying the existing prototype sequence; preserve its content/action semantics and do not fabricate recordings.
- Source `Seismometer.prefab` exists at `Assets/_Project/Apollo15/Prefabs/Equipment/Seismometer.prefab`. Its root scale is 100 and visible model base is offset from the root. Assign this exact prefab as requested while preserving its source geometry/materials; use generic data-configurable placement offset instead of modifying the source or hardcoding a Seismometer branch.
- User explicitly selected deploy_seismometer as the **parent quest ID**, not the objective ID. Retain separate objective ID Deploy_Seismometer. The existing demo quest has both seismic/station objectives; update the shared parent ID and both participating experiment definitions together so Station does not become invalid.
- Current Console has no warnings/errors. However the retained prior deployment test is **failed**, with movement distance 0.09216714 and a 35-second wall-time limit at stage 1. This is not proof that movement or deployment works. Diagnose actual runtime frame rate, time advancement, collision, focus/action ownership and test assumptions before claiming a fix. Do not undertake a broad rendering optimization pass to compensate for a test timeout.
- AssetDatabase dependency/reverse scans confirm Apollo15 references the player, controller, deployment definitions, quest and selector. RoverSeat has no serialized dependents; controller depends on vendor FBX and is referenced by the player prefab. Repeat impact/code-reference checks before each existing edit. A dedicated dependency query is unavailable in this planning session; use read-only AssetDatabase dependency queries as fallback.

## Animator configuration
Modify `Assets/_Project/Apollo15/Animations/ExperimentAstronautLocomotion.controller` in place:
- Parameters: **Speed (float)** and **IsRiding (bool)** only for this scope. Use validated cached hashes and correct parameter types in the adapter. No unused Jump/PickUp/Attack parameters.
- States: Idle (default), Walk, Run and RidingHidden. Use exact imported Idle/Walk/Run sub-assets; RidingHidden uses Idle only as a stable hidden pose, not a claimed sitting animation.
- Idle -> Walk: Speed > 0.1 and IsRiding false.
- Walk -> Idle: Speed < 0.1 and IsRiding false.
- Walk -> Run: Speed > 2.8 and IsRiding false.
- Run -> Walk: Speed < 2.4 and IsRiding false. Include Run -> Idle below 0.1 to handle hard stops/pause without remaining in a run loop.
- Any State -> RidingHidden: IsRiding true, no self-transition. RidingHidden -> Idle: IsRiding false; reset Speed to zero on boarding/dismount transitions.
- Locomotion transitions have no exit time, short approximately 0.1–0.15 second crossfades and no unintended self loops. Use hysteresis between walking/running to prevent flicker. Revalidate thresholds against actual configured walk/sprint speeds.
- Preserve imported clips, Generic Avatar and source controller. Assign updated project-owned controller to both prefab Animator and current scene instance; verify no instance override points at the vendor controller.
- Store/restore the original renderer.enabled values for astronaut visual renderers when boarding/exiting. Do not deactivate the entire actor, Animator or rider lifecycle component. No visible seated pose is claimed.

## Assets to modify or create
Existing:
- `Core/Player/Components/StylizedAstronautAnimatorAdapter.cs`: validated IsRiding support, explicit public SetRiding(bool), Speed reset on mode change; preserve IAstronautAnimator contract.
- `Core/Vehicles/Components/RoverSeat.cs`: explicit selector/actor/anchor wiring, proximity/speed checks, boarding transaction, rider attachment/renderer state, safe dismount and teardown. Preserve useful IInteractable/public dismount entry points; remove second input/drive/camera/proxy behavior.
- `Core/Vehicles/Components/VehicleControlSelector.cs`: seat registration; internal guarded acquisition/release methods used by seat; delegate public mode transitions through seat without recursion; retain one existing input/camera owner.
- `Core/Experiments/Components/ExperimentDeployer.cs`: external-interaction/prompt mode for the router; public eligible-zone access/selection API; preserve standalone default behavior and public TryDeploy. Ensure destroyed/disabled reserved zone releases animation/locks safely even when Unity's object-null comparison hides the destroyed reference.
- `Core/Experiments/Data/ExperimentDefinitionSO.cs`: optional generic serialized SpawnLocalOffset (default zero), read-only property/validation, applied through spawn-anchor transform. Measure seismic visible base and author offset to place exact source prefab on the surface; no experiment-name branches.
- `Apollo15/Prefabs/Characters/ExperimentAstronaut.prefab`: add router/configuration, retain controller/Generic Avatar; scene services remain scene references.
- `Apollo15/Prefabs/Vehicles/Rover.prefab`: seat trigger/interactable and measured mount plus left/right dismount anchors. Keep physics, wheels, config, source meshes/materials and scale unchanged.
- `Apollo15/Data/Experiments/Experiment_Seismometer.asset`: Experiment Name **Seismometer**, Prefab exact existing **Seismometer.prefab**, Dialogue new/existing validated **seismometer.asset**, Quest ID **deploy_seismometer**, Objective ID **Deploy_Seismometer**, measured spawn offset.
- `Apollo15/Data/Missions/Quest_ExperimentDeploymentDemo.asset` and `Data/Experiments/Experiment_Station.asset`: shared parent QuestId becomes deploy_seismometer; preserve objective list, progression behavior and unrelated content.
- `Apollo15/Prefabs/UI/ExperimentDeploymentHUD.prefab`: control hint/context feedback only; retain layout/font/channel architecture.
- `Assets/Scenes/Apollo15.unity`: explicit seat/selector/router/actor references, measured safe anchors and updated seismometer configuration; no duplicate player or camera.
- `Core/Experiments/README.md`: updated nearby boarding controls, parent/objective ID distinction, exact content mapping and spawn-offset authoring.
New:
- `Assets/_Project/Core/Interaction/Components/AstronautInteractionRouter.cs`: single event-driven E handler and prompt owner for the composed scene, explicit experiment/seat/actor references and deterministic selected-target arbitration. Not a global scene search or replacement for all project interactions.
- `Assets/_Project/Apollo15/Data/Dialogues/seismometer.asset`: copy the existing prototype dialogue if exact requested path is still absent. If multiple filename matches appear before implementation, ask which asset rather than guess. Check type is DialogueSequenceSO; do not cast an unrelated SO.
- Temporary scoped editor/bootstrap validation files only as needed; preserve existing test harnesses and use distinct names/session keys. Remove only test artifacts created during this milestone after results are retrieved. Do not overwrite `Assets/Editor/PlayModeTestRunner.cs` or the interrupted `ExperimentDeploymentTestRunner.cs` without first inspecting their purpose/state.

## Boarding and safe-exit transaction
1. Single input owner chooses one valid on-foot target. Boarding requires enabled actor/seat/rover, appropriate distance, stationary rover, no deployment/dialogue lock and no conflicting occupant.
2. Save actor parent, world transform, collider-enabled state, renderer-enabled states and control mode. Acquire rover via existing DirectRoverDriver/selector before disabling or attaching the actor. Failed acquisition leaves all state unchanged and produces no success/quest event.
3. Pause locomotion/deployment, disable CharacterController, attach actor at measured mount anchor with scale-safe parenting, set IsRiding and hide only previously enabled character renderers. Select existing rover camera target and enable the sole rover drive map. Commit ride state atomically; rollback/release on failure.
4. E on later performed input requests exit; debounce per performed action/run so the boarding press cannot exit immediately. Never depend on WasPressedThisFrame polling in a second component.
5. Safe exit checks multiple explicit candidate anchors on current terrain, downward raycast for walkable support/slope, sufficient headroom and capsule clearance including vehicle and experiment colliders. Use actual CharacterController geometry, not a guessed radius. If blocked, stay attached/parked and show feedback; never teleport into an invalid fallback.
6. On successful exit, disable drive/release ownership and park, restore original parent, move actor while its collider remains disabled, reset motor velocity without temporarily re-enabling the collider during validation, restore collider/renderers, clear IsRiding and restore owned on-foot mode/input/camera once.
7. Focus loss parks but does not auto-eject. Seat/vehicle/actor disable or destruction releases ownership and avoids orphaned hidden actors. Use a validated last-safe location only as an emergency lifecycle fallback, preserving scene ownership; do not silently mark an unsafe ordinary exit as successful. Exercise teardown/reentrancy explicitly.
8. No quest progress from boarding unless a separately configured existing boarding objective exists. This milestone does not invent mission rewards.

# Implementation Steps
1. **Revalidate and capture baseline.** Inspect current scene/prefab overrides, imported clips/Avatar, exact dialogue paths, effective input backend/actions, controller graph, rover geometry and seat availability. Gather dependency/reverse/code references for existing edits, scene dirty state and prior test/session state. Dependencies: approval of this revision.
2. **Configure imported-clip Animator.** Update the existing project-owned controller with parameters/states/transitions above; extend adapter and assign controller to prefab/scene. Validate Generic playback, transitions and no parameter/event warnings before boarding gameplay changes. Dependencies: Step 1.
3. **Reconcile Seismometer content.** Resolve/copy seismometer.asset, set exact requested name/prefab/dialogue/parent ID, preserve separate ObjectiveId, update the active quest and Station parent consistently, add generic measured spawn offset and apply it in the existing spawn path. Revalidate clearance/ground placement for the raw prefab. Dependencies: Step 1 and successful schema compilation.
4. **Implement boarding with existing ownership.** Refine RoverSeat and selector together; add measured mount/dismount anchors to rover prefab. Enforce Board rejection/transaction rollback, speed threshold, renderer hiding/restoration, collider lifecycle and safe candidate exits. Do not enable the old polling/proxy/two-camera seat code. Dependencies: Steps 1–2.
5. **Unify interaction input and prompts.** Add router with explicit zone/seat selection, single E subscription and context prompt. Disable deployer's local E/prompt ownership only in composed configuration, preserve generic standalone usage, and add necessary reserved-zone teardown guards. Resolve all 1/2/3/cycle paths through ride state/valid exit. Dependencies: Steps 3–4.
6. **Wire existing scene and documentation.** Update canonical astronaut/rover references and HUD hint in place; preserve one camera/listener/EventSystem, scene terrain/art and unrelated objects. Document hidden-rider limitation, mappings and controls. Save only intended assets. Dependencies: Steps 2–5.
7. **Diagnose and verify runtime.** First measure actual movement, game-time/frame advancement, action state/focus and collision versus prior test's failed values. Fix only evidence-backed integration/test defects. Then test animation, boarding/driving/exit and complete deployment ordering with real input/physics/Animator; visually inspect fresh Scene/Game View captures. Dependencies: Step 6.

# Verification & Testing
- Zero new compilation errors, missing scripts/parameters or stale input subscribers. Controller assigned to actual active astronaut and saved prefab; vendor rig/FBX/controller untouched.
- Animator: play imported Idle/Walk/Run on Generic skeleton; inspect Speed-driven transitions at stop/walk/sprint and hysteresis boundaries. RidingHidden enters/exits via IsRiding; hidden mesh is restored, not replaced with a fabricated sitting animation. No unused jump mechanic.
- Input arbitration: near rover E boards exactly once; same E press cannot exit or deploy. At overlapping seat/zone eligibility, displayed selected target matches execution. E outside eligible targets does nothing. E/3 while riding invokes safe exit; remote 2/cycle cannot acquire driving from a distant on-foot actor.
- Boarding rejection: occupied/disabled/moving rover, distant actor and active deployment/dialogue leave actor transform/renderer/collider/input/camera untouched. No duplicate or unearned mission event.
- Riding: exactly one rover driver and drive map; CharacterController disabled, actor attached with correct scale/anchor, mesh hidden as selected. Vehicle controls/brakes work and physical actor follows vehicle. Exactly one camera/AudioListener.
- Safe exit: stopped rover on flat ground and slope; obstruct left exit then use right; block both exits and deny safely; no ground/headroom denies. Restore collider only after safe relocation, restore renderer state and input once; no model/terrain penetration or teleport inside wheel/chassis geometry.
- Lifecycle: focus loss parks; repeated board/exit, disabled seat/vehicle/actor, destruction, scene unload and Play Mode repeats do not orphan a hidden rider or leave a rover occupied. Emergency fallback is validated and ordinary exits never use unsafe arbitrary offsets.
- Experiment: assert name Seismometer, exact original Seismometer.prefab reference, DialogueSequenceSO at seismometer.asset, parent QuestId deploy_seismometer and separate ObjectiveId Deploy_Seismometer. Quest and both experiment parent IDs match. Spawn aligns visible prefab base to terrain despite source scale/pivot; no source prefab alteration.
- Deployment regression: animation-start -> actual completion -> one spawn -> dialogue request accepted -> exact objective completed; E spam/cancellation/seat interaction cannot duplicate or prematurely award it. Preserve data-only extensibility and Station progression.
- Prior failed test: log actual position change, elapsed game time and wall time independently; distinguish test timing/focus issues from gameplay collision/input bugs. A timeout or movement 0.09216714 is not a success. Keep previous results intact until new reliable evidence is collected.
- UI: prompt/control hint readable and not contradictory while hidden/riding/deploying; no flicker from dual prompt publishers, no extra Canvas/fonts. Enter remains dialogue advance.
- Save/reopen Apollo15 and verify linked assets/overrides persist; repeated Play Mode does not mutate definitions or leave duplicate players/seats/cameras. Test domain reload enabled and, if supported safely by the harness, disabled; report unchecked cases.
- Visual validation: fresh multi-angle captures of astronaut/rover and runtime Game View for on-foot, hidden-rider driving and restored safe exit. At most three visual capture/fix rounds. Avoid general terrain/art/performance redesign.
- Windows build/run only if compatible toolchain is available without permanent scene-list changes; otherwise report unperformed. Preserve vendor assets, rendering/input project settings, packages, unrelated scripts and pre-existing test artifacts.

