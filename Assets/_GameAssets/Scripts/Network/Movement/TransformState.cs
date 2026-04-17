using System;
using Unity.Netcode;
using UnityEngine;

public class TransformState : INetworkSerializable, IEquatable<TransformState>
{
    public int tick;
    public Vector3 position;
    public Quaternion rotation;
    public bool hasStartedMoving;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader) // If we are reading data, we need to read the values in the same order they were written
        {
            var reader = serializer.GetFastBufferReader();
            reader.ReadValueSafe(out tick);
            reader.ReadValueSafe(out position);
            reader.ReadValueSafe(out rotation);
            reader.ReadValueSafe(out hasStartedMoving);
        }
        else // If we are writing data, we need to write the values in a consistent order so they can be read correctly
        {
            var writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(tick);
            writer.WriteValueSafe(position);
            writer.WriteValueSafe(rotation);
            writer.WriteValueSafe(hasStartedMoving);
        }
    }

    public bool Equals(TransformState other)
    {
        return tick == other.tick
            && position == other.position
            && rotation.Equals(other.rotation)
            && hasStartedMoving == other.hasStartedMoving;
    }

    public override bool Equals(object obj)
    {
        return obj is TransformState other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(tick, position, rotation, hasStartedMoving);
    }
}
