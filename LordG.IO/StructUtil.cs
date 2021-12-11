using System.Runtime.InteropServices;

namespace LordG.IO
{
    public static class StructUtil
    {
        public static byte[] ToBytes<T>(this T data) where T : struct
        {
            var size = Marshal.SizeOf(data);
            var bytes = new byte[size];
            var ptr = Marshal.UnsafeAddrOfPinnedArrayElement(bytes, 0);
            Marshal.StructureToPtr(data, ptr, true);
            return bytes;
        }

        public static T ToStruct<T>(this byte[] data) where T : struct
        {
            var ptr = Marshal.UnsafeAddrOfPinnedArrayElement(data, 0);
            return Marshal.PtrToStructure<T>(ptr);
        }
    }
}
