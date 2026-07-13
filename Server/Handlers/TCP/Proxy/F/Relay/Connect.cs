using System.Buffers.Binary;
using EugnetProtocol.Common.Interfaces;
using NLog;
namespace EugnetProtocol.TCP.Proxy.F
{
    public class ConnectStartP2P : IFPacketHandler
    {
        private ConfigData config = GlobalManager.Instance.Data;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public async Task Process(FPacket fPacket, Session session)
        {
            var read = await Reader.TryReadBytes(fPacket.payload, "QBHcHc"); // Direct TCP connect
            if (read.success)
            {
                if(session.currentRoom != null)
                {
                    Session? user = null;
                    long eugid = (long)read.output[0];
                    foreach (var users in session.currentRoom.Users)
                    {
                        if (users.Value.EugenID == eugid)
                        {
                            user = users.Value;
                            break;
                        }
                    }
                    if(user != null)
                    {
                        if (config != null && config.Logging.EnableDebug)
                        {
                            Log.Info($"Direct TCP connect {session.EugenID} -> {user.EugenID}");
                        }
                        BinaryPrimitives.WriteInt64BigEndian(fPacket.payload.AsSpan(0, 8), session.EugenID);
                        await user.Send(await fPacket.ToSend());
                    } else
                    {
                        Log.Error($"User dont found: {read.output[0]}");
                    }
                }  else
                {
                    Log.Error("Try to send message to user but lobby is null");
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
                            Session? user = null;
                            long eugid = (long)read.output[0];
                            foreach (var users in session.currentRoom.Users)
                            {
                                if (users.Value.EugenID == eugid)
                                {
                                    user = users.Value;
                                    break;
                                }
                            }
                            if(user != null)
                            {
                                if (config != null && config.Logging.EnableDebug)
                                {
                                    Log.Info($"STUN_INFO {session.EugenID} -> {user.EugenID}");
                                }
                                BinaryPrimitives.WriteInt64BigEndian(fPacket.payload.AsSpan(0, 8), session.EugenID);
                                await user.Send(await fPacket.ToSend());
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
                            Session? user = null;
                            long eugid = (long)read.output[0];
                            foreach (var users in session.currentRoom.Users)
                            {
                                if (users.Value.EugenID == eugid)
                                {
                                    user = users.Value;
                                    break;
                                }
                            }
                            if(user != null)
                            {
                                if (config != null && config.Logging.EnableDebug)
                                {
                                    Log.Info($"Steam connect {session.EugenID} -> {user.EugenID}");
                                }
                                BinaryPrimitives.WriteInt64BigEndian(fPacket.payload.AsSpan(0, 8), session.EugenID);
                                await user.Send(await fPacket.ToSend());
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