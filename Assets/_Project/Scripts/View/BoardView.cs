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
        [SerializeField] private FeelSettings feel;
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
        private SpriteRenderer _frame;
        private Transform _slotParent;
        private readonly List<SpriteRenderer> _slots = new List<SpriteRenderer>();
        private SpriteMask _mask;
        private BoardEffects _effects;

        // Draw order behind the tiles (tiles use 0 and up).
        private const int FrameSortingOrder = -20;
        private const int SlotSortingOrder = -10;

        public float CellSize => cellSize;
        /// <summary>The tile colors and sprites in use. The HUD uses it so goal icons match the tiles.</summary>
        public TileVisuals Visuals => visuals;
        /// <summary>The timings and strengths of all the juice (HUD, effects, end screen read it from here).</summary>
        public FeelSettings Feel => feel;
        /// <summary>Particles, rocket streaks and the camera shake.</summary>
        public BoardEffects Effects => _effects;

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
            _effects = BoardEffects.Create(transform);
        }

        /// <summary>Shows the given board: removes old tiles, frames the camera, creates one TileView per tile.</summary>
        public void Build(Board board)
        {
            if (tilePrefab == null || visuals == null || feel == null || boardCamera == null)
            {
                throw new System.InvalidOperationException(
                    "BoardView needs its Tile Prefab, Visuals, Feel and Board Camera assigned in the Inspector.");
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

            EnsureFrameSlotsAndMask();
            FitCamera();
            _effects.Prepare(boardCamera, visuals, feel, cellSize, _width * _height);

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
            view.Setup(tileId, color, special, visuals, feel, cellSize);
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

        // Frame = the panel around the board. Slots = one background square behind every cell.
        // Mask = tiles only show inside the board, so new tiles waiting above the board are invisible until they drop in.
        // All of this is created at the start of a level (and reused when the next level starts), never during play.
        private void EnsureFrameSlotsAndMask()
        {
            float boardWidth = _width * cellSize;
            float boardHeight = _height * cellSize;

            if (_mask == null)
            {
                GameObject maskObject = new GameObject("Board Mask");
                maskObject.transform.SetParent(transform, false);
                _mask = maskObject.AddComponent<SpriteMask>();
                _mask.sprite = PlaceholderSprite.Solid;
            }

            _mask.transform.localScale = new Vector3(boardWidth, boardHeight, 1f);

            BuildFrame(boardWidth, boardHeight);
            BuildSlots();
        }

        private void BuildFrame(float boardWidth, float boardHeight)
        {
            if (_frame == null)
            {
                GameObject frameObject = new GameObject("Board Frame");
                frameObject.transform.SetParent(transform, false);
                _frame = frameObject.AddComponent<SpriteRenderer>();
                _frame.sortingOrder = FrameSortingOrder;
            }

            float padding = visuals.FramePaddingInCells * cellSize;
            float frameWidth = boardWidth + 2f * padding;
            float frameHeight = boardHeight + 2f * padding;

            if (visuals.BoardFrameSprite != null)
            {
                // A 9-slice sprite: its corners keep their shape while the middle stretches to any board size.
                _frame.sprite = visuals.BoardFrameSprite;
                _frame.drawMode = SpriteDrawMode.Sliced;
                _frame.size = new Vector2(frameWidth, frameHeight);
                _frame.transform.localScale = Vector3.one;
                _frame.color = visuals.FrameColor;
            }
            else
            {
                // No frame art: a plain dark rectangle, scaled to size.
                _frame.sprite = PlaceholderSprite.Solid;
                _frame.drawMode = SpriteDrawMode.Simple;
                _frame.transform.localScale = new Vector3(frameWidth, frameHeight, 1f);
                _frame.color = new Color(0.10f, 0.11f, 0.16f);
            }
        }

        private void BuildSlots()
        {
            if (_slotParent == null)
            {
                _slotParent = new GameObject("Cell Slots").transform;
                _slotParent.SetParent(transform, false);
            }

            bool showSlots = visuals.CellSlotSprite != null;
            int needed = showSlots ? _width * _height : 0;

            // Only ever grows: a smaller board next level just hides the extra slots.
            while (_slots.Count < needed)
            {
                GameObject slotObject = new GameObject("Slot");
                slotObject.transform.SetParent(_slotParent, false);
                SpriteRenderer slot = slotObject.AddComponent<SpriteRenderer>();
                slot.sortingOrder = SlotSortingOrder;
                slot.drawMode = SpriteDrawMode.Sliced;
                _slots.Add(slot);
            }

            for (int i = 0; i < _slots.Count; i++)
            {
                SpriteRenderer slot = _slots[i];
                slot.gameObject.SetActive(i < needed);
                if (i >= needed) continue;

                slot.sprite = visuals.CellSlotSprite;
                slot.color = visuals.SlotColor;
                slot.size = new Vector2(cellSize * visuals.SlotFill, cellSize * visuals.SlotFill);
                slot.transform.localPosition = CellToLocal(i % _width, i / _width);
            }
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
