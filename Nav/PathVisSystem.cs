using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace TerraBlind
{
	// Compatibility sink for planner diagnostics. The trimmed mod deliberately
	// keeps navigation independent from any in-game debug rendering.
	public class PathVisSystem : ModSystem
	{
		public static bool Enabled = false;
		public static bool ShowPlanner = false;

		public static void SetDeck(List<(int wx, int wy, Color color)> tiles, int ttlFrames) { }
		public static void ClearDeck() { }
		public static void SetHollow(List<(int wx, int wy, int w, int h, Color color)> boxes, int ttlFrames) { }
		public static void SetGhosts(List<(int wx, int wy, ushort type, short frameX, short frameY, bool mine)> ghosts, int ttlFrames = 7200) { }
		public static void ClearGhosts() { }
		public static void SetSSPath(
			List<(float wpx, float wpy, bool isJump)> trail,
			List<(float wpx, float wpy)> explored,
			float goalPx,
			float goalPy,
			List<(int cx, int cy)> placed = null,
			List<(int wx, int wy)> mineTiles = null,
			int ttlFrames = 1200) { }
		public static void SetLookaheadPath(
			List<(float wpx, float wpy, bool isJump)> trail,
			float goalPx,
			float goalPy,
			float startPx,
			float startPy,
			int ttlFrames = 1200) { }
		public static void ClearLookahead() { }
		public static void SetTiles(List<(int wx, int wy, Color color)> tiles, int ttlFrames = 600) { }
		public static void SetLabels(List<(int wx, int wy, string text, Color color)> labels, int ttlFrames = 600) { }
		public static void SetPath(List<(int wx, int wy)> nodes, int ttlFrames = 600) { }
		public static void SetBlocks(List<(int wx, int wy)> pillar, List<(int wx, int wy)> bridge, int ttlFrames = 600) { }
		public static void SetPlanPath(List<NavNode> path, int[] envelope) { }
	}
}
