using Terraria;
using Terraria.ID;

namespace TerraBlind
{
	// Facts about what this player can do right now. Never tell the model an ability is present by assumption.
	public static class PlayerCapabilities
	{
		public static bool HasGrapple(Player p)
			=> p.miscEquips != null && p.miscEquips.Length > 4
			   && p.miscEquips[4] != null && !p.miscEquips[4].IsAir;

		public static bool HasFeatherfall(Player p) => p.HasBuff(BuffID.Featherfall);
		public static bool CanDash(Player p) => p.dashType != 0;
		public static bool DashReady(Player p) => CanDash(p) && p.dashDelay == 0 && p.dash == 0;
		public static bool ExtraJumpReady(Player p) => p.AnyExtraJumpUsable();

		public static string Weapon(Player p)
		{
			var item = p.HeldItem;
			if (item == null || item.IsAir)
				return "{\"present\":false}";
			return "{\"present\":true,\"name\":" + DecisionGateway.Quote(item.Name)
				 + ",\"damage\":" + item.damage
				 + ",\"use_time\":" + item.useTime
				 + ",\"auto_reuse\":" + (item.autoReuse ? "true" : "false")
				 + ",\"projectile_speed\":" + item.shootSpeed.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)
				 + "}";
		}

		static string HotbarWeapons(Player p)
		{
			var sb = new System.Text.StringBuilder("[");
			int count = 0;
			for (int i = 0; i < 10 && i < p.inventory.Length; i++)
			{
				var item = p.inventory[i];
				if (item == null || item.IsAir || item.damage <= 0 || item.useStyle == 0) continue;
				if (item.pick != 0 || item.axe != 0 || item.hammer != 0) continue;
				if (count++ > 0) sb.Append(',');
				sb.Append("{\"slot\":").Append(i)
				  .Append(",\"name\":").Append(DecisionGateway.Quote(item.Name))
				  .Append(",\"damage\":").Append(item.damage)
				  .Append(",\"use_time\":").Append(item.useTime)
				  .Append(",\"selected\":").Append(i == p.selectedItem ? "true" : "false")
				  .Append(",\"projectile_speed\":")
				  .Append(item.shootSpeed.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture))
				  .Append('}');
			}
			return sb.Append(']').ToString();
		}

		public static string Json(Player p)
			=> "{\"grapple_equipped\":" + (HasGrapple(p) ? "true" : "false")
			 + ",\"grapple_attached\":" + (p.grapCount > 0 ? "true" : "false")
				 + ",\"featherfall_active\":" + (HasFeatherfall(p) ? "true" : "false")
				 + ",\"dash_equipped\":" + (CanDash(p) ? "true" : "false")
				 + ",\"dash_ready\":" + (DashReady(p) ? "true" : "false")
				 + ",\"extra_jump_ready\":" + (ExtraJumpReady(p) ? "true" : "false")
			 + ",\"held_weapon\":" + Weapon(p)
			 + ",\"hotbar_weapons\":" + HotbarWeapons(p)
			 + "}";
	}
}
