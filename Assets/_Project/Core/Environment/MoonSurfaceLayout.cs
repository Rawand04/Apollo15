using UnityEngine;
namespace SpaceSeries.Core.Environment
{
    public sealed class MoonSurfaceLayout : MonoBehaviour
    {
        [SerializeField] private GameObject _tilePrefab;
        [SerializeField, Min(1)] private int _rows = 7;
        [SerializeField, Min(1)] private int _columns = 7;
        [SerializeField, Min(1f)] private float _tileSize = 20f;
        [SerializeField] private Transform _generatedRoot;
        public GameObject TilePrefab => _tilePrefab;
        public int Rows => _rows;
        public int Columns => _columns;
        public float TileSize => _tileSize;
        public Transform GeneratedRoot => _generatedRoot;
        public void Configure(GameObject tile, Transform generatedRoot)
        {
            _tilePrefab = tile;
            _generatedRoot = generatedRoot;
        }
    }
}
