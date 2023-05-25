using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Networking.Transport;

namespace AK.JTransport
{
    [BurstCompile]
    public struct ReadJob : IJob
    {
        public NetworkDriver Driver;
        public NativeReference<NetworkConnection> Connection;
        [WriteOnly] public NativeList<byte> Buffer;

        public void Execute()
        {
            NetworkEvent.Type eventType;
            while ((eventType = Connection.Value.PopEvent(Driver, out var reader)) != NetworkEvent.Type.Empty)
            {
                if (eventType == NetworkEvent.Type.Data)
                {
                    var d = new NativeArray<byte>(reader.Length, Allocator.Temp);
                    reader.ReadBytes(d);
                    Buffer.AddRange(d);
                }
                else if (eventType == NetworkEvent.Type.Disconnect)
                {
                    Connection.Value = default;
                }
            }
        }
    }
}
