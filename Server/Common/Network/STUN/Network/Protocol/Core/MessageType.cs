public enum MessageType : ushort
{
    BindingRequest = 0x0001,
    SharedSecretRequest = 0x0002,
    BindingSuccessResponse = 0x0101,
    SharedSecretResponse = 0x0102,
    BindingErrorResponse = 0x0111,
    SharedSecretErrorResponse = 0x0112,
}