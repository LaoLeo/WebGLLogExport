using System;
using System.IO;
using System.Text;
using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace Framework
{
	public class CLogger
	{
        public static bool enableLog = true;
        public static bool enableWarnLog = true;
        public static bool enableErrorLog = true;

        private static bool isInit = false;
        private static string webLogUrl = null;
        //private static ILogHandler unityLogHandler;

        public static bool isDebugBuild = false;
        public static void Init()
        {
            if (!isInit)
            {
                //unityLogHandler = new UnityInnerLogHanlder();
                isDebugBuild = Debug.isDebugBuild || Application.isEditor;
#if UNITY_WEBGL && !UNITY_EDITOR
                _generalRecorder.supportMTR = false;
#else
                _generalRecorder.supportMTR = true;
#endif

                UnityEngine.Debug.Log("isDebugBuild:" + isDebugBuild.ToString());
                if (!isDebugBuild)
                {
                    enableLog = false;
                    enableWarnLog = false;
                }

                _stopWatch.Reset();
                _stopWatch.Start();

				Application.logMessageReceived += _OnLogCallbackHandler;
				Application.logMessageReceivedThreaded += _OnLogCallbackHandler;
                System.AppDomain.CurrentDomain.UnhandledException += _OnUncaughtExceptionHandler;
                isInit = true;
            }
        }

        public static void SetWebLogUrl(string url)
        {
            webLogUrl = url;
        }

        private static void _OnLogCallbackHandler(string message,string stackTrace,LogType logType)
        {
            if (logType == LogType.Error || logType == LogType.Exception)
            {
                _generalRecorder.LogError (message + "\n" + stackTrace);
            }
        }

        private static void _OnUncaughtExceptionHandler (object sender, System.UnhandledExceptionEventArgs args)
        {
            if (enableErrorLog)
            {
                if (args == null || args.ExceptionObject == null) {
                    return;
                }

                try {
                    if (args.ExceptionObject.GetType () != typeof(System.Exception)) {
                        return;
                    }
                } catch {
                    if (UnityEngine.Debug.isDebugBuild == true) {
                    }

                    return;
                }

                System.Exception e = (System.Exception)args.ExceptionObject;
                _generalRecorder.LogError (e.Message + "\n" + e.StackTrace);
            }
        }
		
        private static FileLogRecorder _resRecorder;
        public static FileLogRecorder resRecorder
        {
            get 
            {
                if(_resRecorder == null)
                {
                    _resRecorder = new FileLogRecorder("resource");
                }
                return _resRecorder;
            }
        }

//        private static FileLogRecorder _hotUpdateRecorder;
//        public static FileLogRecorder hotUpdateRecorder
//        {
//            get
//            {
//                if (_hotUpdateRecorder == null)
//                {
//                    _hotUpdateRecorder = new FileLogRecorder("hotupdate");
//                }
//                return _hotUpdateRecorder;
//            }
//        }

        private static FileLogRecorder _timeRecorder;
        private static Stopwatch _stopWatch = new Stopwatch();
        public static FileLogRecorder timeRecorder
        {
            get 
            {
                if(_timeRecorder == null)
                {
                    _timeRecorder = new FileLogRecorder("time");
                }
                return _timeRecorder;
            }
        }

        private static FileLogRecorder _generalRecorder = new FileLogRecorder("aounity");
		public static FileLogRecorder generalRecorder
		{
			get{ return _generalRecorder;}
		}

        public static void LogResource(object msg)
        {
            if(enableLog)
            {
                resRecorder.Log(msg);
            }
        }

        public static void LogTimeStamp(string key)
        {
            if(enableLog)
            {
                StringBuilder sb=new StringBuilder();
                sb.Append(key);
                sb.Append("[elapsedMs=");
                sb.Append(_stopWatch.ElapsedMilliseconds);
                sb.Append("]");

                timeRecorder.Log(sb.ToString());
            }
        }

        public static void ForceLog(object msg,string color="#ffffff")
        {
            StringBuilder sb=new StringBuilder();
            if (color == null || color.Trim().Length == 0)
            {
                sb.Append(msg.ToString());
            }
            else
            {
                sb.Append("<color="+color+">");
                sb.Append(msg.ToString());
                sb.Append("</color>");
            }

            Debug.Log(sb.ToString());

            _generalRecorder.Log(msg);
        }

        public static void Log(object msg,string color= null)
		{
			if (enableLog)
			{
				StringBuilder sb=new StringBuilder();
                if (color == null || color.Trim().Length == 0)
				{
					sb.Append(msg.ToString());
				}
				else
				{
					sb.Append("<color="+color+">");
					sb.Append(msg.ToString());
					sb.Append("</color>");
				}

                UnityEngine.Debug.Log(sb.ToString());

				_generalRecorder.Log(msg);
			}
		}

		public static void LogException(Exception e)
		{
			if (enableErrorLog)
			{
                UnityEngine.Debug.LogError (e.Message + "\n" + e.StackTrace);
                //_logRecorder.LogError (e.Message + "\n" + e.StackTrace);
                _generalRecorder.LogError(e.Message + "\n" + e.StackTrace);

            }
		}

		public static void LogError(object msg)
		{
			if (enableErrorLog)
			{
				UnityEngine.Debug.LogError(msg);
                //_logRecorder.LogError(msg); 
                _generalRecorder.LogError(msg);
            }
		}
		
		public static void LogWarn(object msg)
		{
			if (enableWarnLog)
			{
				UnityEngine.Debug.LogWarning(msg);
				_generalRecorder.LogWarn(msg);
			}
		}


        public static void SendGeneralLogToWeb()
        {
            if(string.IsNullOrEmpty(webLogUrl)) return;

   //         WWWForm form = new WWWForm();
   //         form.AddField("log",generalRecorder.GetContent());
   //         HttpConnection.Instance.RequestWithForm(webLogUrl,form,null);
			//form = new WWWForm();
			//form.AddField("log",generalRecorder.GetPrevContent());
			//HttpConnection.Instance.RequestWithForm(webLogUrl,form,null);
        }

        public static void SendTimeLogToWeb()
        {
            if(string.IsNullOrEmpty(webLogUrl) || !enableLog) return;

            //WWWForm form = new WWWForm();
            //form.AddField("time",timeRecorder.GetContent());
            //HttpConnection.Instance.RequestWithForm(webLogUrl,form,null);
        }

        public static void SendResourceLogToWeb()
        {
            if(string.IsNullOrEmpty(webLogUrl) || !enableLog) return;

            //WWWForm form = new WWWForm();
            //form.AddField("resource",resRecorder.GetContent());
            //HttpConnection.Instance.RequestWithForm(webLogUrl,form,null);
        }

//        public static void SendHotUpdateLogToWeb()
//        {
//            if (string.IsNullOrEmpty(webLogUrl) || !enableLog) return;
//
//            WWWForm form = new WWWForm();
//            form.AddField("hotupdate", hotUpdateRecorder.GetContent());
//            HttpConnnection.Instance.RequestWithForm(webLogUrl, form, null);
//        }
    }
}