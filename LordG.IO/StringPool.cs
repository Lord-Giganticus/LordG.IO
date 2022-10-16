namespace LordG.IO;

public enum StringPoolFormat
{
    Null_Terminated,
    Not_Null_Terminated
}

public class StringPool
{
    public bool mLookUp;
    public List<byte> mBuffer = new();
    public StringPoolFormat mFormat;
    public Dictionary<string, int> mOffsets = new();
    public uint Size => (uint)mBuffer.Count;
    public StringPool(StringPoolFormat format)
    {
        mFormat = format;
    }
    public string Pack(string msg)
    {
        if (mFormat is StringPoolFormat.Null_Terminated && !msg.EndsWith("\0"))
            return msg + "\0";
        return msg;
    }
    public int Write(string msg)
    {
        int offset;
        msg = Pack(msg);
        if (mLookUp && mOffsets.TryGetValue(msg, out var num))
            offset = num;
        else
        {
            offset = mBuffer.Count;
            mOffsets[msg] = offset;
            for (int i = 0; i < msg.Length; i++)
                mBuffer.Add((byte)msg[i]);
        }
        return offset;
    }
    public bool Find(string msg, out int offset)
    {
        return mOffsets.TryGetValue(msg, out offset);
    }
    public void Align32()
    {
        while ((mBuffer.Count % 32) != 0)
            mBuffer.Add(0x0);
    }
}
