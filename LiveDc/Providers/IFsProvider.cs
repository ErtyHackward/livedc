using System.Collections.Generic;
using System.IO;
using SharpDc.Structs;

namespace LiveDc.Providers
{
    public interface IFsProvider
    {
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
    }
}