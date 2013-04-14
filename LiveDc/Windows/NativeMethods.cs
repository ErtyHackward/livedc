using System.Drawing;
using System.Runtime.InteropServices;

namespace LiveDc.Windows
{
    public struct DWMCOLORIZATIONPARAMS
    {
        public uint ColorizationColor, 
            ColorizationAfterglow, 
            ColorizationColorBalance, 
            ColorizationAfterglowBalance, 
            ColorizationBlurBalance, 
            ColorizationGlassReflectionIntensity, 
            ColorizationOpaqueBlend;
    }

    internal static class NativeMethods
    {
        [DllImport("dwmapi.dll", EntryPoint="#127")]
        internal static extern void DwmGetColorizationParameters(ref DWMCOLORIZATIONPARAMS par);

        public static Color GetWindowColorizationColor(bool opaque)
        {
            var par = new DWMCOLORIZATIONPARAMS();
            NativeMethods.DwmGetColorizationParameters(ref par);

            return Color.FromArgb(
                (byte)(opaque ? 255 : par.ColorizationColor >> 24),
                (byte)(par.ColorizationColor >> 16),
                (byte)(par.ColorizationColor >> 8),
                (byte)par.ColorizationColor
            );
        }
    }


}
