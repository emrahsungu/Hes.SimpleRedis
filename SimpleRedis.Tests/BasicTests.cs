using Hes.SimpleRedis;
using NUnit.Framework;

namespace SimpleRedis.Tests
{
    [TestFixture]
    public class BasicTests
    {
        [Test]
        public void Connect()
        {
            // note: defaults to 127.0.0.1:6379
            using(dynamic client = new SimpleRedisClient())
            {
            }
        }

        [Test]
        public void GetSet()
        {
            using (dynamic client = new SimpleRedisClient())
            {
                client.del("Get Set"); // wipe
                client.set("Get Set", "emosh"); // assign
                string val = client.get("Get Set"); // fetch
                Assert.AreEqual("emosh", val);
            }
        }

        [Test]
        public void Counters()
        {
            using (dynamic client = new SimpleRedisClient())
            {
                client.del("counter"); // note: missing counts as 0
                int a = client.incr("counter"); // 0+1: should be 1
                int b = client.incrby("counter", 5); // 1+5, should be 6
                int c = client.decr("counter"); // 6-1: should be 5

                Assert.AreEqual(1, a, "a");
                Assert.AreEqual(6, b, "b");
                Assert.AreEqual(5, c, "c");
            }
        }

        [Test]
        public void DeleteResult()
        {
            using (dynamic client = new SimpleRedisClient())
            {
                client.set("delete", "some val"); // give it a value initially
                bool first = client.del("delete"); // was deleted: true
                bool second = client.del("delete"); // no longer existed: false

                Assert.IsTrue(first);
                Assert.IsFalse(second);
            }
        }

        [Test, ExpectedException(typeof(SimpleRedisException), ExpectedMessage = "ERR value is not an integer or out of range")]
        public void DoomedToFail()
        {
            using (dynamic client = new SimpleRedisClient())
            {
                client.set("fail_incr", "some val"); // give it a value initially
                client.incr("fail_incr"); // what is numeric "some val" + 1 ?
            }
        }

        [Test]
        public void Lists()
        {
            using (dynamic client = new SimpleRedisClient())
            {
                client.del("list");
                client.rpush("list", "item 1");
                client.rpush("list", "item 2");

                string a = client.lpop("list");
                string b = client.lpop("list");
                string c = client.lpop("list");

                client.rpush("list", "item 3");
                string d = client.lpop("list");

                Assert.AreEqual(a, "item 1");
                Assert.AreEqual(b, "item 2");
                Assert.IsNull(c);
                Assert.AreEqual(d, "item 3");
            }
        }
    }
}