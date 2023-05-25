using Unity.Networking.Transport.Error;

namespace AK.JTransport
{
    public class TransportException : System.Exception
    {
        public TransportException(string message, int code) : base($"{message} with status: {(StatusCode)code})") { }
    }
}
