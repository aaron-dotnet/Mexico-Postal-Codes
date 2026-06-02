Option Strict On
Option Infer Off

Imports System.IO
Imports System.Runtime.InteropServices

Public NotInheritable Class AppContext

    Private Shared _workingDirectoryValue As String
    Private Shared _loggerValue As ILogger

    Public Shared Property WorkingDirectory As String
        Get
            If String.IsNullOrEmpty(_workingDirectoryValue) Then
                _workingDirectoryValue = GetDefaultWorkingDirectory()
            End If
            Return _workingDirectoryValue
        End Get
        Set(ByVal value As String)
            _workingDirectoryValue = value
        End Set
    End Property

    Public Shared Property Logger As ILogger
        Get
            If _loggerValue Is Nothing Then
                _loggerValue = NullLogger.Instance
            End If
            Return _loggerValue
        End Get
        Set(ByVal value As ILogger)
            _loggerValue = If(value, NullLogger.Instance)
        End Set
    End Property

    Public Shared Sub Reset()
        _workingDirectoryValue = Nothing
        _loggerValue = Nothing
    End Sub

    Private Shared Function GetDefaultWorkingDirectory() As String
        Dim baseDir As String
        If RuntimeInformation.IsOSPlatform(OSPlatform.Windows) Then
            baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
        Else
            baseDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".local", "share")
        End If

        Dim workingDir As String = Path.Combine(baseDir, "MexicoPostalCodes")
        Try
            Directory.CreateDirectory(workingDir)
            Return workingDir
        Catch
            Return Path.GetTempPath()
        End Try
    End Function

End Class
