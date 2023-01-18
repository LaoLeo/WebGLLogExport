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

        CLogger.Log("CLogger.Log������һ��log");
        CLogger.LogWarn("CLogger.LogWarn, ����һ��log");
        CLogger.LogError("CLogger.LogError,����һ��log");
        CLogger.ForceLog("CLogger.ForceLog, ����һ��log");
        CLogger.LogResource("CLogger.LogResource������һ��log");
        Exception e = new Exception("����һ���쳣,��������");
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
        string content = "[info]����һ��info log��";
        m_file = new FileStream(path, FileMode.Create, FileAccess.ReadWrite);
        m_fileWriter = new BinaryWriter(m_file);
        m_fileWriter.Write(content);
        content = "[warning]����һ��warning log��";
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
