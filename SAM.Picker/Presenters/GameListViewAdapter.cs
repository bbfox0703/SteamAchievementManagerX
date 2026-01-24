#nullable enable

using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SAM.Picker.Presenters
{
    /// <summary>
    /// Adapter for virtual ListView that provides game items on demand.
    /// </summary>
    internal class GameListViewAdapter
    {
        private readonly List<GameInfo> _games;
        private readonly object _lock;

        /// <summary>
        /// Initializes a new GameListViewAdapter.
        /// </summary>
        /// <param name="games">The filtered games list</param>
        /// <param name="lockObject">Lock object for thread-safe access</param>
        public GameListViewAdapter(List<GameInfo> games, object lockObject)
        {
            this._games = games;
            this._lock = lockObject;
        }

        /// <summary>
        /// Handles RetrieveVirtualItem event to create ListViewItems on demand.
        /// </summary>
        public void OnRetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            lock (this._lock)
            {
                // Bounds check to prevent race condition
                if (e.ItemIndex < 0 || e.ItemIndex >= this._games.Count)
                {
                    e.Item = new ListViewItem("Loading...");
                    return;
                }

                var info = this._games[e.ItemIndex];
                e.Item = info.Item = new()
                {
                    Text = info.Name,
                    ImageIndex = info.ImageIndex,
                };
            }
        }

        /// <summary>
        /// Handles SearchForVirtualItem event to implement incremental search.
        /// </summary>
        public void OnSearchForVirtualItem(object sender, SearchForVirtualItemEventArgs e)
        {
            if (e.Direction != SearchDirectionHint.Down || e.IsTextSearch == false)
            {
                return;
            }

            lock (this._lock)
            {
                var count = this._games.Count;
                if (count < 2)
                {
                    return;
                }

                var text = e.Text ?? string.Empty;
                if (string.IsNullOrEmpty(text))
                {
                    return;
                }

                int startIndex = e.StartIndex;

                // Prefix search predicate
                Predicate<GameInfo> predicate = gi => gi.Name != null &&
                    gi.Name.StartsWith(text, StringComparison.CurrentCultureIgnoreCase);

                int index;
                if (e.StartIndex >= count)
                {
                    // Starting from the last item in the list
                    index = this._games.FindIndex(0, startIndex - 1, predicate);
                }
                else if (startIndex <= 0)
                {
                    // Starting from the first item in the list
                    index = this._games.FindIndex(0, count, predicate);
                }
                else
                {
                    // Starting from middle of list
                    index = this._games.FindIndex(startIndex, count - startIndex, predicate);
                    if (index < 0)
                    {
                        // Wrap around to beginning
                        index = this._games.FindIndex(0, startIndex - 1, predicate);
                    }
                }

                e.Index = index < 0 ? -1 : index;
            }
        }
    }
}
