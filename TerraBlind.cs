using Terraria.ModLoader;

namespace TerraBlind
{
	public class TerraBlind : Mod
	{
		public static ModKeybind ToggleRecedingNav;

		public override void Load()
		{
			MazeWand.MarkMainThread();
			// The one retained gameplay key: navigate toward the tile under the cursor.
			ToggleRecedingNav = KeybindLoader.RegisterKeybind(this, "ToggleRecedingNav", "K");
		}

		public override void Unload() => ToggleRecedingNav = null;
	}
}
