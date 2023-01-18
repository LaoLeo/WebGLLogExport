using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
namespace Framework
{	
	public class FileLogRecorder
	{
		private FileStream _file;
		private string _logFilePath;
		private BinaryWriter _fileWriter;
        private StreamReader _fileReader;
        private string _fileName;
        static string _applicationPath;
        static string _logDirPath;
        string _logSuffix = ".log";
        string _logPrev = "-prev";
        static char _connectChar = '_';
        static long _totalSize = 0;
        static int _totalCount = 0;
        string _logTimeRecordPath ;
        static List<LogFile> logs = new List<LogFile>();
        Dictionary<string,string> logInfos = new Dictionary<string, string>();
        struct LogFile {
            public DateTime createTime;
            public FileInfo file;
        }
        /**
         * whether it supports running in mutltithread enviroment
         * */
        private bool _supportMTR = false;

        public bool supportMTR
        {
            get{ return _supportMTR;}
            set{ _supportMTR = value;}
        }

        static public string ApplicationPath
        {
            get
            {
                if (_applicationPath == null)
                {
                    if ((Application.platform == RuntimePlatform.WindowsPlayer) || (Application.platform == RuntimePlatform.WindowsEditor) ||
                        (Application.platform == RuntimePlatform.OSXPlayer) || (Application.platform == RuntimePlatform.OSXEditor))
                    {
                        _applicationPath = Application.dataPath + "/../";

                    }
                    else
                    {
                        _applicationPath = Application.persistentDataPath + "/";
                    }
                }
                return _applicationPath;
            }
        }

        public string LogFilePath
        {
            get
            {
                if (_logFilePath == null)
                {
                    _logFilePath = ApplicationPath + _fileName + _logSuffix;
                }
                return _logFilePath;
            }
        }

        static public string LogDirPath
        {
            get
            {
                if (_logDirPath == null)
                {
                    _logDirPath = ApplicationPath + "PJGLogs";
                }
                return _logDirPath;
            }
        }

