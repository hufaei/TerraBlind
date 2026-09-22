using Terraria.ModLoader;

namespace TerraBlind
{
	// /halt, 一条命令停掉所有还在跑的战斗、寻路和动作原语。
	public class HaltCommand : ModCommand
	{
		public override CommandType Type => CommandType.Chat;
		public override string Command => "halt";
		public override string Description => "急停:停掉全流程和所有正在跑的动作";

		public override void Action(CommandCaller caller, string input, string[] args)
			=> caller.Reply(Halt.All());
	}

	public static class Halt
	{
		// 【由上往下停】。先停编排层再停原语,反过来的话上层下一帧又把原语拉起来
		public static string All()
		{
			var was = new System.Collections.Generic.List<string>();
			if (RecedingNav.Active) was.Add("寻路");
			if (Combat.Enabled || Dodge.Enabled) was.Add("战斗");
			if (BridgeBuilder.IsRunning) was.Add("搭桥");

			Combat.Enabled = false;
			Dodge.Enabled = false;
			BridgeBuilder.Stop();

			RecedingNav.Stop();
			StateSpacePlanner.StopNav();
			NavCoordinator.Stop();

			PlaceAnywhere.Stop();
			PlaceAction.Stop();
			PlaceWalls.Stop();
			PillarUp.Stop();
			PlatformDown.Stop();
			RopeLadder.Stop();
			SettleAt.Stop();
			HopUp.Stop();
			DropDown.Stop();
			WalkPlace.Stop();
			MineCoordinator.Stop();
			ItemUseCoordinator.Stop();
			SkillExecutor.Stop();
			ActExecutor.Stop();

			// 停完还得松手:控制权锁跨帧持有,不放的话下一个动作抢不到
			AxisLock.Reset();

			DiagLog.Write($"[halt] 全停 停掉了:{(was.Count == 0 ? "无" : string.Join("/", was))}");
			return was.Count == 0 ? "本来就没在跑。" : $"停了:{string.Join("、", was)}";
		}
	}
}
