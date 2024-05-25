using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace SQLite.Net2.Tests
{
    [TestFixture]
    public class SerializeTest : BaseTest
    {
        public class SerializeTestObj
        {
            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }

            public String Text { get; set; }

            public override string ToString()
            {
                return string.Format("[TestObj: Id={0}, Text={1}]", Id, Text);
            }
        }

        public class SerializeTestDb : SQLiteConnection
        {
            public SerializeTestDb(String path) : base(path)
            {
                CreateTable<SerializeTestObj>();
            }
        }

        [Test]
        public void SerializeRoundTrip()
        {
            var obj1 = new SerializeTestObj
            {
                Text = "GLaDOS loves testing!"
            };

            SQLiteConnection srcDb = new SerializeTestDb(":memory:");

            var numIn1 = srcDb.Insert(obj1);
            Assert.AreEqual(1, numIn1);

            List<SerializeTestObj> result1 = srcDb.Query<SerializeTestObj>("select * from SerializeTestObj").ToList();
            Assert.AreEqual(numIn1, result1.Count);
            Assert.AreEqual(obj1.Text, result1.First().Text);


            var serialized = srcDb.Serialize();
            srcDb.Close();

            SQLiteConnection destDb = new SerializeTestDb(":memory");
            destDb.Deserialize(serialized);

            result1 = destDb.Query<SerializeTestObj>("select * from SerializeTestObj").ToList();
            Assert.AreEqual(numIn1, result1.Count);
            Assert.AreEqual(obj1.Text, result1.First().Text);

            destDb.Close();
        }

        [Test]
        public void SerializeRoundTripStreams()
        {
            var obj1 = new SerializeTestObj
            {
                Text = "GLaDOS loves testing!"
            };

            SQLiteConnection srcDb = new SerializeTestDb(":memory:");

            var numIn1 = srcDb.Insert(obj1);
            Assert.AreEqual(1, numIn1);

            List<SerializeTestObj> result1 = srcDb.Query<SerializeTestObj>("select * from SerializeTestObj").ToList();
            Assert.AreEqual(numIn1, result1.Count);
            Assert.AreEqual(obj1.Text, result1.First().Text);


            MemoryStream stream = new();
            var size = srcDb.Serialize(stream);
            Assert.That(size, Is.GreaterThan(0));
            srcDb.Close();

            stream.Seek(0, SeekOrigin.Begin);

            SQLiteConnection destDb = new SerializeTestDb(":memory");
            destDb.Deserialize(stream);
            Assert.That(stream.Position, Is.EqualTo(size));

            result1 = destDb.Query<SerializeTestObj>("select * from SerializeTestObj").ToList();
            Assert.AreEqual(numIn1, result1.Count);
            Assert.AreEqual(obj1.Text, result1.First().Text);

            destDb.Close();
        }
    }
}
