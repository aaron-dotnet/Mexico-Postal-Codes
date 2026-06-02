Option Strict On
Option Infer Off

Imports System.IO
Imports System.Text

Public Enum LogLevel
    Trace = 0
    Debug = 1
    Info = 2
    Warning = 3
    [Error] = 4
End Enum

Public Interface ILogger
    Sub Log(ByVal message As String, ByVal level As LogLevel)
End Interface

Public NotInheritable Class NullLogger
    Implements ILogger

    Public Shared ReadOnly Instance As New NullLogger()

    Private Sub New()
    End Sub

    Public Sub Log(ByVal message As String, ByVal level As LogLevel) Implements ILogger.Log
    End Sub
End Class

Public NotInheritable Class FileLogger
    Implements ILogger

    Private ReadOnly _logFile As String
    Private ReadOnly _syncLock As New Object()

    Public Sub New(ByVal logFilePath As String)
        If String.IsNullOrEmpty(logFilePath) Then
            Throw New ArgumentException("Log file path cannot be null or empty.", NameOf(logFilePath))
        End If
        _logFile = logFilePath
    End Sub

    Public Sub Log(ByVal message As String, ByVal level As LogLevel) Implements ILogger.Log
        SyncLock _syncLock
            Try
                Dim logDir As String = Path.GetDirectoryName(_logFile)
                If Not String.IsNullOrEmpty(logDir) Then
                    Directory.CreateDirectory(logDir)
                End If
                Dim line As String = $"[{DateTime.UtcNow:s}] [{level}] {message}{Environment.NewLine}"
                File.AppendAllText(_logFile, line, Encoding.UTF8)
            Catch
            End Try
        End SyncLock
    End Sub
End Class
