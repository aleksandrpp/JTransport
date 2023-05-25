using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace AK.JTransport
{
    [BurstCompile]
    public struct SliceJob : IJob
    {
        [ReadOnly] public NativeArray<byte> Buffer;
        [ReadOnly] public int MaxPacketSize;
        public UnsafeList<NativeArray<byte>> Slices;

        public void Execute()
        {
            var sliceCount = (int)(Buffer.Length / (float)MaxPacketSize);

            for (int slice = 0; slice <= sliceCount; slice++)
            {
                Slice(slice);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Slice(int slice)
        {
            var size = math.min(Buffer.Length - slice * MaxPacketSize, MaxPacketSize);
            var buffer = new NativeArray<byte>(size, Allocator.Temp);
            var sliceBuffer = Buffer.Slice(slice * MaxPacketSize, size);
            sliceBuffer.CopyTo(buffer);

            Slices.Add(buffer);
        }
    }
}
