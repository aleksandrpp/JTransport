using Unity.Collections;
using Unity.Jobs;
using Unity.Networking.Transport;
using System;

namespace AK.JTransport
{
    public class Client : ITransport
    {
        private readonly NetworkDriver _driver;
        private readonly NetworkPipeline _pipeline;
        private readonly int _maxPacketSize;
        private readonly NativeList<byte> _readBuffer;
        private NativeReference<NetworkConnection> _connection;
        private JobHandle _jh;

        public Client(ushort port, params Type[] types) : this(NetworkEndpoint.LoopbackIpv4.WithPort(port), types) { }

        public Client(string address, ushort port, params Type[] types) : this(NetworkEndpoint.Parse(address, port), types) { }

        public Client(NetworkEndpoint endpoint, params Type[] types)
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
            _connection = new NativeReference<NetworkConnection>(Allocator.Persistent)
            {
                Value = _driver.Connect(endpoint)
            };
        }

        public void Update(NativeArray<byte> sendBuffer, out NativeArray<byte> readBuffer)
        {
            _jh.Complete();

            readBuffer = new NativeArray<byte>(_readBuffer.AsArray(), Allocator.Temp);
            _readBuffer.Clear();

            var readJob = new ReadJob
            {
                Driver = _driver,
                Connection = _connection,
                Buffer = _readBuffer
            };

            var sendJob = new SendJob
            {
                Driver = _driver,
                Connection = _connection,
                Pipeline = _pipeline,
                Buffer = sendBuffer,
                MaxPacketSize = _maxPacketSize
            };

            _jh = _driver.ScheduleUpdate();
            _jh = sendJob.Schedule(_jh);
            _jh = sendBuffer.Dispose(_jh);
            _jh = readJob.Schedule(_jh);
        }

        public void Dispose()
        {
            _jh.Complete();

            if (!_connection.IsCreated) return;

            if (_connection.Value.IsCreated)
            {
                _driver.Disconnect(_connection.Value);
                _driver.ScheduleUpdate().Complete();
                _connection.Value = default;
            }

            _driver.Dispose();
            _readBuffer.Dispose();
            _connection.Dispose();
        }
    }
}
