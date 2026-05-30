// Code from https://github.com/seanmcelroy/stungun
using System.Net;

namespace stungun.common.core
{
    public struct MessageResponse
    {
        public IPEndPoint LocalEndpoint;
        public IPEndPoint RemoteEndpoint;
        public Message Message;
        public bool Success;
        public string? ErrorMessage;        
    }
}
