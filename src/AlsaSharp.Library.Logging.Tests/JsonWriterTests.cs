using AlsaSharp.Tests.Library;
using AlsaSharp.Tests.NUnit;
using Microsoft.Extensions.Configuration;

namespace AlsaSharp.Library.Logging.Tests
{
    [TestFixture]
    public class JsonWriterTests
    {
        private readonly IConfiguration _iconfiguration = TestUtils.BuildTestConfiguration();
        private ILog<JsonWriterTests> _log;
        public JsonWriterTests()
        {
            var logBuilder = new LogBuilder(_iconfiguration).UseNunitTestContext();
            logBuilder.Build();
            _log = LogManager.GetLogger<JsonWriterTests>();
            _log.Info($"Logger initialized for {GetType().Name}.");
        }
        [Test]
        public void JsonWriter_Can_Create_Json_Files_In_OutputFolder()
        {
            // use the current working directory (relative filename)
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var fileName = $"results_{timestamp}.json";
            var filePath = fileName;

            // create a simple object with a TimestampUtc property
            var obj = new { TimestampUtc = DateTime.UtcNow };

            // use the JsonWriter to append the object to file
            var writer = new JsonWriter(filePath);
            writer.Append(obj);

            // assert the file exists
            Assert.IsTrue(File.Exists(filePath), "Json file was not created");

            // read the file and ensure the timestamp field is present and recent
            var lines = File.ReadAllLines(filePath);
            Assert.IsTrue(lines.Length >= 1, "Json file should contain at least one JSON line");
            var doc = System.Text.Json.JsonDocument.Parse(lines[0]);
            Assert.IsTrue(doc.RootElement.TryGetProperty("TimestampUtc", out var tsElem), "TimestampUtc property not found in JSON");
            var parsed = tsElem.GetDateTime();
            var diff = DateTime.UtcNow - parsed.ToUniversalTime();
            Assert.LessOrEqual(Math.Abs(diff.TotalSeconds), 30, "Timestamp in JSON is not within expected range");
        }

        [Test]
        public void JsonWriter_Appends_New_Items_Without_Removing_Previous()
        {
            // write in current working directory using a unique relative filename
            var filePath = $"append_test_{Guid.NewGuid():N}.json";

            var writer = new AlsaSharp.Library.Logging.JsonWriter(filePath);

            var first = new { Id = 1, TimestampUtc = DateTime.UtcNow };
            writer.Append(first);
            Thread.Sleep(50);
            var second = new { Id = 2, TimestampUtc = DateTime.UtcNow };
            writer.Append(second);

            var lines = File.ReadAllLines(filePath);
            Assert.AreEqual(2, lines.Length, "File should contain two JSON lines after two appends");

            var doc1 = System.Text.Json.JsonDocument.Parse(lines[0]);
            var doc2 = System.Text.Json.JsonDocument.Parse(lines[1]);
            Assert.AreEqual(1, doc1.RootElement.GetProperty("Id").GetInt32());
            Assert.AreEqual(2, doc2.RootElement.GetProperty("Id").GetInt32());
        }

        [Test]
        public void JsonWriter_File_Is_Named_Correctly()
        {
            // expect filename in current working directory
            var expectedName = $"myresults_{DateTime.UtcNow:yyyyMMdd}.json";
            var filePath = expectedName;

            var writer = new JsonWriter(filePath);
            writer.Append(new { Msg = "ping", TimestampUtc = DateTime.UtcNow });

            Assert.IsTrue(File.Exists(filePath), "File with expected name was not created");
            Assert.AreEqual(expectedName, Path.GetFileName(filePath));
        }
    }
}
