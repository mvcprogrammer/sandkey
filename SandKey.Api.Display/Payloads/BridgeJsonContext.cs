using System.Text.Json.Serialization;

namespace SandKey.Api.Display.Payloads;

/// <summary>
/// Source-generated serialization for the Bridge payloads. Generating the readers at compile
/// time keeps reflection off the Lambda cold path.
/// </summary>
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(BridgeResultPayload<BridgeListingPayload>))]
[JsonSerializable(typeof(BridgeResultPayload<List<BridgeListingPayload>>))]
internal sealed partial class BridgeJsonContext : JsonSerializerContext
{
}
