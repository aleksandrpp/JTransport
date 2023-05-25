using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Networking.Transport;

namespace AK.JTransport
{
    [BurstCompile]
    public struct ReadParallelJob : IJobParallelForDefer
    {
        public NetworkDriver.Concurrent Driver;
        public NativeArray<NetworkConnection> Connections;
        [WriteOnly, NativeDisableParallelForRestriction] public NativeList<byte> Buffer;

        public void Execute(int index)
        {
            var connection = Connections[index];

            NetworkEvent.Type eventType;
            while ((eventType = Driver.PopEventForConnection(connection, out var reader)) != NetworkEvent.Type.Empty)
            {
                if (eventType == NetworkEvent.Type.Data)
                {
                    var d = new NativeArray<byte>(reader.Length, Allocator.Temp);
                    reader.ReadBytes(d);
                    Buffer.AddRange(d);
                }
                else if (eventType == NetworkEvent.Type.Disconnect)
                {
                    Connections[index] = default;
                }
            }
        }
    }
}
