using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;

namespace bfbd.UltraRecord.Core
{
	partial class RawInputApp
	{
		private CacheManager _cacheManager = new CacheManager();

		private SessionInfo CreateRecordSession(string sessionId)
		{
			SessionInfo session = null;
			try
			{
				if (!string.IsNullOrEmpty(sessionId))
					session = _cacheManager.LoadSessionInfo(sessionId);

				if (session == null)
				{
					string sid = string.IsNullOrEmpty(sessionId) ? Guid.NewGuid().ToString("n") : sessionId;
					RemoteSessionInfo rsi = WTSEngine.GetRemoteSessionInfo();
					session = new SessionInfo()
					{
						SessionId = sid,
						CreateTime = DateTime.Now,
						UserName = rsi.UserName,
						Domain = rsi.Domain,
						ClientName = rsi.ClientName,
						ClientAddress = rsi.ClientAddress,
					};
					_cacheManager.WriteSesionInfo(session);
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); throw; }
			return session;
		}

		private bool IsRecording { get; set; }

		private void SnapshotWorker(object state)
		{
			TraceLogger.Instance.WriteEntracne();
			IRawEvent[] evts = state as IRawEvent[];
			Debug.Assert(evts != null && evts.Length > 0);
			var lastEvt = evts[evts.Length - 1];
			try
			{
				if (!IsRecording)
					return;
				TraceLogger.Instance.WriteLineVerbos("SnapshotWorker do record at ticks: " + DateTime.Now.Ticks);

				WindowInfo wi = SnapshotEngine.GetActiveProcessWindow();
				Snapshot sshot = new Snapshot()
				{
					SessionId = _session.SessionId,
					SnapshotId = Guid.NewGuid().ToString("n"),
					SnapTime = DateTime.Now,

					ProcessId = wi.ProcessId,
					ProcessName = wi.ProcessName,
					FileName = wi.FileName,

					ScreenWidth = wi.ScreenWidth,
					ScreenHeight = wi.ScreenHeight,

					WindowHandle = wi.WindowHandle,
					WindowRect = wi.WindowRect,
					WindowTitle = wi.WindowTitle,
					WindowUrl = wi.WindowUrl,
				};
				// image
				if (Global.Config.RecordImage)
				{
					Image img = WindowCaptureEngine.CaptureScreen();
					if (Global.Config.DebugDumpOriginal)
					{
						try
						{
							string path = Path.Combine(CacheManager.CachePath, string.Format(@"{0}\{1}.png", SessionId, sshot.SnapshotId));
							(img as Bitmap).Save(path, System.Drawing.Imaging.ImageFormat.Png);
						}
						catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
					}
					bool isGrayScale;
					sshot.ImageData = GetImageData(img, out isGrayScale);
					sshot.IsGrayScale = isGrayScale;
					sshot.Mouse = GetMouseState(lastEvt.MouseEvent);
				}
				
				// text
				if (lastEvt.IsKeyEvent)
					sshot.ControlText = TextCaptureEngine.GetControlText(new IntPtr(wi.WindowHandle));
				sshot.InputText = _policy.GetText(evts);
				sshot.EventsData = RawInputEvent.ToBinary(evts);

				// flush
				_cacheManager.WriteSnapshot(_session, sshot);

				// set active
				_session.LastActiveTime = DateTime.Now;

				// debug
				if (Global.Config.DebugDumpText)
				{
					try
					{
						if (!string.IsNullOrEmpty(sshot.InputText))
							File.AppendAllText(Path.Combine(CacheManager.CachePath, "InputText.txt"), sshot.InputText + Environment.NewLine);
						if (!string.IsNullOrEmpty(sshot.ControlText))
							File.AppendAllText(Path.Combine(CacheManager.CachePath, "ControlText.txt"), sshot.ControlText + Environment.NewLine);
						if (!string.IsNullOrEmpty(sshot.WindowUrl))
							File.AppendAllText(Path.Combine(CacheManager.CachePath, "WindowUrl.txt"), sshot.WindowUrl + Environment.NewLine);
					}
					catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
				}
			}
			catch (Exception ex) { TraceLogger.Instance.WriteException(ex); }
			TraceLogger.Instance.WriteExit();
		}

		private byte[] GetImageData(Image srcImage, out bool isGrayScale)
		{
			Image newImage;
			if (Global.Config.AgentGrayScale)
			{
				newImage = new ImageColorsConverter().SaveImageWithNewColorTable(srcImage);
				srcImage.Dispose();
				isGrayScale = true;
			}
			else
			{
				Bitmap bmp = new Bitmap(srcImage.Width, srcImage.Height, System.Drawing.Imaging.PixelFormat.Format16bppRgb555);
				using (Graphics g = Graphics.FromImage(bmp))
				{
					g.DrawImage(srcImage, 0, 0);
					srcImage.Dispose();
				}
				newImage = bmp;
				isGrayScale = false;
			}

			byte[] bsImageData;
			using (MemoryStream ms = new MemoryStream())
			{
				newImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
				bsImageData = ms.ToArray();
				newImage.Dispose();
			}
			return bsImageData;
		}

		private MouseState GetMouseState(IMouseEvent mouse)
		{
			MouseState state = null;
			if (mouse != null && (mouse.Evt == RawEventType.LeftButtonDown || mouse.Evt == RawEventType.RightButtonDown))
			{
				state = new MouseState()
				{
					ClickOption = mouse.Evt == RawEventType.LeftButtonDown ? MouseClickOption.LeftButtonDown : MouseClickOption.RightButtonDown,
					X = mouse.X,
					Y = mouse.Y
				};
			}
			return state;
		}

		//[Obsolete]
		//private Image DrawMouseClick(Image img, IMouseEvent mouse)
		//{
		//    if (mouse != null && mouse.Evt == RawEventType.LeftButtonDown || mouse.Evt == RawEventType.RightButtonDown)
		//    {
		//        using (Graphics g = Graphics.FromImage(img))
		//        {
		//            var cur = Resources.mouse;
		//            Pen pen = new Pen(Color.Black, 2f);
		//            g.DrawEllipse(pen, mouse.X - 7, mouse.Y - 2, cur.Height + 3, cur.Height + 3);
		//            g.DrawImage(cur, mouse.X, mouse.Y);
		//            g.Flush();
		//        }
		//    }
		//    return img;
		//}
	}
}
