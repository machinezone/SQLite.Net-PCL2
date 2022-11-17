namespace SQLite.Net2
{
    public interface IColumnDeserializer
    {
        void Deserialize(IColumnReader reader);
    }
}