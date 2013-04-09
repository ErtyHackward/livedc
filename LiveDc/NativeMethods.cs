using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace LiveDc
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
