using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using SpaceSeries.Core.Environment;
using SpaceSeries.Core.Vehicles;
using SpaceSeries.Core.Cameras;

namespace SpaceSeries.Apollo15.Editor
{
    public static class Apollo15SurfaceComposer
    {
        [MenuItem("Apollo15/Setup/Compose Lunar Surface and Vehicles")]
        public static void Compose()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/Apollo15.unity") throw new System.InvalidOperationException("Open Apollo15 before composing.");
            var roots = scene.GetRootGameObjects();
            var environment = roots.Single(r => r.name == "Environment").transform;
            var gameplay = roots.Single(r => r.name == "Gameplay").transform;
            var systems = roots.Single(r => r.name == "Systems").transform;
            var camera = roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)).Single(c => c.CompareTag("MainCamera"));
            var rover = roots.SelectMany(r => r.GetComponentsInChildren<RoverController>(true)).Single();
            var lm = roots.SelectMany(r => r.GetComponentsInChildren<LunarModuleController>(true)).Single();
            if (environment.Cast<Transform>().Any(t => t.name == "MoonSurface"))
                throw new System.InvalidOperationException("Surface already composed. Select MoonSurface and use the terrain regeneration menu instead.");
            var surface = new GameObject("MoonSurface");
            Undo.RegisterCreatedObjectUndo(surface, "Create lunar surface");
            surface.transform.SetParent(environment, false);
            var layout = surface.AddComponent<MoonSurfaceLayout>();
            layout.Configure(AssetDatabase.LoadAssetAtPath<GameObject>(MoonTileSeamBuilder.PrefabPath), null);
            MoonTileSeamBuilder.Generate(layout);
            Physics.SyncTransforms();
            var vehicles = new GameObject("Vehicles");
            Undo.RegisterCreatedObjectUndo(vehicles, "Organize vehicles");
            vehicles.transform.SetParent(gameplay, false);
            Undo.RecordObject(rover.transform, "Place rover");
            Undo.RecordObject(lm.transform, "Place lunar module");
            rover.transform.SetParent(vehicles.transform, true);
            lm.transform.SetParent(vehicles.transform, true);
            rover.transform.SetPositionAndRotation(new Vector3(10f, 4f, -20f), Quaternion.identity);
            lm.transform.SetPositionAndRotation(new Vector3(10f, 5f, 10f), Quaternion.identity);
            rover.transform.localScale = Vector3.one; lm.transform.localScale = Vector3.one;
            Physics.SyncTransforms();
            var ground = surface.GetComponentsInChildren<MeshCollider>();
            float roverHeight = float.NegativeInfinity;
            foreach (var wheel in rover.GetComponentsInChildren<WheelCollider>())
            {
                Vector3 p = wheel.transform.position;
                roverHeight = Mathf.Max(roverHeight, Height(ground, p.x, p.z));
            }
            rover.transform.position = new Vector3(10f, roverHeight + 0.04f, -20f);
            // Support the model at its measured footpad points rather than its elevated hull pivot.
            float lmHeight = float.NegativeInfinity;
            foreach (var renderer in lm.GetComponentsInChildren<Renderer>())
            {
                if (renderer.bounds.min.y > lm.transform.position.y + 0.25f) continue;
                Vector3 p = renderer.bounds.center;
                float localBottom = renderer.bounds.min.y - lm.transform.position.y;
                lmHeight = Mathf.Max(lmHeight, Height(ground, p.x, p.z) - localBottom);
            }
            lm.transform.position = new Vector3(10f, Mathf.Max(0f, lmHeight), 10f);
            var spawn = gameplay.Cast<Transform>().Single(t => t.name == "Player Spawn");
            Undo.RecordObject(spawn, "Place safe player spawn");
            spawn.position = new Vector3(10f, Height(ground, 10f, -15f) + 0.1f, -15f);
            var follow = Undo.AddComponent<VehicleFollowCamera>(camera.gameObject);
            var selectorObject = new GameObject("VehicleControlSelector");
            Undo.RegisterCreatedObjectUndo(selectorObject, "Create vehicle control selection");
            selectorObject.transform.SetParent(systems, false);
            var selector = selectorObject.AddComponent<VehicleControlSelector>();
            var so = new SerializedObject(selector);
            so.FindProperty("_controls").objectReferenceValue = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/_Project/Apollo15/Data/Vehicles/VehicleControls.inputactions");
            so.FindProperty("_driver").objectReferenceValue = rover.GetComponent<DirectRoverDriver>();
            so.FindProperty("_lunarModule").objectReferenceValue = lm;
            so.FindProperty("_roverViewAnchor").objectReferenceValue = rover.transform.Cast<Transform>().Single(t => t.name == "RoverViewAnchor");
            so.FindProperty("_inspectionOrigin").objectReferenceValue = lm.InspectionPoint;
            so.FindProperty("_camera").objectReferenceValue = follow;
            so.ApplyModifiedPropertiesWithoutUndo();
            Undo.RecordObject(camera.transform, "Frame rover camera");
            Vector3 pivot = rover.transform.position + Vector3.up;
            camera.transform.position = pivot + new Vector3(0, 2.5f, -5.4f);
            camera.transform.LookAt(pivot);
            PrefabUtility.RecordPrefabInstancePropertyModifications(rover.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(lm.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(camera.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(spawn);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = surface;
            Debug.Log("Apollo15 composed: 49 seam-corrected tiles, 140m surface, rover and stationary LM. Controls: WASD/arrows, Space brake, 1 base, 2 rover, E inspect, mouse/gamepad look.");
        }

        private static float Height(MeshCollider[] terrain, float x, float z)
        {
            float height = float.NegativeInfinity;
            var ray = new Ray(new Vector3(x, 100f, z), Vector3.down);
            foreach (var collider in terrain)
                if (collider.Raycast(ray, out var hit, 200f)) height = Mathf.Max(height, hit.point.y);
            if (float.IsNegativeInfinity(height)) throw new System.InvalidOperationException("No terrain collision under requested placement.");
            return height;
        }
    }
}
