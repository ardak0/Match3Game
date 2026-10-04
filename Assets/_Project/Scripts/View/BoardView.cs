using System.Collections.Generic;
using Match3.Core;
using Match3.Data;
using Match3.Infrastructure;
using UnityEngine;

namespace Match3.View
{
    /// <summary>
    /// Everything about WHERE things are on screen: cell positions, the tile objects, the board background,
    /// and the camera framing. It builds the picture of a Board but never changes the Board.
    ///
    /// Geometry: the board is centered on this object. Cell (x, y) has its center at
    /// ((x - (width-1)/2) * cellSize, (y - (height-1)/2) * cellSize); y = 0 is the bottom row, as in Core.
    /// </summary>
    public sealed class BoardView : MonoBehaviour
    {
        [SerializeField] private TileView tilePrefab;
        [SerializeField] private TileVisuals visuals;
        [SerializeField] private Camera boardCamera;

        [Tooltip("Size of one cell in world units.")]
        [SerializeField] private float cellSize = 1f;

        [Tooltip("Empty space around the board, in cells, when the camera is fitted to it.")]
        [SerializeField] private float cameraPaddingInCells = 0.75f;

        // Big enough that the dictionary never has to grow while playing.
        private const int InitialTileCapacity = 256;

        private readonly Dictionary<int, TileView> _tiles = new Dictionary<int, TileView>(InitialTileCapacity);

        // Tiles are never created or destroyed during play: they come from this pool and go back to it.
        private ObjectPool<TileView> _tilePool;
        private int _prewarmedTileCount;

        private int _width;
        private int _height;
        private SpriteRenderer _background;
        private SpriteMask _mask;

        public float CellSize => cellSize;
        public int Width => _width;
        public int Height => _height;

        /// <summary>Tiles currently shown on the board (handed out by the pool).</summary>
        public int ActiveTileCount => _tilePool.ActiveCount;

        /// <summary>Tiles waiting in the pool, hidden.</summary>
        public int PooledTileCount => _tilePool.InactiveCount;

        private void Reset()
        {
            boardCamera = Camera.main; // editor-only convenience when the component is added
        }

        private void Awake()
        {
            // The three methods are passed once here, so using the pool later creates no new delegates.
            _tilePool = new ObjectPool<TileView>(CreateTileObject, OnTileTaken, OnTileReturned);
        }

