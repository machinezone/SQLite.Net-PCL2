using NUnit.Framework;

namespace SQLite.Net2.Tests
{
    public class TestColumnSerializerModel : IColumnSerializer
    {
        [PrimaryKey]
        public int Id { get; set; }
        
        public int ShouldBeSet { get; set; }

        public int ShouldNotBeSet { get; set; }
        
        public void Deserialize(IColumnReader reader)
        {
            Id = reader.ReadInt32(0);
            ShouldBeSet = reader.ReadInt32(1);
        }
    }
    
    [TestFixture]
    public class ColumnSerializerTest : BaseTest
    {
        [Test]
        public void CanHandleCustomDeserialization()
        {
            var db = new SQLiteConnection(TestPath.CreateTemporaryDatabase());
            db.CreateTable<TestColumnSerializerModel>();

            db.Insert(new TestColumnSerializerModel
            {
                Id = 1,
                ShouldBeSet = 2,
                ShouldNotBeSet = 3
            });

            var entry = db.Table<TestColumnSerializerModel>().First();
            
            Assert.That(entry.Id, Is.EqualTo(1));
            Assert.That(entry.ShouldBeSet, Is.EqualTo(2));
            Assert.That(entry.ShouldNotBeSet, Is.Not.EqualTo(3));
        }
    }
}
