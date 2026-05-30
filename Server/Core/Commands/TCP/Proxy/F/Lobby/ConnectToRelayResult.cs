public static class Continue
{
    public async static Task Process(FPacket fPacket, Session session)
    {
        var buffer = Writer.WriteBytes("B", StatusCode.SUCCESS);
        FResponse fresponse = new(fPacket.channel,FClientOpcode.CONTINUE,buffer);
        await ProxyReader.FinalizePacket(fresponse.ToSend(),session);
    }
}