        /// <summary>Shows the given board: removes old tiles, frames the camera, creates one TileView per tile.</summary>
        public void Build(Board board)
        {
            if (tilePrefab == null || visuals == null || boardCamera == null)
            {
                throw new System.InvalidOperationException(
                    "BoardView needs its Tile Prefab, Visuals and Board Camera assigned in the Inspector.");
            }

            ClearTiles();
            _width = board.Width;
            _height = board.Height;

            // Upper bound of tile views alive at once: at the start of a wave the board is full (width * height),
            // and that wave creates at most as many new tiles as it clears (<= width * height). So 2x is always enough
            // and the pool never has to Instantiate during play.
            int tilesNeeded = 2 * _width * _height;
            _tilePool.Prewarm(tilesNeeded);
            _prewarmedTileCount = Mathf.Max(_prewarmedTileCount, tilesNeeded);

            EnsureBackgroundAndMask();
            FitCamera();

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    Tile tile = board.Get(x, y);
                    CreateTile(tile.Id, tile.Color, tile.Special, CellToLocal(x, y));
                }
            }
        }

        /// <summary>Center of a cell in this object's local space. Rows above the board (y >= height) work too.</summary>
        public Vector3 CellToLocal(float x, float y)
        {
            return new Vector3(
                (x - (_width - 1) / 2f) * cellSize,
                (y - (_height - 1) / 2f) * cellSize,
                0f);
        }

        /// <summary>Converts a world position to the cell under it. False if it is outside the board.</summary>
        public bool TryWorldToCell(Vector3 worldPosition, out GridPos cell)
        {
            Vector3 local = transform.InverseTransformPoint(worldPosition);
            int x = Mathf.FloorToInt(local.x / cellSize + _width / 2f);
            int y = Mathf.FloorToInt(local.y / cellSize + _height / 2f);

            cell = new GridPos(x, y);
            return x >= 0 && x < _width && y >= 0 && y < _height;
        }

        public TileView CreateTile(int tileId, TileColor color, SpecialType special, Vector3 localPosition)
        {
            TileView view = _tilePool.Get();

            if (_tilePool.TotalCreated > _prewarmedTileCount)
            {
                Debug.LogWarning("The tile pool had to create a tile during play. Its prewarm size is too small.");
            }

            view.transform.localPosition = localPosition;
            view.Setup(tileId, color, special, visuals, cellSize);
            _tiles.Add(tileId, view);
            return view;
        }

        /// <summary>Finds the view of a model tile. A missing one means view and model disagree, which is a bug, so it throws.</summary>
        public TileView GetTile(int tileId)
        {
            if (!_tiles.TryGetValue(tileId, out TileView view))
            {
                throw new KeyNotFoundException("BoardView has no TileView for tile id " + tileId + " (view and model are out of sync).");
            }

            return view;
        }

        /// <summary>Takes the tile off the board and gives it back to the pool (it is hidden, not destroyed).</summary>
        public void RemoveTile(int tileId)
        {
            TileView view = GetTile(tileId);
            _tiles.Remove(tileId);
            _tilePool.Release(view);
        }

        private void ClearTiles()
        {
            foreach (KeyValuePair<int, TileView> pair in _tiles)
            {
                _tilePool.Release(pair.Value);
            }

            _tiles.Clear();
        }

        // ---- the three methods the pool uses ----

        private TileView CreateTileObject() => Instantiate(tilePrefab, transform);

        private static void OnTileTaken(TileView view) => view.gameObject.SetActive(true);

        private static void OnTileReturned(TileView view) => view.ResetForPool();

        // Background = dark panel behind the tiles. Mask = tiles only show inside the board,
        // so new tiles waiting above the board are invisible until they drop in.
        private void EnsureBackgroundAndMask()
        {
            float boardWidth = _width * cellSize;
            float boardHeight = _height * cellSize;

            if (_background == null)
            {
                GameObject backgroundObject = new GameObject("Board Background");
                backgroundObject.transform.SetParent(transform, false);
                _background = backgroundObject.AddComponent<SpriteRenderer>();
                _background.sprite = PlaceholderSprite.Solid;
                _background.color = new Color(0.10f, 0.11f, 0.16f);
                _background.sortingOrder = -10;
            }

            if (_mask == null)
            {
                GameObject maskObject = new GameObject("Board Mask");
                maskObject.transform.SetParent(transform, false);
                _mask = maskObject.AddComponent<SpriteMask>();
                _mask.sprite = PlaceholderSprite.Solid;
            }

            float padding = cellSize * 0.1f;
            _background.transform.localScale = new Vector3(boardWidth + padding, boardHeight + padding, 1f);
            _mask.transform.localScale = new Vector3(boardWidth, boardHeight, 1f);
        }

        // Zooms the camera so the whole board plus padding fits, in portrait or landscape.
        private void FitCamera()
        {
            float halfHeight = (_height + 2f * cameraPaddingInCells) * cellSize / 2f;
            float halfWidth = (_width + 2f * cameraPaddingInCells) * cellSize / 2f;

            // An orthographic camera shows 2 * orthographicSize vertically and that times aspect horizontally.
            boardCamera.orthographicSize = Mathf.Max(halfHeight, halfWidth / boardCamera.aspect);

            Vector3 cameraPosition = boardCamera.transform.position;
            Vector3 center = transform.position;
            boardCamera.transform.position = new Vector3(center.x, center.y, cameraPosition.z);
        }
    }
}
