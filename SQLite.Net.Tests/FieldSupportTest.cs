using NUnit.Framework;

namespace SQLite.Net2.Tests
{
    public class FieldTestModel
    {
        [PrimaryKey, AutoIncrement]
        public int id;

        public string name;
    }
    
    [TestFixture]
    public class FieldSupportTest : BaseTest
    {
        [Test]
        public void CanCreateModelWithFields()
        {
            var db = new SQLiteConnection(TestPath.CreateTemporaryDatabase());
            var mapping = db.GetMapping<FieldTestModel>();
            
            Assert.That(mapping.Columns.Length, Is.EqualTo(2));
            Assert.That(mapping.Columns[0].Name, Is.EqualTo(nameof(FieldTestModel.id)));
            Assert.That(mapping.Columns[1].Name, Is.EqualTo(nameof(FieldTestModel.name)));

            db.CreateTable<FieldTestModel>();

            db.InsertAll(new[]
            {
                new FieldTestModel
                {
                    id = -1,
                    name = "hello"
                },
                new FieldTestModel
                {
                    id = -1,
                    name = "world"
                },
            });

            var model = db.Table<FieldTestModel>().Where(x => x.name == "hello").First();
            Assert.That(model.id, Is.Not.EqualTo(-1));
        }
    }
}
