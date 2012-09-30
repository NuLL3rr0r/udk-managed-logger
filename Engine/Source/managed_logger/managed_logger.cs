using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using RGiesecke.DllExport;
using System.Runtime.InteropServices;
using System.Threading;
using System.Web.Script.Serialization;

namespace managed_logger
{
    internal static class managed_logger
    {
        private static readonly string PIPE_NAME = "UdkManagedLog";

        private static Mutex m_workerMutex = new Mutex();
        private static Queue<string> m_logs = new Queue<string>();

        static managed_logger()
        {
            Thread.CurrentThread.SetApartmentState(ApartmentState.STA);
            Thread thread = new Thread(new ThreadStart(SendLogs));
            thread.IsBackground = true;
            thread.Start();
        }

        [DllExport("Assert", CallingConvention = CallingConvention.StdCall)]
        public static void Assert([MarshalAs(UnmanagedType.LPWStr)]string condition,
            [MarshalAs(UnmanagedType.LPWStr)]string message,
            [MarshalAs(UnmanagedType.LPWStr)]string channels,
            [MarshalAs(UnmanagedType.LPWStr)]string source,
            [MarshalAs(UnmanagedType.LPWStr)]string className,
            [MarshalAs(UnmanagedType.LPWStr)]string stateName,
            [MarshalAs(UnmanagedType.LPWStr)]string funcName)
        {
            Log("Assert", condition, message, channels, source, className, stateName, funcName);
        }

        [DllExport("Debug", CallingConvention = CallingConvention.StdCall)]
        public static void Debug(
            [MarshalAs(UnmanagedType.LPWStr)]string message,
            [MarshalAs(UnmanagedType.LPWStr)]string channels,
            [MarshalAs(UnmanagedType.LPWStr)]string source,
            [MarshalAs(UnmanagedType.LPWStr)]string className,
            [MarshalAs(UnmanagedType.LPWStr)]string stateName,
            [MarshalAs(UnmanagedType.LPWStr)]string funcName)
        {
            Log("Debug", "", message, channels, source, className, stateName, funcName);
        }

        [DllExport("Info", CallingConvention = CallingConvention.StdCall)]
        public static void Info(
            [MarshalAs(UnmanagedType.LPWStr)]string message,
            [MarshalAs(UnmanagedType.LPWStr)]string channels,
            [MarshalAs(UnmanagedType.LPWStr)]string source,
            [MarshalAs(UnmanagedType.LPWStr)]string className,
            [MarshalAs(UnmanagedType.LPWStr)]string stateName,
            [MarshalAs(UnmanagedType.LPWStr)]string funcName)
        {
            Log("Info", "", message, channels, source, className, stateName, funcName);
        }

        [DllExport("Warn", CallingConvention = CallingConvention.StdCall)]
        public static void Warn(
            [MarshalAs(UnmanagedType.LPWStr)]string message,
            [MarshalAs(UnmanagedType.LPWStr)]string channels,
            [MarshalAs(UnmanagedType.LPWStr)]string source,
            [MarshalAs(UnmanagedType.LPWStr)]string className,
            [MarshalAs(UnmanagedType.LPWStr)]string stateName,
            [MarshalAs(UnmanagedType.LPWStr)]string funcName)
        {
            Log("Warn", "", message, channels, source, className, stateName, funcName);
        }

        [DllExport("Error", CallingConvention = CallingConvention.StdCall)]
        public static void Error(
            [MarshalAs(UnmanagedType.LPWStr)]string message,
            [MarshalAs(UnmanagedType.LPWStr)]string channels,
            [MarshalAs(UnmanagedType.LPWStr)]string source,
            [MarshalAs(UnmanagedType.LPWStr)]string className,
            [MarshalAs(UnmanagedType.LPWStr)]string stateName,
            [MarshalAs(UnmanagedType.LPWStr)]string funcName)
        {
            Log("Error", "", message, channels, source, className, stateName, funcName);
        }

        [DllExport("Fatal", CallingConvention = CallingConvention.StdCall)]
        public static void Fatal(
            [MarshalAs(UnmanagedType.LPWStr)]string message,
            [MarshalAs(UnmanagedType.LPWStr)]string channels,
            [MarshalAs(UnmanagedType.LPWStr)]string source,
            [MarshalAs(UnmanagedType.LPWStr)]string className,
            [MarshalAs(UnmanagedType.LPWStr)]string stateName,
            [MarshalAs(UnmanagedType.LPWStr)]string funcName)
        {
            Log("Fatal", "", message, channels, source, className, stateName, funcName);
        }

        static void Log(string type, string condition, string message, string channels, string source, string className, string stateName, string funcName)
        {
            string ts = DateTime.Now.ToString("yyyy:MM:dd HH:mm:ss");
            string[] chs = channels.Split(new char[] { ',' });

            for (int i = 0; i < chs.Length; ++i)
            {
                JavaScriptSerializer jss = new JavaScriptSerializer();
                Log log = new Log();
                log.Type = type;
                log.Timestamp = ts;
                log.Condition = condition;
                log.Message = message;
                log.Channel = chs[i].Trim();
                log.Source = source;
                log.ClassName = className;
                log.StateName = stateName;
                log.FuncName = funcName;

                m_workerMutex.WaitOne();
                m_logs.Enqueue(jss.Serialize(log));
                m_workerMutex.ReleaseMutex();
            }
        }

        static void SendLog(string log)
        {
            try
            {
                using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", PIPE_NAME, PipeDirection.Out))
                {
                    pipeClient.Connect();

                    using (StreamWriter sw = new StreamWriter(pipeClient))
                    {
                        sw.AutoFlush = true;
                        sw.WriteLine(log);
                    }
                }
            }
            catch (Exception ex)
            {
            }
            finally
            {
            }
        }

        static void SendLogs()
        {
            while (true)
            {
                if (m_logs.Count > 0)
                {
                    m_workerMutex.WaitOne();
                    string log = m_logs.Dequeue();
                    m_workerMutex.ReleaseMutex();
                    SendLog(log);
                }
            }
        }
    }
}
