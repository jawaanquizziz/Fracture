#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Valmon.Campaign
{
    public partial class CampaignController
    {
        private void StartPresentationCaptureIfRequested()
        {
            string[] args = System.Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, "-valomonRecordIntro");
            if (i < 0 || i + 1 >= args.Length) return;
            StartCoroutine(RecordPresentation(args[i + 1]));
        }
        private IEnumerator RecordPresentation(string folder)
        {
            introRecording = true; Directory.CreateDirectory(folder); Time.captureFramerate = 24;
            videoWait = 0; if (video != null) { video.Stop(); Destroy(video); video = null; }
            ScreenState = CampaignScreen.Intro; World.ShowStadium();
            for (int frame = 0; frame < 288; frame++)
            {
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(folder, "frame-" + frame.ToString("D4") + ".png"));
            }
            Time.captureFramerate = 0; yield return new WaitForSecondsRealtime(1);
            Debug.Log("[VALOMON] Opening frames captured: 288 at 24 fps.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#else
            Application.Quit(0);
#endif
        }
    }
}
#endif
