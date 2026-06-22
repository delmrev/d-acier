using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.F
{
    public class LobbySystemMsg : IFPacketHandler
    {
        private readonly CreateLobby _createlobby = new();
        private readonly JoinLobby _joinLobby = new();
        public async Task Process(FPacket fPacket, Session session)
        {
            var data = await Reader.ReadBytes(fPacket.payload,"BBHLLQ");
            List<byte> buffer;
            switch ((byte)data[0])
            {
                case 0x44:
                    await _createlobby.Process(fPacket, session);
                break;
                case 0x45:
                    await _joinLobby.Process(fPacket,session);
                break;
                case 0x46:
                    buffer = await Writer.WriteBytes("BBHLLQ",LobbyCommandsClient.Disconnect,StatusCode.Success,339,session.unk_1,session.unk_2,(long)data[5]);
                    FPacket fResponse = new(fPacket.channel,(byte)FClientOpcode.LobbyMessage, buffer);
                    await session.Send(await fResponse.ToSend());
                break;
            }
        }
    }
}