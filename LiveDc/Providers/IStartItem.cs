using System;
using SharpDc.Structs;

namespace LiveDc.Providers
{
    /// <summary>
    /// Describes GUI-protocol interaction object at the stage of file bootstap
    /// </summary>
    public interface IStartItem : IDisposable
    {
        Magnet Magnet { get; }

        /// <summary>
        /// Indicates if file data is available
        /// </summary>
        bool ReadyToStart { get; }

        /// <summary>
        /// Gets current status message
        /// </summary>
        string StatusMessage { get; }

        /// <summary>
        /// Puts this item to be downloaded in background
        /// Dispose will no longer stop download of the item
        /// </summary>
        void AddToQueue();

        /// <summary>
        /// Opens file from the virtual drive
        /// </summary>
        void OpenFile();

        /// <summary>
        /// Indicates current progress 
        /// float.Nan means no progress
        /// float.PositiveInfinity means waiting animation
        /// [0;1] current progress
        /// </summary>
        float Progress { get; }
    }
}