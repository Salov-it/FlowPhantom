using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FlowPhantom.Infrastructure.Network.Tun.Wintun
{
    internal static class WintunApi
    {
        public const string WintunDll = "wintun.dll";

        [DllImport(WintunDll, EntryPoint = "WintunCreateAdapter")]
        public static extern IntPtr WintunCreateAdapter(
            [MarshalAs(UnmanagedType.LPWStr)] string name,
            [MarshalAs(UnmanagedType.LPWStr)] string tunnelType,
            IntPtr requestedGuid
        );

        [DllImport(WintunDll, EntryPoint = "WintunDeleteAdapter")]
        public static extern void WintunDeleteAdapter(IntPtr adapter);

        [DllImport(WintunDll, EntryPoint = "WintunStartSession")]
        public static extern IntPtr WintunStartSession(
            IntPtr adapter,
            uint capacity
        );

        [DllImport(WintunDll, EntryPoint = "WintunEndSession")]
        public static extern void WintunEndSession(IntPtr session);

        [DllImport(WintunDll, EntryPoint = "WintunReceivePacket")]
        public static extern IntPtr WintunReceivePacket(
            IntPtr session,
            out uint size
        );

        [DllImport(WintunDll, EntryPoint = "WintunReleaseReceivePacket")]
        public static extern void WintunReleaseReceivePacket(
            IntPtr session,
            IntPtr packet
        );

        [DllImport(WintunDll, EntryPoint = "WintunAllocateSendPacket")]
        public static extern IntPtr WintunAllocateSendPacket(
            IntPtr session,
            uint size
        );

        [DllImport(WintunDll, EntryPoint = "WintunSendPacket")]
        public static extern void WintunSendPacket(
            IntPtr session,
            IntPtr packet
        );
    }
}

