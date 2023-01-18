using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Framework;
using UnityEngine.UI;

public class Logger : MonoBehaviour
{
    private FileStream m_file;
    private BinaryWriter m_fileWriter;
    [SerializeField]
    private Button m_btnDownloadLog;

    // Start is called before the first frame update
    void Start()
    {
        //TestThrowException();
        //TestFileAPIOnWebGL();
        m_btnDownloadLog?.onClick.AddListener(OnClickDownloadLog);
        TestCLogger();
    }

    void OnClickDownloadLog()
    {
        string logPath = CLogger.generalRecorder.LogFilePath;
        Debug.Log("log path:" + logPath);
#if UNITY_WEBGL && !UNITY_EDITOR
        ExportLogFile(logPath);
#endif
    }

    private void TestCLogger()
    {
        CLogger.Init();

        CLogger.Log("CLogger.Log，这是一条log");
        CLogger.LogWarn("CLogger.LogWarn, 这是一条log");
        CLogger.LogError("CLogger.LogError,这是一条log");
        CLogger.ForceLog("CLogger.ForceLog, 这是一条log");
        CLogger.LogResource("CLogger.LogResource，这是一条log");
        Exception e = new Exception("这是一个异常,紧急处理");
        CLogger.LogException(e);
    }

    // Update is called once per frame
    //void Update()
    //{

    //}

    void TestFileAPIOnWebGL()
    {
        string path = Application.persistentDataPath + "/logger.log";
        Debug.Log("log path:" + path);
        string content = "[info]这是一条info log。";
        m_file = new FileStream(path, FileMode.Create, FileAccess.ReadWrite);
        m_fileWriter = new BinaryWriter(m_file);
        m_fileWriter.Write(content);
        content = "[warning]这是一条warning log。";
        m_fileWriter.Write(content);
        m_fileWriter.Flush();

#if UNITY_WEBGL && !UNITY_EDITOR
        //flush our changes to IndexedDB
        SyncDB();
#endif
    }

    void TestThrowException()
    {
        try
        {
            throw new Exception("throw a test exception");

        }
        catch (Exception err)
        {
            Debug.LogError(err.ToString());
            Debug.LogWarning(System.Environment.StackTrace);
        }
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void SyncDB();
    
    [DllImport("__Internal")]
    private static extern void ExportLogFile(string loggerkey);

    [DllImport("__Internal")]
    private static extern void ArrayBufferUTF8ToStr();
    
    [DllImport("__Internal")]
    private static extern void SaveFile();
#endif
}
