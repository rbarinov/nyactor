using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace NYActor.EventSourcing;

public static class JsonSerializerConfig
{
    public static JsonSerializerSettings Settings { get; set; }

    static JsonSerializerConfig()
    {
        Settings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        Settings.Converters.Add(new StringEnumConverter(true));
    }
}
