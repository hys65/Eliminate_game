using System.Collections.Generic;
using UnityEngine;

namespace EliminateGame.Data
{
    [CreateAssetMenu(fileName = "LevelDatabase", menuName = "EliminateGame/Level Database")]
    public class LevelDatabase : ScriptableObject
    {
        [SerializeField] private List<GameConfig> levels = new List<GameConfig>();
        [SerializeField] private int defaultLevelIndex = 0;

        public int Count => levels.Count;
        public int DefaultLevelIndex => defaultLevelIndex;
        public IReadOnlyList<GameConfig> Levels => levels;

        public bool TryGetLevel(int index, out GameConfig config)
        {
            config = null;

            if (index < 0 || index >= levels.Count)
            {
                return false;
            }

            config = levels[index];
            return config != null;
        }

        public GameConfig GetDefaultLevel()
        {
            if (TryGetLevel(defaultLevelIndex, out GameConfig config))
            {
                return config;
            }

            for (int i = 0; i < levels.Count; i++)
            {
                if (levels[i] != null)
                {
                    return levels[i];
                }
            }

            return null;
        }
    }
}
