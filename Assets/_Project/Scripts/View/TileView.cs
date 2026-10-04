using Match3.Core;
using Match3.Data;
using UnityEngine;

namespace Match3.View
{
    /// <summary>
    /// The picture of one model tile. It only knows how to look right; it never decides anything.
    /// BoardView creates it, StepPlayer moves it. TileId links it to the Tile in the model.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class TileView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;

        public int TileId { get; private set; }

        private void Reset()
        {
            // Runs in the editor when the component is added: fills the Inspector slot for you.
            body = GetComponent<SpriteRenderer>();
        }

        /// <summary>Makes this view show the given model tile, sized to fit one cell.</summary>
        public void Setup(int tileId, TileColor color, TileVisuals visuals, float cellSize)
        {
            TileId = tileId;

            Sprite sprite = visuals.TileSprite != null ? visuals.TileSprite : PlaceholderSprite.RoundedSquare;
            body.sprite = sprite;
            body.color = visuals.GetColor(color);
            body.sortingOrder = 0;

            // Tiles are only visible inside the board's mask, so tiles waiting above the board stay hidden.
            body.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

            float spriteWidth = sprite.bounds.size.x;
            float scale = cellSize * visuals.TileFill / spriteWidth;
            transform.localScale = new Vector3(scale, scale, 1f);

            gameObject.name = "Tile " + tileId + " " + color;
        }
    }
}