        public string LogTimeRecordPath
        {
            get
            {
                if (_logTimeRecordPath == null)
                {
                    _logTimeRecordPath = LogDirPath + "/" + _fileName + "TimeRecord.txt";
                }
                return _logTimeRecordPath;
            }
        }
		private FileStream _prevfile;
		string prevLogFilePath = "";
		private StreamReader _prevfileReader;
        public FileLogRecorder(string fileName)
        {
            _fileName = fileName;
            try
            {
                string preFileName =  fileName + _logPrev ;
                prevLogFilePath = LogFilePath.Replace(fileName, preFileName);
                ReadLogTime();
                if (!Directory.Exists(LogDirPath))
                {
                    Directory.CreateDirectory(LogDirPath);
                    File.Delete(LogFilePath);
                    File.Delete(prevLogFilePath);
                    //FileUtils.Instance.DeleteFile (LogFilePath);
                    //FileUtils.Instance.DeleteFile (prevLogFilePath);
                }
                string prevLogName = fileName + _logPrev;
                if (File.Exists(prevLogFilePath))
                {
                    string fileCreatTime = GetLogTime(prevLogName);
                    string logDir =  LogDirPath + "/" + fileCreatTime.Substring(0,10);
                    if (!Directory.Exists(logDir))
                    {
                        Directory.CreateDirectory(logDir);
                    }
                    string destFilePath = logDir + "/" + prevLogName + _connectChar + fileCreatTime + _logSuffix;
                    //FileUtils.Instance.DeleteFile (destFilePath);
                    File.Delete(destFilePath);
                    System.IO.File.Move(prevLogFilePath, destFilePath);
                }
                if (File.Exists(LogFilePath))
                {
                    System.IO.File.Move(LogFilePath, prevLogFilePath);
                    string creatTime = GetLogTime(fileName);
                    logInfos[prevLogName] = creatTime;
                }
                logInfos[fileName] = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                WriteLogTime();
            }
            catch (Exception e)
            {
            }
            this._file = new FileStream(LogFilePath, FileMode.Create, FileAccess.ReadWrite);
            this._fileWriter = new BinaryWriter(this._file);
            this._fileReader = new StreamReader(_file);
        }
        //解析日志时间
        void ReadLogTime()
        {
            if (!File.Exists(LogTimeRecordPath))
            {
               return;
            }
            using (StreamReader sr = new StreamReader(LogTimeRecordPath))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if(line!="")
                    {
                        string[] temp = line.Split('#');
                        if(temp.Length == 2)
                        {
                            logInfos.Add(temp[0],temp[1]);
                        }
                    }
                }
            }
        }
        //写日志时间
        void WriteLogTime()
        {
            if (File.Exists(LogTimeRecordPath))
            {
               File.Delete(LogTimeRecordPath);
            }
            using (StreamWriter sw = new StreamWriter(LogTimeRecordPath))
            {
                foreach(var item in logInfos)
                {
                    sw.WriteLine(item.Key + '#' + item.Value);
                }
            }
        }
        string GetLogTime(string name)
        {
            if(logInfos.ContainsKey(name))
            {
                return logInfos[name];
            }
            return DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        }

		//删除大于某个日期/日志数量超出某个阈值/日志大小超出某个阈值 的日志文件
        static public void DeleteOldLog(int expirationTime,int maxSize,int maxLogCount )
        {
            try
            {
                DateTime curTime = DateTime.Now;
                DirectoryInfo logDir = new DirectoryInfo(LogDirPath);
                DirectoryInfo[] dis = logDir.GetDirectories();
                DeleteByExpiredTime(dis, expirationTime);
                DeleteBySizeOrCount(dis, maxSize, maxLogCount);
            }
            catch (Exception e)
            {
            }
        }
        static void DeleteByExpiredTime(DirectoryInfo[] dis,int expirationTime)
        {
            DateTime curTime = DateTime.Now;
            for (int i = 0; i < dis.Length; i++)
            {
                if(dis[i].Name.Length!=10)
                {
                    continue;
                }
                DateTime dirCreatTime = Convert.ToDateTime(dis[i].Name);
                TimeSpan ts = curTime - dirCreatTime;
                if (ts.TotalSeconds > expirationTime)
                {
                    dis[i].Delete(true);
                    dis[i] = null;
                }
            }
        }
        static void DeleteBySizeOrCount(DirectoryInfo[] dis,int maxSize,int maxLogCount)
        {
            _totalSize = 0;
            _totalCount = 0;
            for (int i = 0; i < dis.Length; i++)
            {
                if (dis[i] != null)
                {
                    UpdateLogSizeAndCount(dis[i]);
                }
            }
            if (logs.Count > 0 && (_totalSize >= maxSize || _totalCount >= maxLogCount))
            {
                logs.Sort(delegate (LogFile f1, LogFile f2)
                {
                    return (int)(f1.createTime - f2.createTime).TotalSeconds;
                });
                logs[0].file.Delete();
            }
            logs.Clear();
        }
        static void UpdateLogSizeAndCount(DirectoryInfo dir)
        {
            //获取di目录中所有文件的大小
            foreach (FileInfo item in dir.GetFiles())
            {
                _totalSize += item.Length;
                _totalCount++;
                LogFile logFile = new LogFile();
                logFile.file = item;
                logFile.createTime = AnalysisFileTime(item.Name);
                logs.Add(logFile);
            }
            //获取di目录中所有的文件夹,并保存到一个数组中,以进行递归
            DirectoryInfo[] dis = dir.GetDirectories();
            if (dis.Length > 0)
            {
                for (int i = 0; i < dis.Length; i++)
                {
                    UpdateLogSizeAndCount(dis[i]);
                }
            }
        }

        static DateTime AnalysisFileTime(string fileName)
        {
            string[] temp = fileName.Split(_connectChar);
            if(temp.Length !=3)
            {
                return Convert.ToDateTime("1970-01-01 17:09:14");
            }
            DateTime createTime = Convert.ToDateTime(temp[1] +" "+temp[2].Substring(0,8).Replace ("-", ":"));
            return createTime;
        }

		~FileLogRecorder()
		{
			if ((Application.platform == RuntimePlatform.WindowsPlayer) || (Application.platform == RuntimePlatform.WindowsEditor) || 
			    (Application.platform == RuntimePlatform.OSXPlayer) || (Application.platform == RuntimePlatform.OSXEditor))
			{
				if (this._fileWriter != null)
				{
					this._fileWriter.Close();
					this._fileWriter = null;
				}
                if(this._fileReader != null)
                {
                    this._fileReader.Close();
                    this._fileReader = null;
                }
				if (this._file != null)
				{
					this._file.Close();
					this._file.Dispose();
					this._file = null;
				}
				if(this._prevfileReader != null)
				{
					this._prevfileReader.Close();
					this._prevfileReader = null;
				}
				if (this._prevfile != null)
				{
					this._prevfile.Close();
					this._prevfile.Dispose();
					this._prevfile = null;
				}
			}
		}

		public string GetPrevContent()
		{
			String content = "NULL";
			if (File.Exists (prevLogFilePath)) {
				if (_prevfile == null) {
					_prevfile = new FileStream (prevLogFilePath, FileMode.Open, FileAccess.Read);
					_prevfileReader = new StreamReader (_prevfile);
				}
				if (supportMTR) {
					lock (this) {
						_prevfile.Seek (0, SeekOrigin.Begin);
						content = _prevfileReader.ReadToEnd ();
						_prevfile.Seek (0, SeekOrigin.End);
					}
				} else {
					_prevfile.Seek (0, SeekOrigin.Begin);
					content = _prevfileReader.ReadToEnd ();
					_prevfile.Seek (0, SeekOrigin.End);
				}
				System.Text.StringBuilder sb = new System.Text.StringBuilder ();
				string str = "==================================PREV===================================\r\n";
				sb.Append (str);
				sb.Append (content);
				content = sb.ToString ();
			}
			return content;
		}

        public string GetContent()
        {
            String content;
            if(supportMTR)
            {
                lock(this)
                {
                    _file.Seek(0,SeekOrigin.Begin);
                    content = _fileReader.ReadToEnd();
                    _file.Seek(0,SeekOrigin.End);
                }
            }
            else
            {
                _file.Seek(0,SeekOrigin.Begin);
                content = _fileReader.ReadToEnd();
                _file.Seek(0,SeekOrigin.End);
            }
            return content;
        }

		private string DateTimeNowFormat(){
			return string.Format ("{0}-{1}-{2} {3}:{4}:{5}", DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
		}
           
		public void Log(object msg)
		{
			string str = (msg == null) ? "null" : msg.ToString ();
			string[] formatStrs = new string[] { "[INFO：", DateTimeNowFormat(), "]", str, "\r\n" };

			try {
				if (supportMTR) {
					lock (this) {
						this._fileWriter.Write (string.Concat (formatStrs).ToCharArray ());
						this._fileWriter.Flush ();
					}
				} else {
					this._fileWriter.Write (string.Concat (formatStrs).ToCharArray ());
					this._fileWriter.Flush ();
				}
			} catch (Exception e) {
				
			}
		}
		
		public void LogWarn(object msg)
		{
			string str = (msg == null) ? "null" : msg.ToString ();
			string[] formatStrs = new string[] { "[WARN：", DateTimeNowFormat(), "]", str, "\r\n" };

			try {
				if (supportMTR) {
					lock (this) {
						this._fileWriter.Write (string.Concat (formatStrs).ToCharArray ());
						this._fileWriter.Flush ();
					}
				} else {
					this._fileWriter.Write (string.Concat (formatStrs).ToCharArray ());
					this._fileWriter.Flush ();
				}
			} catch (Exception e) {
			}
		}
		
		public void LogError(object msg)
		{
			string str = (msg == null) ? "null" : msg.ToString ();
			string[] formatStrs = new string[] { "[ERROR：", DateTimeNowFormat(), "]", str, "\r\n" };

			try {
				if (supportMTR) {
					lock (this) {
						this._fileWriter.Write (string.Concat (formatStrs).ToCharArray ());
						this._fileWriter.Flush ();
					}
				} else {
					this._fileWriter.Write (string.Concat (formatStrs).ToCharArray ());
					this._fileWriter.Flush ();
				}
			} catch (Exception e) {
			}
		}
	}
}
