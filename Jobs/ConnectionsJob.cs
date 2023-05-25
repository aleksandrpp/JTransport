using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Networking.Transport;

namespace AK.JTransport
{
    [BurstCompile]
    public struct ConnectionsJob : IJob
    {
        public NetworkDriver Driver;
        public NativeList<NetworkConnection> Connections;

        public void Execute()
        {
            for (int i = 0; i < Connections.Length; i++)
            {
                if (!Connections[i].IsCreated)
                {
                    Connections.RemoveAtSwapBack(i);
                    i--;
                }
            }

            NetworkConnection connection;
            while ((connection = Driver.Accept()) != default)
            {
                Connections.Add(connection);
            }
        }
    }
}
