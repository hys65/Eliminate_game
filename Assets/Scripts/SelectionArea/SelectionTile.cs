using System;
using EliminateGame.Pattern;
using UnityEngine;

namespace EliminateGame.SelectionArea
{
    public class SelectionTile : MonoBehaviour
    {
        [SerializeField] private BlockColor color;
        [SerializeField] private int x;
        [SerializeField] private int y;
        [SerializeField] private bool isUnlocked;

        public BlockColor Color => color;
        public int X => x;
        public int Y => y;
        public bool IsUnlocked => isUnlocked;
        public bool IsRemoved { get; private set; }

        public event Action<SelectionTile> Clicked;

        public void Initialize(int gridX, int gridY, BlockColor tileColor, bool startUnlocked)
        {
            x = gridX;
            y = gridY;
            color = tileColor;
            IsRemoved = false;
            SetUnlocked(startUnlocked);
            name = $"SelectionTile_{x}_{y}_{color}";
        }

        public void SetUnlocked(bool unlocked)
        {
            if (IsRemoved)
            {
                return;
            }

            isUnlocked = unlocked;
            Debug.Log($"Selection Area tile ({x},{y}) {color} unlocked={isUnlocked}");
        }

        public void RemoveFromSelectionArea()
        {
            if (IsRemoved)
            {
                return;
            }

            IsRemoved = true;
            isUnlocked = false;
            gameObject.SetActive(false);
            Debug.Log($"Selection Area tile ({x},{y}) {color} removed.");
        }

        private void OnMouseDown()
        {
            if (!enabled || IsRemoved || !isUnlocked)
            {
                return;
            }

            Clicked?.Invoke(this);
        }
    }
}
