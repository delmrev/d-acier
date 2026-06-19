public static class LobbySystemMsg
{
    public static async Task Process(FPacket fPacket, Session session)
    {
        var data = await Reader.ReadBytes(fPacket.payload,"BBHLLQ");
        List<byte> buffer;
        switch ((byte)data[0])
        {
            case 0x44:
                await CreateLobby.Process(fPacket, session);
            break;
            case 0x45:
                await JoinLobby.Process(fPacket,session);
            break;
            case 0x46:
                buffer = await Writer.WriteBytes("BBHLLQ",LobbyCommandsClient.Disconnect,StatusCode.Success,339,session.unk_1,session.unk_2,(long)data[5]);
                FResponse fResponse = new(fPacket.channel,FClientOpcode.LobbyMessage, buffer);
                await ProxyReader.FinalizePacket(await fResponse.ToSend(),session);
            break;
        }
    }
}