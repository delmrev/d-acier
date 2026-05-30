public static class LobbySystemMsg
{
    public static async Task Process(FPacket fPacket, Session session)
    {
        var data = Reader.ReadBytes(fPacket.payload,"BBHLLQ");
        List<byte> buffer;
        switch ((byte)data[0])
        {
            case 0x44:
                await CreateLobby.Process(fPacket, session);
            break;
            case 0x45:
                await JoinLobby.Process(fPacket,session);
            break;
            case 0x46: // Disconnect (Dedicated)
                buffer = Writer.WriteBytes("BBHLLQ",LobbyCommandsClient.DISCONNECT,StatusCode.SUCCESS,339,session.unk_1,session.unk_2,(long)data[5]);
                FResponse fResponse = new(fPacket.channel,FClientOpcode.LobbyMessage, buffer);
                await ProxyReader.FinalizePacket(fResponse.ToSend(),session);
            break;
        }
    }
}