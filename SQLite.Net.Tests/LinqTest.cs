using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;


namespace SQLite.Net2.Tests 
{
    [TestFixture]
    public class LinqTest : BaseTest
    {
        private TestDb CreateDb()
        {
            var db = new TestDb();
            db.CreateTable<Product>();
            db.CreateTable<KeyedProduct>(); 
            db.CreateTable<Order>();
            db.CreateTable<OrderLine>();
            db.CreateTable<OrderHistory>();
            return db;
        }

        public class Issue96_A
        {
            [AutoIncrement, PrimaryKey]
            public int ID { get; set; }

            public string AddressLine { get; set; }

            [Indexed]
            public int? ClassB { get; set; }

            [Indexed]
            public int? ClassC { get; set; }
        }

        public class Issue96_B
        {
            [AutoIncrement, PrimaryKey]
            public int ID { get; set; }

            public string CustomerName { get; set; }
        }

        public class Issue96_C
        {
            [AutoIncrement, PrimaryKey]
            public int ID { get; set; }

            public string SupplierName { get; set; }
        }

        [Test]
        public void FindWithExpression()
        {
            TestDb db = CreateDb();

            var r = db.Find<Product>(x => x.Price == 10);
            Assert.IsNull(r);
        }

        [Test]
        public void FunctionParameter()
        {
            TestDb db = CreateDb();

            db.Insert(new Product
            {
                Name = "A",
                Price = 20,
            });

            db.Insert(new Product
            {
                Name = "B",
                Price = 10,
            });

            Func<decimal, List<Product>> GetProductsWithPriceAtLeast =
                delegate(decimal val) { return (from p in db.Table<Product>() where p.Price > val select p).ToList(); };

            List<Product> r = GetProductsWithPriceAtLeast(15);
            Assert.AreEqual(1, r.Count);
            Assert.AreEqual("A", r[0].Name);
        }

        [Test]
        public void GetWithExpression()
        {
            TestDb db = CreateDb();

            db.Insert(new Product
            {
                Name = "A",
                Price = 20,
            });

            db.Insert(new Product
            {
                Name = "B",
                Price = 10,
            });

            db.Insert(new Product
            {
                Name = "C",
                Price = 5,
            });

            Assert.AreEqual(3, db.Table<Product>().Count());

            var r = db.Get<Product>(x => x.Price == 10);
            Assert.IsNotNull(r);
            Assert.AreEqual("B", r.Name);
        }

        [Test]
        public void Issue96_NullabelIntsInQueries()
        {
            TestDb db = CreateDb();
            db.CreateTable<Issue96_A>();

            int id = 42;

            db.Insert(new Issue96_A
            {
                ClassB = id,
            });
            db.Insert(new Issue96_A
            {
                ClassB = null,
            });
            db.Insert(new Issue96_A
            {
                ClassB = null,
            });
            db.Insert(new Issue96_A
            {
                ClassB = null,
            });


            Assert.AreEqual(1, db.Table<Issue96_A>().Where(p => p.ClassB == id).Count());
            Assert.AreEqual(3, db.Table<Issue96_A>().Where(p => p.ClassB == null).Count());
        }

        [Test]
        public void OrderByKey()
        {
            TestDb db = CreateDb();

            var products = new List<KeyedProduct>()
            {
                new ()
                {
                    Id = ("FOO", 1),
                    Name = "A",
                    TotalSales = 1,
                },
                new()
                {
                    Id = ("FOO", 2),
                    Name = "C",
                    TotalSales = 5,
                },
                new()
                {
                    Id = ("FOO",3),
                    Name = "D",
                    TotalSales = 400,
                },
                new()
                {
                    Id = ("FOO", 10),
                    Name = "B",
                    TotalSales = 100,
                },
            
            };
            foreach (var product in products)
            {
                db.Insert(product);
            }

            List<KeyedProduct> asc = (from p in db.Table<KeyedProduct>().OrderByKey() select p).ToList();
            Assert.That(products, Is.EquivalentTo(asc));

            List<KeyedProduct> desc = (from p in db.Table<KeyedProduct>().OrderByKeyDescending() select p).ToList();
            products.Reverse();
            Assert.That(products, Is.EquivalentTo(desc));
        }
        
        [Test]
        public void OrderByCast()
        {
            TestDb db = CreateDb();

            db.Insert(new Product
            {
                Name = "A",
                TotalSales = 1,
            });
            db.Insert(new Product
            {
                Name = "B",
                TotalSales = 100,
            });

            List<Product> nocast = (from p in db.Table<Product>() orderby p.TotalSales descending select p).ToList();
            Assert.AreEqual(2, nocast.Count);
            Assert.AreEqual("B", nocast[0].Name);

            List<Product> cast = (from p in db.Table<Product>() orderby (int) p.TotalSales descending select p).ToList();
            Assert.AreEqual(2, cast.Count);
            Assert.AreEqual("B", cast[0].Name);
        }

        [Test]
        public void WhereGreaterThan()
        {
            TestDb db = CreateDb();

            db.Insert(new Product
            {
                Name = "A",
                Price = 20,
            });

            db.Insert(new Product
            {
                Name = "B",
                Price = 10,
            });

            Assert.AreEqual(2, db.Table<Product>().Count());

            List<Product> r = (from p in db.Table<Product>() where p.Price > 15 select p).ToList();
            Assert.AreEqual(1, r.Count);
            Assert.AreEqual("A", r[0].Name);
        }

        public class Issue303_A
        {
            [PrimaryKey, NotNull]
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class Issue303_B
        {
            [PrimaryKey, NotNull]
            public int Id { get; set; }
            public bool Flag { get; set; }
        }

        [Test]
        public void Issue303_WhereNot_A()
        {
            using (var db = new TestDb())
            {
                db.CreateTable<Issue303_A>();
                db.Insert(new Issue303_A { Id = 1, Name = "aa" });
                db.Insert(new Issue303_A { Id = 2, Name = null });
                db.Insert(new Issue303_A { Id = 3, Name = "test" });
                db.Insert(new Issue303_A { Id = 4, Name = null });

                var r = (from p in db.Table<Issue303_A>() where !(p.Name == null) select p).ToList();
                Assert.AreEqual(2, r.Count);
                Assert.AreEqual(1, r[0].Id);
                Assert.AreEqual(3, r[1].Id);
            }
        }

        [Test]
        public void Issue303_WhereNot_B()
        {
            using (var db = new TestDb())
            {
                db.CreateTable<Issue303_B>();
                db.Insert(new Issue303_B { Id = 1, Flag = true });
                db.Insert(new Issue303_B { Id = 2, Flag = false });
                db.Insert(new Issue303_B { Id = 3, Flag = true });
                db.Insert(new Issue303_B { Id = 4, Flag = false });

                var r = (from p in db.Table<Issue303_B>() where !p.Flag select p).ToList();
                Assert.AreEqual(2, r.Count);
                Assert.AreEqual(2, r[0].Id);
                Assert.AreEqual(4, r[1].Id);
            }
        }
    }
}
