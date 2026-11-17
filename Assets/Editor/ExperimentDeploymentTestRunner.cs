using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using System.Collections.Generic;
using System.Linq;

[InitializeOnLoad]
internal static class ExperimentDeploymentTestRunner
{
    private const string Prefix = "ExperimentTest.";
    private static Component _selector, _actor, _deployer, _runner, _mission, _station, _seismic;
    private static Keyboard _keyboard;
    private static int _wait, _stage;
    private static float _time;
    private static double _wallStart;
    private static Vector3 _start;
    private static readonly List<string> Logs = new List<string>();
    private static readonly List<string> Errors = new List<string>();
    static ExperimentDeploymentTestRunner()
    {
        var state = SessionState.GetString(Prefix + "State", "Idle");
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
    private static object Property(object c, string name) => c.GetType().GetProperty(name).GetValue(c);
    private static object Call(object c, string method, params object[] args) => c.GetType().GetMethod(method).Invoke(c, args);
    private static void Check(bool condition, string message)
    { Logs.Add(message + "=" + condition); Debug.Log("[Test] " + message + "=" + condition); if (!condition) Errors.Add(message); }
    private static void Keys(params Key[] keys) { InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys)); }
    private static Vector3 Ground(Vector3 p)
    { Physics.SyncTransforms(); if (!Physics.Raycast(p + Vector3.up * 100, Vector3.down, out var hit, 200, ~0, QueryTriggerInteraction.Ignore)) throw new System.Exception("No ground"); return hit.point + Vector3.up * .08f; }
    private static void Teleport(Vector3 p) { Call(_actor, "Teleport", Ground(p), Quaternion.identity); }
    private static bool Completed(string id)
    { var q = Property(_mission,"ActiveQuest"); return (bool)Property(Call(q,"FindObjectiveState",id),"IsCompleted"); }
    private static void Next() { _stage++; _time=Time.time; }
    private static void Setup()
    {
        var c=UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Component>(true)).Where(x=>x!=null).ToArray();
        _selector=c.Single(x=>x.GetType().Name=="VehicleControlSelector");_actor=c.Single(x=>x.GetType().Name=="AstronautController");_deployer=c.Single(x=>x.GetType().Name=="ExperimentDeployer");_runner=c.Single(x=>x.GetType().Name=="DialogueRunner");_mission=c.Single(x=>x.GetType().Name=="MissionManager");_station=c.Single(x=>x.GetType().Name=="ExperimentDeploymentZone"&&x.name.Contains("Station"));_seismic=c.Single(x=>x.GetType().Name=="ExperimentDeploymentZone"&&x.name.Contains("Seismometer"));
        _keyboard=InputSystem.AddDevice<Keyboard>();_wallStart=EditorApplication.timeSinceStartup;_time=Time.time;
        Application.logMessageReceived+=CaptureLog;
        Check((bool)Property(_selector,"OnFootSelected"),"Starts on foot");
        Check(c.OfType<Camera>().Count(x=>x.enabled&&x.gameObject.activeInHierarchy)==1 && c.OfType<AudioListener>().Count(x=>x.enabled&&x.gameObject.activeInHierarchy)==1,"One camera/listener");
        Teleport(new Vector3(10,0,-21));Check(!(bool)Call(_deployer,"TryDeploy",_station),"Outside zone rejected");_start=_actor.transform.position;Keys(Key.W);
    }
    private static void CaptureLog(string text,string stack,LogType type) { if(type==LogType.Error||type==LogType.Exception)Errors.Add(text); }
    private static void Tick()
    {
        try
        {
            if(!EditorApplication.isPlaying){Finish("Unexpected Play Mode exit");return;}
            if(_selector==null){if(++_wait<10)return;Setup();return;}
            if(EditorApplication.timeSinceStartup-_wallStart>35){Finish("35s wall limit at stage "+_stage);return;}
            float e=Time.time-_time;
            switch(_stage)
            {
                case 0:
                    if(e<.7f)return;Keys();Check(Vector3.Distance(_start,_actor.transform.position)>.15f,"W movement distance "+Vector3.Distance(_start,_actor.transform.position));
                    Teleport(_station.transform.position+Vector3.back*1.5f);Keys(Key.E);Next();break;
                case 1:
                    if(e<.25f)return;Keys();Check((bool)Property(_deployer,"IsDeploying"),"E starts real animation");Check(Property(_station,"SpawnedInstance")==null && !Completed("Deploy_Station"),"No premature spawn/reward");Check(!(bool)Call(_deployer,"TryDeploy",_station),"Spam rejected");Check(!(bool)Call(_selector,"SelectRover"),"Switch locked during animation");Next();break;
                case 2:
                    if(Property(_station,"State").ToString()!="Deployed"){if(e>5)throw new System.Exception("Station did not deploy: "+Property(_deployer,"LastFailure"));return;}
                    Check(Property(_station,"SpawnedInstance")!=null,"Animation completion spawned station");Check((bool)Property(_runner,"IsDialogueActive") && Completed("Deploy_Station"),"Spawn then dialogue and exact objective");
                    Check(!Completed("Deploy_Seismometer"),"Unrelated objective not completed");ScreenCapture.CaptureScreenshot("Assets/ExperimentDeploymentVerification.png");SessionState.SetString(Prefix+"ScreenshotPath","Assets/ExperimentDeploymentVerification.png");Next();break;
                case 3:
                    if(e<.3f)return;Call(_runner,"SkipTyping");Call(_runner,"Advance");Next();break;
                case 4:
                    if(e<.2f)return;Check((bool)Property(_actor,"IsLocomotionActive"),"Movement restored after dialogue");Teleport(_seismic.transform.position+Vector3.back*1.5f);
                    Check((bool)Call(_deployer,"TryDeploy",_seismic),"Second data-only experiment accepted");Call(_deployer,"CancelDeployment");Check(Property(_seismic,"State").ToString()=="Available"&&Property(_seismic,"SpawnedInstance")==null&&!Completed("Deploy_Seismometer"),"Cancel releases without spawn/reward");Next();break;
                case 5:
                    if(e<.2f)return;Keys(Key.E);Next();break;
                case 6:
                    if(e<.2f)return;Keys();if(Property(_seismic,"State").ToString()!="Deployed"){if(e>5)throw new System.Exception("Seismic did not deploy: "+Property(_deployer,"LastFailure"));return;}
                    Check(Completed("Deploy_Seismometer")&&(bool)Property(Property(_mission,"ActiveQuest"),"IsCompleted"),"Two experiments complete open checklist");Check(!(bool)Call(_deployer,"TryDeploy",_seismic),"Committed zone cannot repeat");Call(_runner,"SkipTyping");Call(_runner,"Advance");Next();break;
                case 7:
                    if(e<.2f)return;Check((bool)Call(_selector,"SelectRover"),"Rover handoff after deployment");Call(_selector,"SelectBase");Check(!(bool)Property(_selector,"OnFootSelected")&&!(bool)Property(_selector,"RoverSelected"),"Base handoff");Call(_selector,"SelectOnFoot");Check((bool)Property(_selector,"OnFootSelected")&&(bool)Property(_actor,"IsLocomotionActive"),"Return to on-foot control");Finish(null);break;
            }
        }
        catch(System.Exception ex){Finish(ex.ToString());}
    }
    [System.Serializable]private class Result{public bool success;public string error;public string[] logs;}
    private static void Finish(string error)
    {
        EditorApplication.update-=Tick;Application.logMessageReceived-=CaptureLog;if(error!=null)Errors.Add(error);
        if(_keyboard!=null)InputSystem.RemoveDevice(_keyboard);
        SessionState.SetString(Prefix+"Result",JsonUtility.ToJson(new Result{success=Errors.Count==0,error=string.Join("; ",Errors),logs=Logs.ToArray()}));SessionState.SetString(Prefix+"State","Done");EditorApplication.isPlaying=false;
    }
}
