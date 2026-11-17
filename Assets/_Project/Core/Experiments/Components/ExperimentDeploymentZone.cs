using System.Collections.Generic;
using UnityEngine;
using SpaceSeries.Core.Interaction;

namespace SpaceSeries.Core.Experiments
{
    [RequireComponent(typeof(BoxCollider)), DisallowMultipleComponent]
    public sealed class ExperimentDeploymentZone : MonoBehaviour, IInteractable
    {
        public enum DeploymentState { Available, Deploying, Deployed }
        [SerializeField] private ExperimentDefinitionSO _definition;
        [SerializeField] private Transform _spawnAnchor;
        [SerializeField] private GameObject _marker;
        [SerializeField] private int _priority;
        [Header("Placement clearance")]
        [SerializeField] private Vector3 _clearanceSize = new Vector3(1.2f, 1f, 1.2f);
        [Tooltip("Layers that obstruct placement; terrain touching the equipment base is ignored by the raised check.")]
        [SerializeField] private LayerMask _obstacleLayers = ~0;
        private BoxCollider _trigger;
        private IInteractor _owner;
        private readonly Dictionary<IInteractor, HashSet<Collider>> _occupants = new Dictionary<IInteractor, HashSet<Collider>>();
        private readonly Collider[] _obstacles = new Collider[32];
        public ExperimentDefinitionSO Definition => _definition;
        public Transform SpawnAnchor => _spawnAnchor;
        public int Priority => _priority;
        public DeploymentState State { get; private set; }
        public GameObject SpawnedInstance { get; private set; }
        public string PromptText => _definition == null ? "Experiment" : _definition.ExperimentName;
        public string ActionVerb => "Deploy";
        public Vector3 InteractionPosition => transform.position;
        public bool IsInteractable => isActiveAndEnabled && State == DeploymentState.Available;
        private void Awake() { _trigger = GetComponent<BoxCollider>(); }
        private void OnValidate() { var box = GetComponent<BoxCollider>(); if (box != null) box.isTrigger = true; }
        public bool Contains(IInteractor actor)
        {
            if (actor == null || actor.InteractorTransform == null || !isActiveAndEnabled) return false;
            if (_trigger == null) _trigger = GetComponent<BoxCollider>();
            if (!_trigger.enabled || !_trigger.isTrigger) return false;
            Vector3 p = transform.InverseTransformPoint(actor.InteractorTransform.position) - _trigger.center;
            Vector3 half = _trigger.size * 0.5f;
            return Mathf.Abs(p.x) <= half.x && Mathf.Abs(p.y) <= half.y && Mathf.Abs(p.z) <= half.z;
        }
        public bool CanInteract(IInteractor actor) => IsInteractable && _definition != null && Contains(actor);
        public void OnFocusGained(IInteractor actor) { }
        public void OnFocusLost(IInteractor actor) { }
        public InteractionResult Interact(IInteractor actor)
        {
            bool accepted = CanInteract(actor) && actor is IExperimentDeploymentRequester requester && requester.RequestDeployment(this);
            return accepted ? InteractionResult.Success(null, default, default) : InteractionResult.Failed();
        }
        public bool TryReserve(IInteractor actor)
        {
            if (!CanInteract(actor)) return false;
            _owner = actor; State = DeploymentState.Deploying; return true;
        }
        public void Release(IInteractor actor)
        {
            if (State != DeploymentState.Deploying || _owner != actor) return;
            State = DeploymentState.Available; _owner = null;
        }
        public bool Commit(IInteractor actor, GameObject spawned)
        {
            if (State != DeploymentState.Deploying || _owner != actor || spawned == null) return false;
            SpawnedInstance = spawned; State = DeploymentState.Deployed; _owner = null;
            if (_marker != null) _marker.SetActive(false);
            return true;
        }
        public bool IsPlacementClear()
        {
            if (_spawnAnchor == null) return false;
            Vector3 half = _clearanceSize * 0.5f;
            int count = Physics.OverlapBoxNonAlloc(_spawnAnchor.position + _spawnAnchor.up * (half.y + 0.12f), half,
                _obstacles, _spawnAnchor.rotation, _obstacleLayers, QueryTriggerInteraction.Ignore);
            if (count == _obstacles.Length) return false;
            for (int i = 0; i < count; i++)
                if (_obstacles[i] != null && !_obstacles[i].transform.IsChildOf(transform)) return false;
            return true;
        }
        private void OnTriggerEnter(Collider other)
        {
            var actor = other.GetComponentInParent<IInteractor>();
            if (!(actor is IExperimentDeploymentRequester)) return;
            if (!_occupants.TryGetValue(actor, out var colliders)) { colliders = new HashSet<Collider>(); _occupants.Add(actor, colliders); }
            colliders.Add(other);
        }
        private void OnTriggerExit(Collider other)
        {
            var actor = other.GetComponentInParent<IInteractor>();
            if (actor == null || !_occupants.TryGetValue(actor, out var colliders)) return;
            colliders.Remove(other); if (colliders.Count == 0) _occupants.Remove(actor);
        }
        private void OnDisable()
        {
            _occupants.Clear();
            if (State == DeploymentState.Deploying) { State = DeploymentState.Available; _owner = null; }
        }
        private void OnDrawGizmosSelected()
        {
            var box = GetComponent<BoxCollider>(); if (box == null) return;
            Gizmos.matrix = transform.localToWorldMatrix; Gizmos.color = Color.cyan; Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
