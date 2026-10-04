using System;
using Match3.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Match3.View
{
    /// <summary>
    /// Turns a mouse drag or a finger swipe into "swap these two neighboring cells".
    /// Press on a tile, drag far enough in one direction, and SwipeDetected fires once.
    /// It works for mouse and touch because both are "Pointer" devices in the Input System.
    ///
    /// SwipeInput only reports intent. It does not know the rules and never touches the board.
    /// Whoever runs the game flow turns InputEnabled off while animations play.
    /// (Update only samples the pointer; game flow is driven by the SwipeDetected event.)
    /// </summary>
    public sealed class SwipeInput : MonoBehaviour
    {
        [SerializeField] private BoardView boardView;
        [SerializeField] private Camera inputCamera;

        [Tooltip("How far you must drag before it counts as a swipe, as a fraction of one cell's size on screen.")]
        [SerializeField, Range(0.1f, 1f)] private float swipeThresholdInCells = 0.35f;

        private bool _inputEnabled = true;
        private bool _isDragging;
        private GridPos _startCell;
        private Vector2 _startScreenPosition;

        /// <summary>Raised once per swipe with the cell that was pressed and its neighbor in the swipe direction.</summary>
        public event Action<GridPos, GridPos> SwipeDetected;

        public bool InputEnabled
        {
            get => _inputEnabled;
            set
            {
                _inputEnabled = value;
                if (!value) _isDragging = false; // a drag in progress is cancelled
            }
        }

        private void Reset()
        {
            // Editor-only convenience when the component is added.
            boardView = GetComponent<BoardView>();
            inputCamera = Camera.main;
        }

        private void Update()
        {
            if (!_inputEnabled) return;

            Pointer pointer = Pointer.current; // the last used mouse / touchscreen / pen
            if (pointer == null) return;

            Vector2 position = pointer.position.ReadValue();

            if (pointer.press.wasPressedThisFrame)
            {
                BeginDrag(position);
            }
            else if (_isDragging && pointer.press.isPressed)
            {
                ContinueDrag(position);
            }
            else
            {
                _isDragging = false; // released
            }
        }

        private void BeginDrag(Vector2 screenPosition)
        {
            Vector3 world = inputCamera.ScreenToWorldPoint(
                new Vector3(screenPosition.x, screenPosition.y, -inputCamera.transform.position.z));

            _isDragging = boardView.TryWorldToCell(world, out _startCell);
            _startScreenPosition = screenPosition;
        }

        private void ContinueDrag(Vector2 screenPosition)
        {
            Vector2 drag = screenPosition - _startScreenPosition;

            // Convert the threshold from "cells" to pixels for the current camera zoom and screen size.
            float pixelsPerWorldUnit = inputCamera.pixelHeight / (2f * inputCamera.orthographicSize);
            float thresholdPixels = swipeThresholdInCells * boardView.CellSize * pixelsPerWorldUnit;
            if (drag.sqrMagnitude < thresholdPixels * thresholdPixels) return;

            _isDragging = false; // one swipe per press, whatever happens next

            // Only the dominant axis counts: a mostly-horizontal drag is a horizontal swipe.
            GridPos target = Mathf.Abs(drag.x) > Mathf.Abs(drag.y)
                ? new GridPos(_startCell.X + (drag.x > 0f ? 1 : -1), _startCell.Y)
                : new GridPos(_startCell.X, _startCell.Y + (drag.y > 0f ? 1 : -1));

            // Swiping off the edge of the board does nothing.
            Vector3 targetWorld = boardView.transform.TransformPoint(boardView.CellToLocal(target.X, target.Y));
            if (!boardView.TryWorldToCell(targetWorld, out _)) return;

            SwipeDetected?.Invoke(_startCell, target);
        }
    }
}
