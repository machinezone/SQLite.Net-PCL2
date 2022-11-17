namespace SQLite.Net2
{
    public interface IColumnReader
    {
        int ColumnCount { get; }

        string GetColumnName(int col);
        
        bool ReadBoolean(int col);
        
        byte ReadByte(int col);
        sbyte ReadSByte(int col);
        
        short ReadInt16(int col);
        ushort ReadUInt16(int col);
        
        int ReadInt32(int col);
        uint ReadUInt32(int col);
        
        long ReadInt64(int col);
        ulong ReadUInt64(int col);

        float ReadSingle(int col);
        double ReadDouble(int col);

        string ReadString(int col);
    }
    
    public interface IColumnSerializer
    {
        void Deserialize(IColumnReader reader);
    }
}
