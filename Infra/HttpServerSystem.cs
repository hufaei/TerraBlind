using System;
using System.Net;
using System.Text;
using System.Threading;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace TerraBlind
{
	// Local observability for the game and decision models. Gameplay control stays in the mod.
	public class HttpServerSystem : ModSystem
	{
		public static volatile Snapshot LatestSnapshot;

		private const string Prefix = "http://127.0.0.1:17878/";
		private HttpListener _listener;
		private Thread _thread;
		private volatile bool _running;
		private bool _announced;

		public override void PostUpdateEverything()
		{
			if (!_announced && _running)
			{
				if (Main.netMode == 2) { _announced = true; return; }
				if (Main.LocalPlayer == null || !Main.LocalPlayer.active) return;
				Chatter.Say("[TerraBlind] HTTP server listening on " + Prefix, Color.LightGreen);
				_announced = true;
			}

			if (Main.LocalPlayer != null && Main.LocalPlayer.active) SurvivalReflex.Tick();
		}

		public override void Load()
		{
			LatestSnapshot = null;
			try
			{
				_listener = new HttpListener();
				_listener.Prefixes.Add(Prefix);
				_listener.Start();
				_running = true;
				_thread = new Thread(Loop) { IsBackground = true, Name = "TerraBlindHttp" };
				_thread.Start();
				Mod.Logger.Info("TerraBlind HTTP server listening on " + Prefix);
			}
			catch (Exception e)
			{
				Mod.Logger.Error("TerraBlind failed to start HTTP server: " + e);
			}
		}

		public override void Unload()
		{
			_running = false;
			_announced = false;
			try { _listener?.Stop(); } catch { }
			try { _listener?.Close(); } catch { }
			_listener = null;
			try { _thread?.Join(500); } catch { }
			_thread = null;
			LatestSnapshot = null;
		}

		private void Loop()
		{
			while (_running && _listener != null)
			{
				HttpListenerContext ctx;
				try { ctx = _listener.GetContext(); }
				catch { break; }

				try { Handle(ctx); }
				catch (Exception e)
				{
					try { Write(ctx, 500, "{\"error\":\"internal_error\"}"); } catch { }
					Mod.Logger.Warn("TerraBlind request error: " + e.Message);
				}
			}
		}

		private static void Handle(HttpListenerContext ctx)
		{
			string method = ctx.Request.HttpMethod;
			string path = ctx.Request.Url.AbsolutePath;

			if (method == "GET" && path == "/health")
			{
				Write(ctx, 200, "{\"ok\":true}");
				return;
			}
			if (method == "GET" && path == "/state")
			{
				Write(ctx, 200, StateSerializer.ToJson(LatestSnapshot));
				return;
			}
			if (method == "GET" && path == "/jev")
			{
				Write(ctx, 200, JevPage.Html(), "text/html; charset=utf-8");
				return;
			}
			if (method == "GET" && path == "/jev_log")
			{
				Write(ctx, 200, JevPage.Json());
				return;
			}
			if (method == "POST" && path == "/jev_clear")
			{
				JevLog.Clear();
				Write(ctx, 200, "{\"ok\":true}");
				return;
			}

			bool knownPath = path == "/health" || path == "/state" || path == "/jev" ||
				path == "/jev_log" || path == "/jev_clear";
			Write(ctx, knownPath ? 405 : 404, knownPath
				? "{\"error\":\"method_not_allowed\"}"
				: "{\"error\":\"not_found\"}");
		}

		private static void Write(HttpListenerContext ctx, int status, string body,
			string contentType = "application/json; charset=utf-8")
		{
			byte[] bytes = Encoding.UTF8.GetBytes(body ?? "null");
			ctx.Response.StatusCode = status;
			ctx.Response.ContentType = contentType;
			ctx.Response.ContentLength64 = bytes.Length;
			ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
			ctx.Response.OutputStream.Close();
		}
	}
}
