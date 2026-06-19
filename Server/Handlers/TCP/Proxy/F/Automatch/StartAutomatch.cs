public static class StartAutomatch
{
    public async static Task Process(FPacket fPacket, Session session)
    {
       await Global.AddToAutoMatch(session);
    }
}