using System.Net.Http;
using System.Text;

namespace TerraBlind
{
	// The mod talks only to Decision Infra. Provider keys and provider-specific routing stay in the gateway.
	public static class DecisionGateway
	{
		public const string DefaultUrl = "http://127.0.0.1:8080/v1/systemone";
		public const string JevModel = "jev-latest";
		public const string LayaModel = "laya-multilingual";
		public const string DefaultModel = JevModel;

		public static string Url
		{
			get
			{
				string configured = Config.I?.DecisionGatewayUrl?.Trim();
				return string.IsNullOrEmpty(configured) ? DefaultUrl : configured;
			}
		}

		public static string Model
		{
			get
			{
				string configured = Config.I?.DecisionModel?.Trim();
				return string.IsNullOrEmpty(configured) ? DefaultModel : configured;
			}
		}

		public static string Quote(string value)
		{
			var sb = new StringBuilder("\"");
			foreach (char c in value ?? "")
			{
				if (c == '"' || c == '\\') sb.Append('\\').Append(c);
				else if (c == '\n' || c == '\r') sb.Append(' ');
				else sb.Append(c);
			}
			return sb.Append('"').ToString();
		}

		public static HttpRequestMessage Request(string body)
		{
			var request = new HttpRequestMessage(HttpMethod.Post, Url)
			{
				Content = new StringContent(body, Encoding.UTF8, "application/json"),
			};
			// Local loopback needs no token. This optional token keeps remote authenticated gateways possible.
			string token = System.Environment.GetEnvironmentVariable("DECISION_GATEWAY_TOKEN")?.Trim();
			if (!string.IsNullOrEmpty(token)) request.Headers.Add("Authorization", "Bearer " + token);
			return request;
		}
	}
}
