public static class GetRoomList
{
    public static async Task Process(FPacket fPacket, Session session)
    {
        session.has_EF = true;
        var buffer = await Writer.WriteBytes("H", 0);
        FResponse response = new(fPacket.channel, FClientOpcode.Brausing, buffer);
        await ProxyReader.FinalizePacket(await response.ToSend(), session); 
        buffer = await Writer.WriteBytes("LLLLLLLLLLLLLL", Global.GetPlayersCount(), Global.GetRoomsCount(), 0, 340, 442, 4, 0, 1, 0, 0, 0, 0, 0, 0);
        response = new(fPacket.channel, FClientOpcode.PublicInformation, buffer);
        await ProxyReader.FinalizePacket(await response.ToSend(), session); 
        response = new(fPacket.channel, FClientOpcode.BrausingMessageEnd, [0x00, 0x01]);
        await ProxyReader.FinalizePacket(await response.ToSend(), session);
    }
}