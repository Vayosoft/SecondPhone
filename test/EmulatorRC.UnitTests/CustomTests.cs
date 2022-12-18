using System.Diagnostics;
using System.Text.Json.Serialization;
using MemoryPack;
using Newtonsoft.Json;
using Xunit.Abstractions;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace EmulatorRC.UnitTests;

public class CustomTests
{
    private readonly ITestOutputHelper _helper;


    public CustomTests(ITestOutputHelper helper)
    {
        _helper = helper;
    }


    [Fact]
    public void SerializeTest()
    {
        var counter = 1000000;
        var sw = new Stopwatch();

        var testEvent = new TestEvent
        {
            Id = -1,
            Name = "TestEvent",
            StartDate = DateTime.UtcNow,
            Departments = new List<string> { "Dep1", "Dep2" },
            Counters = new Dictionary<string, long> { { "us", 2 }, { "il", 20 } }
        };


        sw.Start();
        foreach (var i in Enumerable.Range(0, counter))
        {
            testEvent.Id = i;
            
            var serialized = MemoryPackSerializer.Serialize(testEvent);
            var source = MemoryPackSerializer.Deserialize<TestEvent>(serialized);
        }
        sw.Stop();

        var s = Math.Round(counter / sw.Elapsed.TotalSeconds, 2);
        _helper.WriteLine($"[MemoryPackSerializer] elapsed: {sw.Elapsed}, speed: {s}/sec");

       
        sw.Reset();
        sw.Start();
        foreach (var i in Enumerable.Range(0, counter))
        {
            testEvent.Id = i;

            var serialized = JsonSerializer.Serialize(testEvent);
            var source = JsonSerializer.Deserialize<TestEvent>(serialized);
        }
        sw.Stop();

        s = Math.Round(counter / sw.Elapsed.TotalSeconds, 2);
        _helper.WriteLine($"[Text.JSON] elapsed: {sw.Elapsed}, speed: {s}/sec");


        sw.Reset();
        sw.Start();
        foreach (var i in Enumerable.Range(0, counter))
        {
            testEvent.Id = i;

            var serialized = JsonConvert.SerializeObject(testEvent);
            var source = JsonConvert.DeserializeObject<TestEvent>(serialized);
        }
        sw.Stop();

        s = Math.Round(counter / sw.Elapsed.TotalSeconds, 2);
        _helper.WriteLine($"[NewtonsoftJson] elapsed: {sw.Elapsed}, speed: {s}/sec");

        /* [MemoryPackSerializer] elapsed: 00:00:00.9235076, speed: 1082828.12/sec
           [Text.JSON] elapsed: 00:00:02.4749351, speed: 404051/sec
           [NewtonsoftJson] elapsed: 00:00:04.8457659, speed: 206365.73/sec
         */





    }

    

}

[MemoryPackable]
public partial class TestEvent : BaseEvent
{
    [JsonProperty("sd"), JsonPropertyName("sd")]
    public DateTime StartDate { set; get; }

    [JsonProperty("dp"), JsonPropertyName("dp")]
    public List<string>? Departments { set; get; }

    [JsonProperty("c"), JsonPropertyName("c")]
    public Dictionary<string, long>? Counters { set; get; }
}

[MemoryPackable]
public partial class BaseEvent
{
    [JsonProperty("id"), JsonPropertyName("id")]
    public int Id { set; get; }

    [JsonProperty("n"), JsonPropertyName("n")]
    public string Name { init; get; } = string.Empty;
}