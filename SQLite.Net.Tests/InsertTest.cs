using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;


namespace SQLite.Net2.Tests 
{
    [TestFixture]
    public class InsertTest : BaseTest
    {
        [SetUp]
        public void Setup()
        {
            _db = new TestDb(TestPath.CreateTemporaryDatabase());
        }

        [TearDown]
        public void TearDown()
        {
            if (_db != null)
            {
                _db.Close();
            }
        }

        private TestDb _db;

        public class TestObj
        {
            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }

            public String Text { get; set; }

            public override string ToString()
            {
                return string.Format("[TestObj: Id={0}, Text={1}]", Id, Text);
            }
        }

        public class TestObj2
        {
            [PrimaryKey]
            public int Id { get; set; }

            public String Text { get; set; }

            public override string ToString()
            {
                return string.Format("[TestObj: Id={0}, Text={1}]", Id, Text);
            }
        }

        public class OneColumnObj
        {
            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }
        }

        public class UniqueObj
        {
            [PrimaryKey]
            public int Id { get; set; }
        }

        public class TestDb : SQLiteConnection
        {
            public TestDb(String path)
                : base(path)
            {
                CreateTable<TestObj>();
                CreateTable<TestObj2>();
                CreateTable<OneColumnObj>();
                CreateTable<UniqueObj>();
            }
        }

        [Test]
        public void InsertALot()
        {
            int n = 10000;
            IEnumerable<TestObj> q = from i in Enumerable.Range(1, n)
                select new TestObj
                {
                    Text = "I am"
                };
            TestObj[] objs = q.ToArray();
            _db.TraceListener = DebugTraceListener.Instance;

            var sw = new Stopwatch();
            sw.Start();

            int numIn = _db.InsertAll(objs);

            sw.Stop();

            Assert.AreEqual(numIn, n, "Num inserted must = num objects");

            TestObj[] inObjs = _db.CreateCommand("select * from TestObj").ExecuteQuery<TestObj>().ToArray();

            for (int i = 0; i < inObjs.Length; i++)
            {
                Assert.AreEqual(i + 1, objs[i].Id);
                Assert.AreEqual(i + 1, inObjs[i].Id);
                Assert.AreEqual("I am", inObjs[i].Text);
            }

            var numCount = _db.CreateCommand("select count(*) from TestObj").ExecuteScalar<int>();

            Assert.AreEqual(numCount, n, "Num counted must = num objects");
        }

        [Test]
        public void InsertAllFailureInsideTransaction()
        {
            var testObjects = Enumerable.Range(1, 20).Select(i => new UniqueObj { Id = i }).ToList();
            testObjects[^1].Id = 1; // causes the insert to fail because of duplicate key

            ExceptionAssert.Throws<SQLiteException>(() => _db.RunInTransaction(db => { db.InsertAll(testObjects, false); }));
            Assert.AreEqual(0, _db.Table<UniqueObj>().Count());
        }

        [Test]
        public void InsertAllFailureInTransaction2()
        {
            var testObjects = Enumerable.Range(1, 20).Select(i => new UniqueObj { Id = i }).ToList();
            testObjects[^1].Id = 1; // causes the insert to fail because of duplicate key

            ExceptionAssert.Throws<SQLiteException>(() => _db.InsertAll(testObjects, true));

            Assert.AreEqual(0, _db.Table<UniqueObj>().Count());
        }

        [Test]
        public void InsertAllFailureInTransaction3()
        {
            var testObjects = Enumerable.Range(1, 20).Select(i => new UniqueObj { Id = i }).ToList();
            testObjects[^1].Id = 1; // causes the insert to fail because of duplicate key

            ExceptionAssert.Throws<SQLiteException>(() =>
            {
                _db.BeginTransaction();
                _db.InsertAll(testObjects);
                _db.Commit();
            });

            Assert.AreEqual(0, _db.Table<UniqueObj>().Count());
        }

        [Test]
        public void InsertAllFailureSucceedsOutsideTransaction()
        {
            _db.DeleteAll<UniqueObj>();
            var testObjects = Enumerable.Range(1, 20).Select(i => new UniqueObj { Id = i }).ToList();
            testObjects[^1].Id = 1; // causes the last insert to fail because of duplicate key, but will let all previous insert in the db

            ExceptionAssert.Throws<SQLiteException>(() => _db.InsertAll(testObjects, false));

            Assert.AreEqual(19, _db.Table<UniqueObj>().Count());
        }

        [Test]
        public void InsertAllSuccessInsideTransaction()
        {
            var testObjects = Enumerable.Range(1, 20).Select(i => new UniqueObj { Id = i }).ToList();
            _db.RunInTransaction(db => { db.InsertAll(testObjects); });

            Assert.AreEqual(testObjects.Count, _db.Table<UniqueObj>().Count());
        }

        [Test]
        public void InsertAllSuccessOutsideTransaction()
        {
            List<UniqueObj> testObjects = Enumerable.Range(1, 20).Select(i => new UniqueObj
            {
                Id = i
            }).ToList();

            _db.InsertAll(testObjects);

            Assert.AreEqual(testObjects.Count, _db.Table<UniqueObj>().Count());
        }

        [Test]
        public void InsertIntoOneColumnAutoIncrementTable()
        {
            var obj = new OneColumnObj();
            _db.Insert(obj);

            var result = _db.Get<OneColumnObj>(1);
            Assert.AreEqual(1, result.Id);
        }

        [Test]
        public void InsertIntoTwoTables()
        {
            var obj1 = new TestObj
            {
                Text = "GLaDOS loves testing!"
            };
            var obj2 = new TestObj2
            {
                Text = "Keep testing, just keep testing"
            };

            int numIn1 = _db.Insert(obj1);
            Assert.AreEqual(1, numIn1);
            int numIn2 = _db.Insert(obj2);
            Assert.AreEqual(1, numIn2);

            List<TestObj> result1 = _db.Query<TestObj>("select * from TestObj").ToList();
            Assert.AreEqual(numIn1, result1.Count);
            Assert.AreEqual(obj1.Text, result1.First().Text);

            List<TestObj> result2 = _db.Query<TestObj>("select * from TestObj2").ToList();
            Assert.AreEqual(numIn2, result2.Count);
        }

        [Test]
        public void InsertOrReplace()
        {
            _db.TraceListener = DebugTraceListener.Instance;
            _db.InsertAll(from i in Enumerable.Range(1, 20)
                select new TestObj
                {
                    Text = "#" + i
                });

            Assert.AreEqual(20, _db.Table<TestObj>().Count());

            var t = new TestObj
            {
                Id = 5,
                Text = "Foo",
            };
            _db.InsertOrReplace(t);

            List<TestObj> r = (from x in _db.Table<TestObj>() orderby x.Id select x).ToList();
            Assert.AreEqual(20, r.Count);
            Assert.AreEqual("Foo", r[4].Text);
        }

        [Test]
        public void InsertOrIgnore()
        {
            _db.TraceListener = DebugTraceListener.Instance;
            _db.InsertOrIgnoreAll(from i in Enumerable.Range(1, 20)
                select new TestObj2
                {
                    Id = i,
                    Text = "#" + i
                });

            Assert.AreEqual(20, _db.Table<TestObj2>().Count());

            var t = new TestObj2
            {
                Id = 5,
                Text = "Foo",
            };
            _db.InsertOrIgnore(t);

            List<TestObj2> r = (from x in _db.Table<TestObj2>() orderby x.Id select x).ToList();
            Assert.AreEqual(20, r.Count);
            Assert.AreEqual("#5", r[4].Text);
        }

        [Test]
        public void InsertTwoTimes()
        {
            var obj1 = new TestObj
            {
                Text = "GLaDOS loves testing!"
            };
            var obj2 = new TestObj
            {
                Text = "Keep testing, just keep testing"
            };


            int numIn1 = _db.Insert(obj1);
            int numIn2 = _db.Insert(obj2);
            Assert.AreEqual(1, numIn1);
            Assert.AreEqual(1, numIn2);

            List<TestObj> result = _db.Query<TestObj>("select * from TestObj").ToList();
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(obj1.Text, result[0].Text);
            Assert.AreEqual(obj2.Text, result[1].Text);
        }

        [Test]
        public void InsertWithExtra()
        {
            var obj1 = new TestObj2
            {
                Id = 1,
                Text = "GLaDOS loves testing!"
            };
            var obj2 = new TestObj2
            {
                Id = 1,
                Text = "Keep testing, just keep testing"
            };
            var obj3 = new TestObj2
            {
                Id = 1,
                Text = "Done testing"
            };

            _db.Insert(obj1);


            try
            {
                _db.Insert(obj2);
                Assert.Fail("Expected unique constraint violation");
            }
            catch (SQLiteException)
            {
            }
            _db.Insert(obj2, "OR REPLACE");


            try
            {
                _db.Insert(obj3);
                Assert.Fail("Expected unique constraint violation");
            }
            catch (SQLiteException)
            {
            }
            _db.Insert(obj3, "OR IGNORE");

            List<TestObj> result = _db.Query<TestObj>("select * from TestObj2").ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(obj2.Text, result.First().Text);
        }
    }
}