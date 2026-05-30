public static class MMS
{
    public static async Task Process(Session session)
    {
            FResponse fResponce = new(1, FClientOpcode.MMS_MSG_INIT, Writer.WriteBytes("IBQS", 71, 0x00, session.EugenID, DReader.GetMMSJson(session)));
            fResponce.payload.AddRange(new byte[4]);
            byte[] key = new byte[128];
            Random random = new();
            random.NextBytes(key);
            fResponce.payload.AddRange(key);
                
            await ProxyReader.FinalizePacket(fResponce.ToSend(), session);
    }
}