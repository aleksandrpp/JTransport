using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Networking.Transport;
using Unity.Mathematics;
using System.Runtime.CompilerServices;
using System;

namespace AK.JTransport
{
    [BurstCompile]
    public struct SendParallelJob : IJobParallelForDefer
    {
        public NetworkDriver.Concurrent Driver;
        [ReadOnly] public NativeArray<NetworkConnection> Connections;
        [ReadOnly] public NetworkPipeline Pipeline;
        [ReadOnly] public NativeArray<byte> Buffer;
        [ReadOnly] public int MaxPacketSize;

        public void Execute(int index)
        {
            if (Driver.GetConnectionState(Connections[index]) != NetworkConnection.State.Connected)
                return;

            if (Buffer.Length == 0)
                return;

            var sliceCount = (int)(Buffer.Length / (float)MaxPacketSize);

            for (int slice = 0; slice <= sliceCount; slice++)
            {
                SliceSend(index, slice);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SliceSend(int index, int slice)
        {
            var size = math.min(Buffer.Length - slice * MaxPacketSize, MaxPacketSize);
            var buffer = new NativeArray<byte>(size, Allocator.Temp);
            var sliceBuffer = Buffer.Slice(slice * MaxPacketSize, size);
            sliceBuffer.CopyTo(buffer);

            Send(index, buffer);
        }

        private void Send(int index, NativeArray<byte> buffer)
        {
            var result = Driver.BeginSend(Pipeline, Connections[index], out var writer);
            if (result < 0)
                throw new TransportException("Begin send failed", result);

            writer.WriteBytes(buffer);

            result = Driver.EndSend(writer);
            if (result < 0)
                throw new TransportException("End send failed", result);
        }
    }
}
