using System.Diagnostics;

namespace TerraBlind
{
	// 分诊的唯一入口。卡住恢复仍由确定性 baseline 决定并记录，
	// 不伪装成一次模型调用；模型当前只参与战斗和 boss 走位。
	public static class Triage
	{
		static readonly IStuckTriage _impl = new BaselineTriage();
		static readonly Stopwatch _clock = Stopwatch.StartNew();

		public static void Observe(in StuckScene s)
		{
			var r = _impl.Pick(in s);
			DiagLog.Write($"[triage] ({s.Cx},{s.Cy}) H{s.CurH} → {r.Rung} conf={r.Confidence:0.00} {r.Why}");
			JevLog.Add(new JevLog.Entry
			{
				Ms = _clock.ElapsedMilliseconds,
				Site = "triage",
				State = s.ToJson(),
				Pick = r.Rung.ToString(),
				Confidence = r.Confidence,
				Probs = r.Probs ?? "",
				Why = r.Why,
				LatencyMs = r.LatencyMs,
			});
		}
	}
}
