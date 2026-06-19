public static class Continue
{
    public async static Task Process(FPacket fPacket, Session session)
    {
        var buffer = await Writer.WriteBytes("B", StatusCode.Success);
        FResponse fresponse = new(fPacket.channel,FClientOpcode.CONTINUE,buffer);
        await ProxyReader.FinalizePacket(await fresponse.ToSend(),session);
    }
}