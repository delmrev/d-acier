using System.Buffers.Binary;
using NLog;

public static class ConnectStartP2P
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public async static Task Process(FPacket fPacket, Session session)
    {
        var read = await Reader.TryReadBytes(fPacket.payload, "QBHcHc"); // Direct TCP connect
        if (read.success)
        {
            if(session.currentRoom != null)
            {
                if(session.currentRoom.Host==session){
                    var user = session.currentRoom.Users.FirstOrDefault(r => (long)read.output[0] == r.EugenID);
                    if(user != null)
                    {
                        Log.Info($"Direct TCP connect {(long)read.output[0]} -> {user.EugenID}");
                        BinaryPrimitives.WriteInt64BigEndian(fPacket.payload.AsSpan(0, 8), session.EugenID);
                        await ProxyReader.FinalizePacket(fPacket.ToSend(),user);
                    } else
                    {
                        Log.Error($"User dont found: {read.output[0]}");
                    }
                } else
                {
                    Log.Error("Try to send message to user but lobby is null");
                }
            }
        } else
        {
            using MemoryStream m = new(fPacket.payload);
            using BinaryReader reader = new(m);
            read = await Reader.TryReadBytes("QBBLHLH", reader); // STUN_INFO
            try
            {
                Reader.ReadBuf(16, reader);
            } catch
            {
                read.success = false;
            }
            if (read.success)
            {
                if (read.success)
                {
                    if(session.currentRoom != null)
                    {
                        var user = session.currentRoom.Users.FirstOrDefault(r => (long)read.output[0] == r.EugenID);
                        if(user != null)
                        {
                            Log.Info($"STUN_INFO {(long)read.output[0]} -> {user.EugenID}");
                            BinaryPrimitives.WriteInt64BigEndian(fPacket.payload.AsSpan(0, 8), session.EugenID);
                            await ProxyReader.FinalizePacket(fPacket.ToSend(),user);
                        } else
                        {
                            Log.Error($"User dont found: {read.output[0]}");
                        }
                    } else
                    {
                        Log.Error("Try to send message to user but lobby is null");
                    }
                }
            } else
            {
                read = await Reader.TryReadBytes(fPacket.payload, "QBQ"); //Steam Connect
                if (read.success)
                {
                    if(session.currentRoom != null)
                    {
                        if(session.currentRoom.Host==session){
                            var user = session.currentRoom.Users.FirstOrDefault(r => (long)read.output[0] == r.EugenID);
                            if(user != null)
                            {
                                Log.Info($"Steam connect {(long)read.output[0]} -> {user.EugenID}");
                                BinaryPrimitives.WriteInt64BigEndian(fPacket.payload.AsSpan(0, 8), session.EugenID);
                                await ProxyReader.FinalizePacket(fPacket.ToSend(),user);
                            } else
                            {
                                Log.Error($"User dont found: {read.output[0]}");
                            }
                        } else
                        {
                            Log.Error("Try to send message to user but lobby is null");
                        }
                    }
                }
            }
        }
    }
}