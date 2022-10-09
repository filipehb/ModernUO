using System;

namespace Server.Custom.FaintSystem;

public class Faint
    : ISerializable
{
    //TODO Verificar implementação dessa interface ISerializable
    private DateTime _lastSerialized;
    private long _savePosition;
    private BufferWriter _saveBuffer;
    public int Faints { get; set; }
    public bool isRecoverFaintRunning { get; set; }

    public Faint(int faints, bool isRecoverFaintRunning)
    {
        Faints = faints;
        this.isRecoverFaintRunning = isRecoverFaintRunning;
    }

    public DateTime Created { get; set; }

    DateTime ISerializable.LastSerialized
    {
        get => _lastSerialized;
        set => _lastSerialized = value;
    }

    long ISerializable.SavePosition
    {
        get => _savePosition;
        set => _savePosition = value;
    }

    BufferWriter ISerializable.SaveBuffer
    {
        get => _saveBuffer;
        set => _saveBuffer = value;
    }

    public int TypeRef { get; }
    public Serial Serial { get; }

    public void BeforeSerialize()
    {
        throw new NotImplementedException();
    }

    public void Deserialize(IGenericReader reader)
    {
        throw new System.NotImplementedException();
    }

    public void Serialize(IGenericWriter writer)
    {
        throw new System.NotImplementedException();
    }

    public void Delete()
    {
        throw new NotImplementedException();
    }

    public bool Deleted { get; }
    public void SetTypeRef(Type type)
    {
        throw new NotImplementedException();
    }
}
