using Newtonsoft.Json;
using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace ProtectedResource.IntegrationTests
{
    public abstract class TableManagerTestBase
        : TestBase
    {
        private readonly Random _random;
        
        protected TableManagerTestBase()
        {
            _random = new Random();
        }

        protected const int PrimaryKey = 5002; //This is in the database already

        protected RudimentaryEntity[] GetRandomObjectRequests(int count = 10)
        {
            var arr = new RudimentaryEntity[count];

            for (var i = 0; i < count; i++)
            {
                arr[i] = new RudimentaryEntity
                {
                    PrimaryKey = PrimaryKey,
                    DollarAmount = Convert.ToDecimal(_random.NextDouble()),
                    ForeignKey = _random.Next(),
                    IsYes = _random.Next() % 2 == 0,
                    Label = GetNextString(),
                    LuckyNumber = _random.Next(),
                    MathCalculation = _random.NextDouble(),
                    ReferenceId = Guid.NewGuid(),
                    RightNow = DateTime.UtcNow
                };
            }

            return arr;
        }

        protected string[] GetRandomJsonRequests(int count = 10)
        {
            var arrObj = GetRandomObjectRequests(count);

            var arr = arrObj.Select(JsonConvert.SerializeObject).ToArray();

            return arr;
        }

        private string GetNextString()
        {
            var arr = new char[10];

            for (var i = 0; i < arr.Length; i++)
            {
                arr[i] = (char)_random.Next(255);
            }
            
            var str = new string(arr);

            return str;
        }

        protected void AssertAreEqual(string expected, string actual)
        {
            var jExpected = JObject.Parse(expected);
            var jActual = JObject.Parse(actual);

            var message = $"Expected: {expected}{Environment.NewLine}Actual: {actual}";

            Assert.IsTrue(JToken.DeepEquals(jExpected, jActual), message);
        }

        protected void AssertAreEqual(JObject expected, JObject actual)
        {
            var jExpected = expected.ToString(Formatting.None);
            var jActual = actual.ToString(Formatting.None);

            var message = $"Expected: {jExpected}{Environment.NewLine}Actual: {jActual}";

            Assert.IsTrue(JToken.DeepEquals(expected, actual), message);
        }
    }
}
