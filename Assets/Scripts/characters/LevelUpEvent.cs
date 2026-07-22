using System.Collections.Generic;
using BaaroForce.Utils;

namespace BaaroForce.Characters
{
    /// <summary>
    /// A single level-up reached by a <see cref="Character"/> — recorded on
    /// <see cref="Character.PendingLevelUpEvents"/> so <c>LevelUpUI</c> can replay it as an
    /// animated reveal after a fight, even though the stat gains were already applied
    /// immediately when the level-up happened.
    /// </summary>
    public class LevelUpEvent
    {
        public int Level { get; }
        public List<StatPointGain> StatGains { get; }

        /// <summary>Name of the talent granted at Level 3, or null if none was granted
        /// (e.g. every other level).</summary>
        public string TalentGained { get; set; }

        public LevelUpEvent(int level, List<StatPointGain> statGains)
        {
            Level = level;
            StatGains = statGains;
        }
    }
}
