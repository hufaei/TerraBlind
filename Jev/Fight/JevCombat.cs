using System.Net.Http;
using Terraria;

namespace TerraBlind
{
	// 真正问 Jev 的 brain。【后台发请求,主线程拿上一次的结果】-- 形状照 TrapEscape,
	// 游戏主线程等 70ms 就是掉帧
	public class JevCombat : ICombatBrain
	{
		const long ResultTtlMs = 5000;
		static readonly HttpClient _http = new() { Timeout = System.TimeSpan.FromSeconds(5) };
		static readonly CombatBaseline _fallback = new();
		static volatile bool _busy;
		sealed class Pending
		{
			public int Version;
			public int LatencyMs;
			public string Body;
		}
		static volatile Pending _pending;    // 后台写完整一份才赋值
		static string _wantedState;
		static int _wantedVersion;
		static int _sentVersion;
		static long _retryAt;
		static CombatCall _last;
		static bool _haveLast;
		static long _lastAt = -ResultTtlMs;
		static int _sent, _failed;
		static readonly System.Diagnostics.Stopwatch _clock = System.Diagnostics.Stopwatch.StartNew();

		public static string Stats => $"发了{_sent}次 失败{_failed}次 {DecisionGateway.Model} via gateway";

		// 问题措辞固定不变,写死在这儿好让人审。criteria 的 key 和 CombatAct 一一对应
		static string Body(string state)
			=> "{\"model\":" + DecisionGateway.Quote(DecisionGateway.Model) + ",\"state\":" + DecisionGateway.Quote(state) + ",\"questions\":{"
			 + "\"act\":{\"type\":\"choice\",\"instructions\":"
			 + "\"泰拉瑞亚里一个自动玩家正在赶路,附近出现了敌人。当前装备和武器只以 capabilities 字段为准。"
			 + "根据敌人的威胁和它自己的血量,选它现在最该做的一件事。\",\"criteria\":{"
			 + "\"Fight\":\"转身打。敌人够得着、打得过,或者不打就会一直挨打\","
			 + "\"Ignore\":\"不理它,继续赶路。太远、或者威胁小到不值得停下来\","
			 + "\"Flee\":\"走开。血少了,或者这群敌人打不过\","
			 + "\"WallOff\":\"用方块把自己封起来躲开\","
			 + "\"Heal\":\"先回血\"}},"
			 + "\"interrupt_placement\":{\"type\":\"noul\",\"instructions\":"
			 + "\"这个威胁大到值得中断手上正在进行的方块放置吗?放置中断会留下半截工程。\"}"
			 + "}}";

		public CombatCall Decide(Player p, int tcx, int tcy, int dist, bool workBusy)
		{
			ConsumePending();
			_wantedState = Facts(p, tcx, tcy, dist, workBusy);
			_wantedVersion++;
			_haveLast = false; // 旧现场的结论不用在新现场
			StartLatest();
			return CurrentOrFallback(p, tcx, tcy, dist, workBusy);
		}

		// Combat 每帧调用这个来收上一个异步结果。不等 HTTP,也不在现场没变时重复发请求。
		public CombatCall Poll(Player p, int tcx, int tcy, int dist, bool workBusy)
		{
			ConsumePending();
			if (_haveLast && _clock.ElapsedMilliseconds - _lastAt > ResultTtlMs)
			{
				_wantedState = Facts(p, tcx, tcy, dist, workBusy);
				_wantedVersion++;
				_haveLast = false;
			}
			StartLatest(); // 忙时若又收到新现场,上一请求结束后补发最新一份
			return CurrentOrFallback(p, tcx, tcy, dist, workBusy);
		}

		static CombatCall CurrentOrFallback(Player p, int tcx, int tcy, int dist, bool workBusy)
			=> _haveLast && _clock.ElapsedMilliseconds - _lastAt <= ResultTtlMs
				? _last
				: _fallback.Decide(p, tcx, tcy, dist, workBusy);

		static void ConsumePending()
		{
			var done = _pending;
			if (done == null) return;
			_pending = null;
			// 请求在飞时现场已变,它的答案不能覆盖最新现场。
			if (done.Version != _wantedVersion) return;
			if (done.Body == null)
			{
				// Keep one request in flight and back off before retrying an unchanged scene.
				_sentVersion = 0;
				_retryAt = _clock.ElapsedMilliseconds + 1000;
				return;
			}
			Parse(done.Body, done.LatencyMs);
		}

		static void StartLatest()
		{
			if (_busy || _wantedState == null || _sentVersion == _wantedVersion
				|| _clock.ElapsedMilliseconds < _retryAt) return;
			_sentVersion = _wantedVersion;
			Fire(_sentVersion, _wantedState);
		}

