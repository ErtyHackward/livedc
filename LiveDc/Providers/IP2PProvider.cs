using System;
using LiveDc.Notify;
using SharpDc.Structs;

namespace LiveDc.Providers
{
    /// <summary>
    /// General p2p features that required by LiveDC to work
    /// </summary>
    public interface IP2PProvider : IDisposable, IFsProvider
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
    }
}
