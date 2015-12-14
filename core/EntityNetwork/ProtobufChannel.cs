using System;
using System.IO;
using ProtoBuf.Meta;
using TrackableData;
using TypeAlias;

namespace EntityNetwork
{
    public interface ByteChannel
    {
        void Write(byte[] bytes);
    }

    public class ProtobufChannelToClientZoneOutbound : IChannelToClientZone
    {
        public TypeAliasTable TypeTable;
        public TypeModel TypeModel;

        public ByteChannel OutboundChannel;

        private MemoryStream _stream;
        private BinaryWriter _writer;

        public void Begin()
        {
            _stream = new MemoryStream();
            _writer = new BinaryWriter(_stream);
        }

        public byte[] End()
        {
            _writer.Flush();
            var bytes = _stream.ToArray();
            _writer.Close();
            _writer = null;
            _stream = null;

            if (OutboundChannel != null)
                OutboundChannel.Write(bytes);

            return bytes;
        }

        public void Spawn(int entityId, Type protoTypeType, int ownerId, EntityFlags flags, ISpawnPayload payload)
        {
            _writer.Write((byte)1);
            _writer.Write(entityId);
            var typeAlias = TypeTable.GetAlias(protoTypeType);
            if (typeAlias == 0)
                throw new ArgumentException("Type of protoType doesn't have alias. Type: " + protoTypeType.FullName);
            _writer.Write(typeAlias);
            _writer.Write(ownerId);
            _writer.Write((byte)flags);
            ProtobufStreamHelper.WriteObject(_writer, payload, TypeTable, TypeModel);
        }

        public void Despawn(int entityId)
        {
            _writer.Write((byte)2);
            _writer.Write(entityId);
        }

        public void Invoke(int entityId, IInvokePayload payload)
        {
            _writer.Write((byte)3);
            _writer.Write(entityId);
            ProtobufStreamHelper.WriteObject(_writer, payload, TypeTable, TypeModel);
        }

        public void UpdateChange(int entityId, IUpdateChangePayload payload)
        {
            _writer.Write((byte)4);
            _writer.Write(entityId);
            ProtobufStreamHelper.WriteObject(_writer, payload, TypeTable, TypeModel);
        }

        public void OwnershipChange(int entityId, int ownerId)
        {
            _writer.Write((byte)5);
            _writer.Write(entityId);
            _writer.Write(ownerId);
        }
    }

    public class ProtobufChannelToClientZoneInbound : ByteChannel
    {
        public TypeAliasTable TypeTable;
        public TypeModel TypeModel;

        public IChannelToClientZone InboundClientZone;

        public void Write(byte[] bytes)
        {
            FromBytes(bytes, InboundClientZone);
        }

        public void FromBytes(byte[] bytes, IChannelToClientZone channelToClientZone)
        {
            using (var stream = new MemoryStream(bytes))
            using (var reader = new BinaryReader(stream))
            {
                while (stream.Position < stream.Length)
                {
                    var code = reader.ReadByte();
                    switch (code)
                    {
                        case 1:
                        {
                            var entityId = reader.ReadInt32();
                            var typeAlias = reader.ReadInt32();
                            var ownerId = reader.ReadInt32();
                            var flags = (EntityFlags)reader.ReadByte();
                            var payload = (ISpawnPayload)ProtobufStreamHelper.ReadObject(reader, TypeTable, TypeModel);
                            channelToClientZone.Spawn(entityId, TypeTable.GetType(typeAlias), ownerId, flags, payload);
                            break;
                        }
                        case 2:
                        {
                            var entityId = reader.ReadInt32();
                            channelToClientZone.Despawn(entityId);
                            break;
                        }
                        case 3:
                        {
                            var entityId = reader.ReadInt32();
                            var payload = (IInvokePayload)ProtobufStreamHelper.ReadObject(reader, TypeTable, TypeModel);
                            channelToClientZone.Invoke(entityId, payload);
                            break;
                        }
                        case 4:
                        {
                            var entityId = reader.ReadInt32();
                            var payload =
                                (IUpdateChangePayload)ProtobufStreamHelper.ReadObject(reader, TypeTable, TypeModel);
                            channelToClientZone.UpdateChange(entityId, payload);
                            break;
                        }
                        case 5:
                        {
                            var entityId = reader.ReadInt32();
                            var ownerId = reader.ReadInt32();
                            channelToClientZone.OwnershipChange(entityId, ownerId);
                            break;
                        }
                        default:
                            throw new Exception("Invalid code: " + code);
                    }
                }
            }
        }
    }

