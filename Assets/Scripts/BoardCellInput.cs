using UnityEngine;
using UnityEngine.EventSystems;

namespace KillerMineDoku.UI
{
    [DisallowMultipleComponent]
    public sealed class BoardCellInput : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private PuzzleBoardRuntime board;
        private int row;
        private int column;

        public void Initialize(PuzzleBoardRuntime owner, int nextRow, int nextColumn)
        {
            board = owner;
            row = nextRow;
            column = nextColumn;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            board?.HoverCell(row, column);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            board?.ClearHover(row, column);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                board?.ClickCell(row, column, BoardCellView.CellMark.Safe);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                board?.ClickCell(row, column, BoardCellView.CellMark.Mine);
            }
        }
    }
}
