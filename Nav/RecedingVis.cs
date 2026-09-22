using System.Collections.Generic;

namespace TerraBlind
{
	// Navigation still emits diagnostic decisions, but the trimmed mod does not
	// render them. Keeping this no-op boundary avoids coupling the planner to UI.
	public static class RecedingVis
	{
		public static void SetField(int gx, int gy) { }

		public static void SetDecision(
			int curCx,
			int curCy,
			int curH,
			int gx,
			int gy,
			List<StateSpacePlanner.Cand> candidates,
			(int, int)? chosen,
			float score,
			(float x, float y) shortDirection,
			(float x, float y) mediumDirection,
			(float x, float y) longDirection) { }

		public static void Clear() { }
	}
}