		static string Facts(Player p, int tcx, int tcy, int dist, bool workBusy)
			=> "{\"hp\":" + p.statLife + ",\"hp_max\":" + p.statLifeMax
			 + ",\"hp_percent\":" + (p.statLife * 100 / System.Math.Max(1, p.statLifeMax))
			 + ",\"player_cell\":[" + (int)(p.Center.X / 16f) + "," + (int)(p.Center.Y / 16f) + "]"
				 + ",\"placing_now\":" + (workBusy ? "true" : "false")
				 + ",\"capabilities\":" + PlayerCapabilities.Json(p)
			 + ",\"enemies\":" + ThreatScan.Json(p, (int)(p.Center.X / 16f), (int)(p.Center.Y / 16f))
			 + "}";

		static void Fire(int version, string state)
		{
			_busy = true;
			_sent++;
			var sw = System.Diagnostics.Stopwatch.StartNew();
			System.Threading.Tasks.Task.Run(async () =>
			{
				try
				{
					using var req = DecisionGateway.Request(Body(state));
					using var res = await _http.SendAsync(req);
					string txt = await res.Content.ReadAsStringAsync();
					if (!res.IsSuccessStatusCode)
					{
						_failed++;
						DiagLog.Write($"[jev] HTTP {(int)res.StatusCode} {txt.Substring(0, System.Math.Min(200, txt.Length))}");
						_pending = new Pending { Version = version, Body = null };
					}
					else _pending = new Pending
					{
						Version = version,
						LatencyMs = (int)sw.ElapsedMilliseconds,
						Body = txt,
					};
				}
				catch (System.Exception e)
				{
					_failed++;
					DiagLog.Write($"[jev] 请求炸了 {e.GetType().Name} {e.Message}");
					_pending = new Pending { Version = version, Body = null };
				}
				finally { _busy = false; }
			});
		}

		// 手写取值:mod 里没有 JSON 库,而要读的就三个字段
		static string Field(string s, string name)
		{
			int i = s.IndexOf("\"" + name + "\"", System.StringComparison.Ordinal);
			if (i < 0) return null;
			i = s.IndexOf(':', i);
			if (i < 0) return null;
			i++;
			while (i < s.Length && (s[i] == ' ' || s[i] == '"')) i++;
			int j = i;
			while (j < s.Length && s[j] != '"' && s[j] != ',' && s[j] != '}') j++;
			return s.Substring(i, j - i).Trim();
		}

		static void Parse(string txt, int ms)
		{
			string act = Field(txt, "choice");
			string conf = Field(txt, "confidence");
			string noul = Field(txt, "noul");
			if (act == null) { DiagLog.Write($"[jev] 读不出 choice: {txt.Substring(0, System.Math.Min(200, txt.Length))}"); return; }

			var call = new CombatCall { LatencyMs = ms, Why = DecisionGateway.Model };
			call.Act = act switch
			{
				"Fight" => CombatAct.Fight,
				"Flee" => CombatAct.Flee,
				"WallOff" => CombatAct.WallOff,
				"Heal" => CombatAct.Heal,
				_ => CombatAct.Ignore,
			};
			float.TryParse(conf, System.Globalization.NumberStyles.Float,
				System.Globalization.CultureInfo.InvariantCulture, out float c);
			call.Confidence = c;
			call.InterruptWork = noul != null
				&& float.TryParse(noul, System.Globalization.NumberStyles.Float,
					System.Globalization.CultureInfo.InvariantCulture, out float nv)
				&& nv > 0.7f;
			call.Probs = Probs(txt);
			// 置信太低就不听它的,退回 baseline。0.6 和 python 侧 fastjudge.CONF_ACT 一个数:
			// 实测 45 血遇恶魔眼它自己也只有 0.51~0.56,那种判断不值得照着动
			if (c > 0f && c < 0.6f) { call.Why = $"jev confidence {c:0.00} too low"; _haveLast = false; return; }
			_last = call; _haveLast = true; _lastAt = _clock.ElapsedMilliseconds;
		}

		// "Fight:0.72,Ignore:0.21" 这样一行,给观测页看
		static string Probs(string txt)
		{
			int i = txt.IndexOf("\"probabilities\"", System.StringComparison.Ordinal);
			if (i < 0) return "";
			int a = txt.IndexOf('{', i), b = txt.IndexOf('}', a < 0 ? i : a);
			if (a < 0 || b < 0) return "";
			return txt.Substring(a + 1, b - a - 1).Replace("\"", "").Replace(":", ":").Trim();
		}
	}
}
