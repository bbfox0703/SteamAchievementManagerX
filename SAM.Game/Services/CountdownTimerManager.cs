using System.Collections.Generic;

namespace SAM.Game.Services
{
    /// <summary>
    /// Manages countdown timers for achievements.
    /// Tracks countdown values and provides decrement logic.
    /// </summary>
    internal class CountdownTimerManager
    {
        private readonly Dictionary<string, int> _achievementCounters;

        /// <summary>
        /// Initializes a new instance of the CountdownTimerManager.
        /// </summary>
        public CountdownTimerManager()
        {
            this._achievementCounters = new Dictionary<string, int>();
        }

        /// <summary>
        /// Sets a countdown timer for an achievement.
        /// </summary>
        /// <param name="achievementId">The achievement ID</param>
        /// <param name="seconds">Countdown duration in seconds (-1 to disable)</param>
        public void SetTimer(string achievementId, int seconds)
        {
            this._achievementCounters[achievementId] = seconds;
        }

        /// <summary>
        /// Gets the current countdown value for an achievement.
        /// </summary>
        /// <param name="achievementId">The achievement ID</param>
        /// <returns>Countdown value in seconds, or -1 if not set</returns>
        public int GetTimer(string achievementId)
        {
            return this._achievementCounters.TryGetValue(achievementId, out int value) ? value : -1;
        }

        /// <summary>
        /// Decrements the countdown timer for an achievement.
        /// </summary>
        /// <param name="achievementId">The achievement ID</param>
        /// <param name="newValue">Output parameter containing the new countdown value</param>
        /// <returns>True if timer reached zero (should trigger unlock), false otherwise</returns>
        public bool DecrementTimer(string achievementId, out int newValue)
        {
            newValue = -1;

            if (!this._achievementCounters.TryGetValue(achievementId, out int counter))
            {
                return false;
            }

            if (counter <= 0)
            {
                return false;
            }

            counter--;
            newValue = counter;
            this._achievementCounters[achievementId] = counter;

            if (counter == 0)
            {
                // Reset to -1 to prevent re-triggering
                this._achievementCounters[achievementId] = -1;
                newValue = -1;
                return true; // Timer reached zero, should trigger unlock
            }

            return false;
        }

        /// <summary>
        /// Checks if a timer is active (value > 0) for an achievement.
        /// </summary>
        /// <param name="achievementId">The achievement ID</param>
        /// <returns>True if timer is active, false otherwise</returns>
        public bool IsTimerActive(string achievementId)
        {
            return this._achievementCounters.TryGetValue(achievementId, out int value) && value > 0;
        }

        /// <summary>
        /// Clears all countdown timers.
        /// </summary>
        public void ClearAllTimers()
        {
            this._achievementCounters.Clear();
        }

        /// <summary>
        /// Gets the count of active timers (value > 0).
        /// </summary>
        /// <returns>Number of active timers</returns>
        public int GetActiveTimerCount()
        {
            int count = 0;
            foreach (var value in this._achievementCounters.Values)
            {
                if (value > 0)
                {
                    count++;
                }
            }
            return count;
        }
    }
}
