using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

[InitializeOnLoad]
internal static class PlayModeTestRunner
{
    private static Component _selector, _driver, _rover, _lm;
    private static Rigidbody _body;
    private static Keyboard _keyboard;
    private static Vector3 _start;
    private static float _stageTime, _speed;
    private static int _stage, _wait;
    private static double _wallStart;
    private static readonly List<string> Logs = new List<string>();
    private static readonly List<string> Errors = new List<string>();
    private const string RoverShot = "Assets/LunarRoverVerification.png";
    private const string BaseShot = "Assets/LunarBaseVerification.png";
    static PlayModeTestRunner()
    {
        var state = SessionState.GetString("PlayModeTest.State", "Idle");
        if (state == "WaitingForCompile") EditorApplication.delayCall += () =>
        {
            SessionState.SetString("PlayModeTest.State", "EnteringPlayMode");
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
        SessionState.SetString("PlayModeTest.State", "InPlayMode");
        EditorApplication.update += Tick;
    }
    private static object Property(Component c, string name) => c.GetType().GetProperty(name).GetValue(c);
    private static object Call(Component c, string method) => c.GetType().GetMethod(method).Invoke(c, null);
    private static void Check(bool condition, string message)
    {
        Logs.Add(message + "=" + condition); Debug.Log("[Test] " + message + "=" + condition);
        if (!condition) Errors.Add(message);
    }
    private static void Keys(params Key[] keys)
    {
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys));
        InputSystem.Update();
    }
    private static void Setup()
    {
        var components = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Component>(true)).Where(c => c != null).ToArray();
        _selector = components.Single(c => c.GetType().Name == "VehicleControlSelector");
        _driver = components.Single(c => c.GetType().Name == "DirectRoverDriver");
        _rover = components.Single(c => c.GetType().Name == "RoverController");
        _lm = components.Single(c => c.GetType().Name == "LunarModuleController");
        _body = _rover.GetComponent<Rigidbody>();
        _keyboard = InputSystem.AddDevice<Keyboard>();
        _wallStart = EditorApplication.timeSinceStartup; _stageTime = Time.time;
        Check((bool)Property(_selector, "RoverSelected") && (bool)Property(_driver, "IsDriving"), "Initial rover control");
        Check(components.OfType<Camera>().Count(c => c.enabled) == 1 && components.OfType<AudioListener>().Count(c => c.enabled) == 1, "Single camera and listener");
        Check(components.OfType<MeshCollider>().Count() == 49, "49 collision tiles");
        Application.logMessageReceived += CaptureLog;
    }
    private static void CaptureLog(string text, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception) Errors.Add(text);
    }
    private static void Tick()
    {
        try
        {
            if (!EditorApplication.isPlaying) { Finish("Play Mode stopped before completion"); return; }
            if (_selector == null) { if (++_wait < 8) return; Setup(); return; }
            if (EditorApplication.timeSinceStartup - _wallStart > 40) { Finish("Runtime test exceeded 40s wall limit"); return; }
            float elapsed = Time.time - _stageTime;
            switch (_stage)
            {
                case 0:
                    if (elapsed < 3.5f) return;
                    foreach (var wheel in _rover.GetComponentsInChildren<WheelCollider>())
                    {
                        string diagnostic = wheel.name + " grounded=" + wheel.isGrounded + " position=" + wheel.transform.position + " sprungMass=" + wheel.sprungMass + " body=" + _body.position + " rotation=" + _body.rotation.eulerAngles;
                        Logs.Add(diagnostic); Debug.Log("[Test] " + diagnostic);
                    }
                    int grounded = _rover.GetComponentsInChildren<WheelCollider>().Count(w => w.isGrounded);
                    Check(grounded == 4, "All wheels grounded: " + grounded);
                    _start = _body.position; Keys(Key.W); _stage++; _stageTime = Time.time; break;
                case 1:
                    if (elapsed < 6f) return;
                    _speed = _body.linearVelocity.magnitude;
                    Check(_body.position.z > -10f && _body.position.y > -0.6f, "Drove across terrain seam z=" + _body.position.z + " y=" + _body.position.y);
                    Check(_speed > 0.3f && _speed < 5f, "Forward speed " + _speed);
                    ScreenCapture.CaptureScreenshot(RoverShot);
                    SessionState.SetString("PlayModeTest.RoverScreenshotPath", RoverShot);
                    Keys(Key.Space); _stage++; _stageTime = Time.time; break;
                case 2:
                    if (elapsed < 3.5f) return;
                    Check(_body.linearVelocity.magnitude < 0.5f, "Brake speed " + _body.linearVelocity.magnitude);
                    Keys(Key.S); _stage++; _stageTime = Time.time; break;
                case 3:
                    if (elapsed < 3f) return;
                    Check((float)Property(_rover, "CurrentSpeedMs") < -0.2f, "Reverse speed " + Property(_rover, "CurrentSpeedMs"));
                    Keys(Key.W, Key.D); _stage++; _stageTime = Time.time; break;
                case 4:
                    if (elapsed < 1f) return;
                    Check(_rover.GetComponentsInChildren<WheelCollider>().Any(w => Mathf.Abs(w.steerAngle) > 5f), "Steering input");
                    Keys(Key.Digit1); _stage++; _stageTime = Time.time; break;
                case 5:
                    if (elapsed < 1f) return;
                    Keys();
                    Check(!(bool)Property(_selector, "RoverSelected") && !(bool)Property(_driver, "IsDriving"), "Base selection parks rover");
                    Check(_body.linearVelocity.magnitude < 0.5f, "Parked speed " + _body.linearVelocity.magnitude);
                    Keys(Key.E); _stage++; _stageTime = Time.time; break;
                case 6:
                    if (elapsed < 0.2f) return;
                    Keys();
                    Check((int)Property(_lm, "InspectionCount") == 1, "LM inspection count " + Property(_lm, "InspectionCount"));
                    Check(Mathf.Abs(_lm.transform.position.y) < 0.01f, "Stationary LM on junction apron");
                    ScreenCapture.CaptureScreenshot(BaseShot);
                    SessionState.SetString("PlayModeTest.ScreenshotPath", BaseShot);
                    Keys(Key.Digit2); _stage++; _stageTime = Time.time; break;
                case 7:
                    if (elapsed < 0.3f) return;
                    Keys();
                    Check((bool)Property(_driver, "IsDriving"), "Return to rover");
                    ((Behaviour)_selector).enabled = false;
                    Check(!(bool)Property(_driver, "IsDriving"), "Disable clears ownership");
                    ((Behaviour)_selector).enabled = true;
                    _stage++; _stageTime = Time.time; break;
                case 8:
                    if (elapsed < 0.3f) return;
                    Check((bool)Property(_driver, "IsDriving"), "Re-enable restores selected rover");
                    Check(_body.linearVelocity.magnitude < 0.5f, "No stale throttle after re-enable");
                    Finish(null); break;
            }
        }
        catch (System.Exception exception) { Finish(exception.ToString()); }
    }
    [System.Serializable] private class Result { public bool success; public string error; public string[] logs; }
    private static void Finish(string error)
    {
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= CaptureLog;
        if (error != null) Errors.Add(error);
        if (_keyboard != null) InputSystem.RemoveDevice(_keyboard);
        SessionState.SetString("PlayModeTest.Result", JsonUtility.ToJson(new Result { success = Errors.Count == 0, error = string.Join("; ", Errors), logs = Logs.ToArray() }));
        SessionState.SetString("PlayModeTest.State", "Done");
        EditorApplication.isPlaying = false;
    }
}
