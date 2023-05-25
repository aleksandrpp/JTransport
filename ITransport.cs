using Unity.Collections;
using System;

namespace AK.JTransport
{
    public interface ITransport : IDisposable
    {
        /// <summary>
        /// Processing transport
        /// </summary>
        /// <param name="sendBuffer">Bytes to send allocated as TempJob</param>
        /// <param name="readBuffer">Bytes to read allocated as Temp</param>
        void Update(NativeArray<byte> sendBuffer, out NativeArray<byte> readBuffer);
    }
}
