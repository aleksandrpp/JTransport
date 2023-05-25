using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Networking.Transport;

namespace AK.JTransport
{
    public class Server : ITransport
    {
        private readonly NetworkDriver _driver;
        private readonly NetworkPipeline _pipeline;
        private readonly int _maxPacketSize;
        private readonly NativeList<byte> _readBuffer;
        private readonly NativeList<NetworkConnection> _connections;
        private JobHandle _jh;

        public Server(ushort port, params Type[] types) : this(NetworkEndpoint.AnyIpv4.WithPort(port), types) { }

        public Server(NetworkEndpoint endpoint, params Type[] types)
        {
            NetworkSettings settings = new();
            settings.WithNetworkConfigParameters(
                connectTimeoutMS: 200,
                disconnectTimeoutMS: int.MaxValue,
                heartbeatTimeoutMS: 0);

            _driver = NetworkDriver.Create(settings);
            _pipeline = _driver.CreatePipeline(types);

            _maxPacketSize = NetworkParameterConstants.MTU - _driver.MaxHeaderSize(_pipeline);

            _readBuffer = new NativeList<byte>(Allocator.Persistent);
            _connections = new NativeList<NetworkConnection>(4, Allocator.Persistent);

            var result = _driver.Bind(endpoint);
            if (result < 0)
                throw new TransportException("Server bind failed", result);

            result = _driver.Listen();
            if (result < 0)
                throw new TransportException($"Server listen failed", result);
        }

        public void Update(NativeArray<byte> sendBuffer, out NativeArray<byte> readBuffer)
        {
            _jh.Complete();

            readBuffer = new NativeArray<byte>(_readBuffer.AsArray(), Allocator.Temp);
            _readBuffer.Clear();

            var connectionsJob = new ConnectionsJob
            {
                Driver = _driver,
                Connections = _connections
            };

            var readJob = new ReadParallelJob
            {
                Driver = _driver.ToConcurrent(),
                Connections = _connections.AsDeferredJobArray(),
                Buffer = _readBuffer
            };

            var sendJob = new SendParallelJob
            {
                Driver = _driver.ToConcurrent(),
                Connections = _connections.AsDeferredJobArray(),
                Pipeline = _pipeline,
                Buffer = sendBuffer,
                MaxPacketSize = _maxPacketSize
            };

            _jh = _driver.ScheduleUpdate();
            _jh = connectionsJob.Schedule(_jh);
            _jh = sendJob.Schedule(_connections, 4, _jh);
            _jh = sendBuffer.Dispose(_jh);
            _jh = readJob.Schedule(_connections, 4, _jh);
        }

        public void Dispose()
        {
            _jh.Complete();

            for (int i = 0; i < _connections.Length; i++)
            {
                if (_connections[i].IsCreated)
                {
                    _driver.Disconnect(_connections[i]);
                }
            }

            _driver.ScheduleUpdate().Complete();

            _driver.Dispose();
            _readBuffer.Dispose();
            _connections.Dispose();
        }
    }
}
