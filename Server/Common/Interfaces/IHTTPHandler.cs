namespace EugnetProtocol.Common.Interfaces
{
    public interface IHTTPHandler
    {
        public Task Process(HTTPRequestOptions request, HTTPResponseOptions response);
    }
}
