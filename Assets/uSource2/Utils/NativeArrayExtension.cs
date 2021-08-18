using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace uSource2
{
    public static class NativeArrayExtension
    {
        public static byte[] ToRawBytes<T>(this System.Collections.Generic.IEnumerable<T> arr) where T : struct
        {
            var array = new NativeArray<T>(arr.ToArray(), Allocator.Temp);
            var slice = new NativeSlice<T>(array).SliceConvert<byte>();
            var bytes = new byte[slice.Length];
            slice.CopyTo(bytes);

            return bytes;
        }

        public static byte[] ToRawBytes<T>(this Unity.Collections.NativeArray<T> arr) where T : struct
        {
            var slice = new NativeSlice<T>(arr).SliceConvert<byte>();
            var bytes = new byte[slice.Length];
            slice.CopyTo(bytes);
            return bytes;
        }

        public static void CopyFromRawBytes<T>(this NativeArray<T> arr, byte[] bytes) where T : struct
        {
            var byteArr = new NativeArray<byte>(bytes, Allocator.Temp);
            var slice = new NativeSlice<byte>(byteArr).SliceConvert<T>();

            UnityEngine.Debug.Assert(arr.Length == slice.Length);

            slice.CopyTo(arr);
        }

        public static void CopyFromRawBytes<T>(this NativeSlice<T> arr, byte[] bytes) where T : struct
        {
            var byteArr = new NativeArray<byte>(bytes, Allocator.Temp);
            var slice = new NativeSlice<byte>(byteArr).SliceConvert<T>();

            UnityEngine.Debug.Assert(arr.Length == slice.Length);

            arr.CopyFrom(slice);
        }

        public static NativeArray<T> FromRawBytes<T>(byte[] bytes, Allocator allocator) where T : struct
        {
            int structSize = UnsafeUtility.SizeOf<T>();

            UnityEngine.Debug.Assert(bytes.Length % structSize == 0);

            int length = bytes.Length / UnsafeUtility.SizeOf<T>();
            var arr = new NativeArray<T>(length, allocator);
            arr.CopyFromRawBytes(bytes);
            return arr;
        }
    }
}
