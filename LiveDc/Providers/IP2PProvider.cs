using System;
using System.Collections.Generic;
using System.IO;
using LiveDc.Notify;
using SharpDc.Structs;

namespace LiveDc.Providers
{
    /// <summary>
    /// General p2p features that required by LiveDC to work
    /// </summary>
    public interface IP2PProvider : IDisposable
    {
        /// <summary>
        /// Occurs when online status is changed
        /// </summary>
        event EventHandler StatusChanged;
        
        /// <summary>
        /// Indicates if the provider is connected and able to work
        /// </summary>
        bool Online { get; }

        /// <summary>
        /// Prepares specific resources (establish connections with servers, etc)
        /// </summary>
        void Initialize();

        /// <summary>
        /// Enumerates all files that are acessible now
        /// </summary>
        /// <returns></returns>
        IEnumerable<Magnet> AllMagnets();

        /// <summary>
        /// Returns file data stream for the magnet
        /// </summary>
        /// <param name="magnet"></param>
        /// <returns></returns>
        Stream GetStream(Magnet magnet);
        
        bool CanHandle(Magnet magnet);
        
        /// <summary>
        /// Creates communication object between GUI and the provider
        /// Tries to start download at the same time
        /// </summary>
        /// <param name="magnet"></param>
        /// <returns></returns>
        IStartItem StartItem(Magnet magnet);

        /// <summary>
        /// Gets path of a file on a virtual drive
        /// </summary>
        /// <param name="magnet"></param>
        /// <returns></returns>
        string GetVirtualPath(Magnet magnet);

        /// <summary>
        /// Gets system path of cache file. Returns null if not applicable
        /// </summary>
        /// <param name="magnet"></param>
        /// <returns></returns>
        string GetRealPath(Magnet magnet);

        /// <summary>
        /// Updates view information for a file
        /// </summary>
        /// <param name="control"></param>
        void UpdateFileItem(DcFileControl control);

        /// <summary>
        /// Completely removes file from the app
        /// Stops seeding
        /// Deletes data from the hard drive
        /// </summary>
        /// <param name="magnet"></param>
        void DeleteFile(Magnet magnet);
    }
}
