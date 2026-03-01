Option Strict On
Option Infer Off

Imports System.IO
Imports System.IO.Compression
Imports System.Runtime.InteropServices
Imports System.Text

Public MustInherit Class c_Functions
    Public Shared Property WorkingDirectory As String = GetWorkingDirectory()
    Private Shared ReadOnly _logFile As String = Path.Combine(WorkingDirectory, "MexicoPostalCodes.log")
    Private Shared ReadOnly _synclock As New Object()
    Public Enum LogLevel
        Trace
        Debug
        Info
        Warning
        [Error]
    End Enum
    Private Shared Function GetWorkingDirectory() As String
        Static workingDir As String = Nothing
        If Not String.IsNullOrEmpty(workingDir) Then Return workingDir

        Dim dir As String

        If RuntimeInformation.IsOSPlatform(OSPlatform.Windows) Then
            ' %AppData%
            dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
        Else
            ' Unix Systems
            ' $HOME/.config/
            dir = Path.Combine(Environment.GetFolderPath(
                               Environment.SpecialFolder.UserProfile), ".config")
        End If

        workingDir = Path.Combine(dir, "MexicoPostalCodes")

        Try
            Directory.CreateDirectory(workingDir)
        Catch ex As Exception
            workingDir = Path.Combine(Path.GetTempPath())
        End Try

        Return workingDir
    End Function

    Public Shared Sub Log(ByRef message As String, Optional level As LogLevel = LogLevel.Info)
        SyncLock _synclock
            Try
                Directory.CreateDirectory(WorkingDirectory)
                Dim line As String = $"[{DateTime.UtcNow:s}] [{level}] {message}{Environment.NewLine}"
                File.AppendAllText(_logFile, line, Encoding.UTF8)
            Catch
                Console.WriteLine($"Failed to write log: {message}")
            End Try
        End SyncLock
    End Sub
    Public Shared Function ExtractZip(zipPath As String, extractPath As String) As String
        If Not File.Exists(zipPath) Then
            Throw New FileNotFoundException($"The file:'{zipPath}' not exists.")
        End If
        Try
            Directory.CreateDirectory(extractPath)
            ZipFile.ExtractToDirectory(zipPath, extractPath, True)

        Catch ioex As IOException
            Log($"Error trying to writing to '{extractPath}': {ioex.Message}", LogLevel.Error)
        Catch unauthex As UnauthorizedAccessException
            Log($"Not have the required permission: " & unauthex.Message, LogLevel.Error)
        Catch ex As Exception
            Log(ex.Message, LogLevel.Error)
        End Try

        Dim files As String() = Directory.GetFiles(extractPath)
        If files.Length = 0 Then
            Log($"No se encontró ningún archivo extraído en '{extractPath}'", LogLevel.Warning)
            Return String.Empty
        End If


        Return files(0)     ' <--- Extrated file
    End Function
    Public Shared Function GetString(ByRef fullString As String, startStr As String, endStr As String,
                                     Optional excessAmount As Integer = 0,
                                     Optional firstCoincidence As Boolean = False) As String
        ' Función para filtrar contenido de una cadena.
        Dim startWord, endWord As Integer
        Dim comparison As StringComparison = StringComparison.OrdinalIgnoreCase

        startWord = fullString.IndexOf(startStr, comparison)
        If startWord = -1 Then Return String.Empty

        If firstCoincidence Then
            endWord = fullString.IndexOf(endStr, startWord, comparison)
        Else
            endWord = fullString.LastIndexOf(endStr, comparison)
        End If

        If endWord = -1 OrElse endWord < startWord Then Return String.Empty

        Return fullString.AsSpan(startWord, endWord - startWord + endStr.Length - excessAmount).ToString()
    End Function
End Class
