using System;

using UnityEngine;

using Yamly.UnityEditor;

using UnityDebug = UnityEngine.Debug;

namespace Yamly
{
    public static class LogUtils
    {
        private static bool IsVerbose
        {
            get
            {
                try
                {
                    var settings = YamlySettings.Instance;
                    return settings.VerboseLogs;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public static void Verbose(Exception e)
        {
            if (IsVerbose)
            {
                UnityDebug.LogException(e);
            }
        }

        public static void Verbose<T>(T t)
        {
            if (IsVerbose)
            {
                UnityDebug.Log(t.ToString());
            }
        }
        
        public static void Verbose<T>(T t, UnityEngine.Object c)
        {
            if (IsVerbose)
            {
                UnityDebug.Log(t.ToString(), c);
            }
        }

        public static void Warning<T>(T t)
        {
            UnityDebug.LogWarning(t.ToString());
        }

        public static void Error<T>(T t)
        {
            UnityDebug.LogError(t.ToString());
        }

        public static void Error(Exception e)
        {
            UnityDebug.LogException(e);
        }
        
        public static void Error<T>(T t, UnityEngine.Object c)
        {
            UnityDebug.LogError(t.ToString(), c);
        }

        public static void Info<T>(T t)
        {
            UnityDebug.Log(t.ToString());
        }
        
        public static void Profile<T>(T t)
        {
            UnityDebug.Log(t.ToString());
        }

        public static void Info(Exception e)
        {
            UnityDebug.LogException(e);
        }
    }
}