    public class ProtobufChannelToServerZoneOutbound : IChannelToServerZone
    {
        public TypeAliasTable TypeTable;
        public TypeModel TypeModel;

        public ByteChannel OutboundChannel;

        private MemoryStream _stream;
        private BinaryWriter _writer;

        public void Begin()
        {
            _stream = new MemoryStream();
            _writer = new BinaryWriter(_stream);
        }

        public byte[] End()
        {
            _writer.Flush();
            var bytes = _stream.ToArray();
            _writer.Close();
            _writer = null;
            _stream = null;

            if (OutboundChannel != null)
                OutboundChannel.Write(bytes);

            return bytes;
        }

        public void Invoke(int clientId, int entityId, IInvokePayload payload)
        {
            _writer.Write((byte)11);
            _writer.Write(entityId);
            ProtobufStreamHelper.WriteObject(_writer, payload, TypeTable, TypeModel);
        }

        public void UpdateChange(int clientId, int entityId, int trackableDataIndex, ITracker tracker)
        {
            _writer.Write((byte)12);
            _writer.Write(entityId);
            _writer.Write((byte)trackableDataIndex);
            ProtobufStreamHelper.WriteObject(_writer, tracker, TypeTable, TypeModel);
        }
    }

    public class ProtobufChannelToServerZoneInbound : ByteChannel
    {
        public TypeAliasTable TypeTable;
        public TypeModel TypeModel;

        public int ClientId;
        public IChannelToServerZone InboundServerZone;

        public void Write(byte[] bytes)
        {
            FromBytes(bytes, InboundServerZone);
        }

        public void FromBytes(byte[] bytes, IChannelToServerZone channelToServerZone)
        {
            using (var stream = new MemoryStream(bytes))
            using (var reader = new BinaryReader(stream))
            {
                while (stream.Position < stream.Length)
                {
                    var code = reader.ReadByte();
                    switch (code)
                    {
                        case 11:
                        {
                            var entityId = reader.ReadInt32();
                            var invokePayload = (IInvokePayload)ProtobufStreamHelper.ReadObject(
                                reader, TypeTable, TypeModel);
                            channelToServerZone.Invoke(ClientId, entityId, invokePayload);
                            break;
                        }
                        case 12:
                        {
                            var entityId = reader.ReadInt32();
                            var trackableDataIndex = reader.ReadByte();
                            var tracker = (ITracker)ProtobufStreamHelper.ReadObject(
                                reader, TypeTable, TypeModel);
                            channelToServerZone.UpdateChange(ClientId, entityId, trackableDataIndex, tracker);
                            break;
                        }
                        default:
                            throw new Exception("Invalid code: " + code);
                    }
                }
            }
        }
    }

    public static class ProtobufStreamHelper
    {
        public static void WriteObject(BinaryWriter writer, object obj, TypeAliasTable typeTable, TypeModel typeModel)
        {
            if (obj != null)
            {
                var pos = (int)writer.BaseStream.Position;
                writer.BaseStream.Seek(4, SeekOrigin.Current);
                var typeAlias = typeTable.GetAlias(obj.GetType());
                if (typeAlias == 0)
                    throw new ArgumentException("Type of object doesn't have alias. Type: " + obj.GetType().FullName);
                writer.Write(typeAlias);
                typeModel.Serialize(writer.BaseStream, obj);
                var posEnd = (int)writer.BaseStream.Position;
                writer.Seek(pos, SeekOrigin.Begin);
                writer.Write((posEnd - pos) - 4);
                writer.Seek(posEnd, SeekOrigin.Begin);
            }
            else
            {
                writer.Write(0);
            }
        }

        public static object ReadObject(BinaryReader reader, TypeAliasTable typeTable, TypeModel typeModel)
        {
            var length = reader.ReadInt32();
            if (length > 0)
            {
                var typeAlias = reader.ReadInt32();
                var type = typeTable.GetType(typeAlias);
                var obj = typeModel.Deserialize(reader.BaseStream, null, type, length - 4);
                return obj;
            }
            else
            {
                return null;
            }
        }
    }
}
