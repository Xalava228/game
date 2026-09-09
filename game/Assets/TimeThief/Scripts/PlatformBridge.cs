using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace TimeThief
{
    public sealed class PlatformBridge : MonoBehaviour
    {
        GameManager game;
        Action<bool> adCallback;
        bool busy;
#if UNITY_WEBGL && !UNITY_EDITOR
  [DllImport("__Internal")] static extern void TT_Ready();
  [DllImport("__Internal")] static extern void TT_Gameplay(int active);
  [DllImport("__Internal")] static extern void TT_Ad(int rewarded);
  [DllImport("__Internal")] static extern void TT_Save(string json);
  [DllImport("__Internal")] static extern string TT_Load();
  [DllImport("__Internal")] static extern string TT_Language();
  [DllImport("__Internal")] static extern int TT_CanAd();
#endif
        public void Init(GameManager g)
        {
            game = g;
        }

        public static string Language()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
   return TT_Language();
#else
            return "ru";
#endif
        }

        public bool CanAd
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
    return !busy&&TT_CanAd()==1;
#else
                return false;
#endif
            }
        }

        public void Ready()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
   TT_Ready();
#endif
        }

        public void Gameplay(bool active)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
   TT_Gameplay(active?1:0);
#endif
        }

        public static string Load()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
   return TT_Load();
#else
            return "";
#endif
        }

        public static void Save(string json)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
   TT_Save(json);
#endif
        }

        public void Ad(bool rewarded, Action<bool> callback)
        {
            if (!CanAd)
            {
                callback(false);
                return;
            }

            busy = true;
            adCallback = callback;
            game.SetAdPause(true);
#if UNITY_WEBGL && !UNITY_EDITOR
   TT_Ad(rewarded?1:0);
#else
            OnAdDone("0");
#endif
        }

        public void OnAdDone(string rewarded)
        {
            if (!busy)
                return;
            busy = false;
            var callback = adCallback;
            adCallback = null;
            game.SetAdPause(false);
            callback?.Invoke(rewarded == "1");
        }

        public void OnPlatformPause(string paused) => game.SetPlatformPause(paused == "1");
    }
}
