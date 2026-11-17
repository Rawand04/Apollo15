using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using System.Linq;
using System.Collections.Generic;

[InitializeOnLoad]
internal static class AstronautBoardingVerification
{
    private const string Prefix = "AstronautBoardVerify.";
    private static Component _actor, _motor, _input, _deployer, _router, _seat, _selector, _mission, _runner, _zone;
    private static Animator _animator, _placement;
    private static Keyboard _keyboard;
    private static int _wait, _phase, _finishFrames;
    private static float _startGame;
    private static double _startWall;
    private static bool _oldBackground;
    private static readonly List<string> Logs = new List<string>();
    private static readonly List<string> Errors = new List<string>();
    static AstronautBoardingVerification()
    {
        string state = SessionState.GetString(Prefix + "State", "Idle");
        if (state == "WaitingForCompile") EditorApplication.delayCall += () =>
        {
            SessionState.SetString(Prefix + "State", "EnteringPlayMode");
            EditorApplication.playModeStateChanged += StateChanged;
            EditorApplication.isPlaying = true;
        };
        else if (state == "EnteringPlayMode") EditorApplication.playModeStateChanged += StateChanged;
        else if (state == "InPlayMode" && EditorApplication.isPlaying) EditorApplication.update += Tick;
    }
    private static void StateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        EditorApplication.playModeStateChanged -= StateChanged;
        SessionState.SetString(Prefix + "State", "InPlayMode");
        EditorApplication.update += Tick;
    }
    private static object Prop(object c, string p) => c.GetType().GetProperty(p).GetValue(c);
    private static object Call(object c, string method, params object[] args) => c.GetType().GetMethod(method).Invoke(c, args);
    private static void Check(bool condition, string text)
    { Logs.Add(text + "=" + condition); Debug.Log("[Test] " + text + "=" + condition); if (!condition) Errors.Add(text); }
    private static void Keys(params Key[] keys) { InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys)); InputSystem.Update(); }
    private static Vector3 Ground(Vector3 p)
    {
        var hits = Physics.RaycastAll(p + Vector3.up * 30, Vector3.down, 60, ~0, QueryTriggerInteraction.Ignore);
        foreach (var hit in hits.OrderBy(h => h.distance)) if (!hit.transform.IsChildOf(_actor.transform)) return hit.point + Vector3.up * .08f;
        throw new System.Exception("No ground at " + p);
    }
    private static void Teleport(Vector3 p) { Call(_actor, "Teleport", Ground(p), Quaternion.identity); Physics.SyncTransforms(); }
    private static void Setup()
    {
        _oldBackground = Application.runInBackground; Application.runInBackground = true;
        var all = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Component>(true)).Where(c => c != null).ToArray();
        _actor = all.Single(c => c.GetType().Name == "AstronautController"); _motor = all.Single(c => c.GetType().Name == "AstronautMotor");
        _input = all.Single(c => c.GetType().Name == "ExperimentPlayerInputProvider"); _deployer = all.Single(c => c.GetType().Name == "ExperimentDeployer");
        _router = all.Single(c => c.GetType().Name == "AstronautInteractionRouter"); _seat = all.Single(c => c.GetType().Name == "RoverSeat");
        _selector = all.Single(c => c.GetType().Name == "VehicleControlSelector"); _mission = all.Single(c => c.GetType().Name == "MissionManager");
        _runner = all.Single(c => c.GetType().Name == "DialogueRunner"); _zone = all.Single(c => c.GetType().Name == "ExperimentDeploymentZone" && c.name.Contains("Seismometer"));
        _animator = _actor.GetComponentInChildren<Animator>(true); _placement = all.OfType<Animator>().Single(a => a.name == "EquipmentPlacementRig");
        _keyboard = InputSystem.AddDevice<Keyboard>();
        _startGame = Time.time; _startWall = EditorApplication.timeSinceStartup;
        Application.logMessageReceived += Capture;
        Check((bool)Prop(_selector, "OnFootSelected"), "Starts on foot");
        Check(all.OfType<Camera>().Count(c => c.enabled && c.gameObject.activeInHierarchy) == 1 && all.OfType<AudioListener>().Count(c => c.enabled && c.gameObject.activeInHierarchy) == 1, "Single camera/listener");
    }
    private static void Capture(string text, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception) Errors.Add(text); }
    private static void Contracts()
    {
        Check(_animator.avatar != null && _animator.avatar.isValid && !_animator.avatar.isHuman, "Supplied Generic avatar preserved");
        var definition = Prop(_zone, "Definition");
        Check((string)Prop(definition, "ExperimentName") == "Seismometer" && (string)Prop(definition, "QuestId") == "deploy_seismometer", "Requested name and parent quest");
        Check(AssetDatabase.GetAssetPath((Object)Prop(definition, "Prefab")).EndsWith("/Seismometer.prefab") && AssetDatabase.GetAssetPath((Object)Prop(definition, "Dialogue")).EndsWith("/seismometer.asset"), "Exact prefab/dialogue references");
        Teleport(new Vector3(10, 0, -21));
        Check(!(bool)Call(_selector, "SelectRover"), "Remote boarding rejected");
        Keys(Key.W); Check(((Vector2)Prop(_input, "MoveInput")).y > .9f, "Callback input receives W");
        var start = _actor.transform.position;
        for (int i = 0; i < 60; i++) Call(_motor, "Tick", Vector2.up, false, 1f / 60f);
        float distance = Vector3.Distance(start, _actor.transform.position);
        Check(distance > .1f, "Controlled motor movement distance " + distance); Keys();
        _animator.Play("Base Layer.Idle", 0, 0); _animator.SetFloat("Speed", 1.8f); _animator.Update(0); _animator.Update(.2f);
        Logs.Add("Walk diagnostic initialized=" + _animator.isInitialized + " transition=" + _animator.IsInTransition(0) + " nextWalk=" + _animator.GetNextAnimatorStateInfo(0).IsName("Walk") + " clips=" + string.Join(",", _animator.GetCurrentAnimatorClipInfo(0).Select(c => c.clip.name)));
        Check(_animator.GetCurrentAnimatorStateInfo(0).IsName("Walk"), "Idle to imported Walk transition");
        _animator.SetFloat("Speed", 3.8f); _animator.Update(0); _animator.Update(.2f);
        Logs.Add("Run diagnostic transition=" + _animator.IsInTransition(0) + " nextRun=" + _animator.GetNextAnimatorStateInfo(0).IsName("Run") + " clips=" + string.Join(",", _animator.GetCurrentAnimatorClipInfo(0).Select(c => c.clip.name)));
        Check(_animator.GetCurrentAnimatorStateInfo(0).IsName("Run"), "Walk to imported Run transition");
_animator.SetFloat("Speed", 0); _animator.Update(0); _animator.Update(.2f);
        Check(_animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), "Run to Idle transition");
        var rover = _seat.GetComponentInParent<Rigidbody>(); rover.linearVelocity = Vector3.zero; rover.angularVelocity = Vector3.zero;
        Teleport(_seat.transform.parent.TransformPoint(new Vector3(-1.7f, .5f, .3f)));
        Keys(Key.E); Keys();
        bool riding = (bool)Prop(_seat, "IsRiding");
        Check(riding, "Real E boards once; failure=" + Prop(_seat, "LastFailure"));
        if (riding)
        {
            Check(!_actor.GetComponent<CharacterController>().enabled && _actor.GetComponentsInChildren<Renderer>(true).All(r => !r.enabled), "Rider collider off and mesh hidden");
            Check((bool)Prop(_selector, "RoverSelected"), "Existing rover control acquired");
            _animator.Update(0); _animator.Update(.2f);
            Check(_animator.GetCurrentAnimatorStateInfo(0).IsName("RidingHidden"), "IsRiding enters hidden state");
            Check((bool)Call(_seat, "TryDismount"), "Stopped rover safe exit; failure=" + Prop(_seat, "LastFailure"));
            Check(_actor.GetComponent<CharacterController>().enabled && _actor.GetComponentsInChildren<Renderer>(true).Any(r => r.enabled) && (bool)Prop(_selector, "OnFootSelected"), "Exit restores mesh collider and on-foot ownership");
        }
        Teleport(_zone.transform.position + Vector3.back * 1.5f);
        bool began = (bool)Call(_deployer, "TryDeploy", _zone);
        Check(began, "Seismometer deployment starts; failure=" + Prop(_deployer, "LastFailure"));
        if (began)
        {
            _placement.Update(0); _placement.Update(.8f);
            var quest = Prop(_mission, "ActiveQuest"); var objective = Call(quest, "FindObjectiveState", "Deploy_Seismometer");
            Check(Prop(_zone, "SpawnedInstance") == null && !(bool)Prop(objective, "IsCompleted"), "No spawn/reward before real clip completion");
            Check(!(bool)Call(_deployer, "TryDeploy", _zone), "Duplicate deployment rejected");
            _placement.Update(1.1f);
            Check(Prop(_zone, "SpawnedInstance") != null && (bool)Prop(objective, "IsCompleted") && (bool)Prop(_runner, "IsDialogueActive"), "Actual clip event spawns then dialogue/objective");
            Check(!(bool)Prop(Call(quest, "FindObjectiveState", "Deploy_Station"), "IsCompleted"), "Station objective remains independent");
        }
        ScreenCapture.CaptureScreenshot("Assets/AstronautBoardingVerification.png");
        SessionState.SetString(Prefix + "ScreenshotPath", "Assets/AstronautBoardingVerification.png");
    }
    private static void Tick()
    {
        try
        {
            if (_actor == null)
            {
                if (_startWall == 0) _startWall = EditorApplication.timeSinceStartup;
                if (EditorApplication.timeSinceStartup - _startWall > 10) { Finish("No five gameplay frames reached during bootstrap initialization."); return; }
                if (Time.frameCount < 5) return;
                Setup(); return;
            }
            if (_phase == 0)
            {
if (EditorApplication.timeSinceStartup - _startWall < .75) return;
                Logs.Add("Natural runtime diagnostic: gameTime=" + (Time.time - _startGame) + " wallTime=" + (EditorApplication.timeSinceStartup - _startWall) + " focused=" + Application.isFocused + " paused=" + EditorApplication.isPaused);
                Contracts(); _phase = 1; return;
            }
            if (++_finishFrames >= 2) Finish(null);
        }
        catch (System.Exception ex) { Finish(ex.ToString()); }
    }
    [System.Serializable] private class Result { public bool success; public string error; public string[] logs; }
    private static void Finish(string error)
    {
        EditorApplication.update -= Tick; Application.logMessageReceived -= Capture;
        if (error != null) Errors.Add(error); if (_keyboard != null) InputSystem.RemoveDevice(_keyboard);
        Application.runInBackground = _oldBackground;
        SessionState.SetString(Prefix + "Result", JsonUtility.ToJson(new Result { success = Errors.Count == 0, error = string.Join("; ", Errors), logs = Logs.ToArray() }));
        SessionState.SetString(Prefix + "State", "Done"); EditorApplication.isPlaying = false;
    }
}
