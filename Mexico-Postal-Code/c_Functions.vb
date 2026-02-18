Option Strict On
Option Infer Off

Imports System.IO
Imports System.IO.Compression

Public MustInherit Class c_Functions
    Public Shared Function ExtractZip(zipPath As String, extractPath As String) As String
        If Not File.Exists(zipPath) Then
            Throw New FileNotFoundException($"The file:'{zipPath}' not exists.")
        End If

        If Not Directory.Exists(extractPath) Then
            Directory.CreateDirectory(extractPath)
        End If

        ZipFile.ExtractToDirectory(zipPath, extractPath)

        ' regresar la ruta del archivo extraído
        Dim extractedFile As String = Directory.GetFiles(extractPath)(0)
        Return extractedFile
    End Function
    Public Shared Function GetString(ByRef fullString As String, startStr As String, endStr As String,
                                     Optional excessAmount As Integer = 0,
                                     Optional firstCoincidence As Boolean = False) As String
        ' Función para filtrar contenido de una cadena.

        Dim startWord As Integer = fullString.IndexOf(startStr, StringComparison.OrdinalIgnoreCase)
        If startWord = -1 Then Return String.Empty

        Dim endWord As Integer
        If firstCoincidence Then
            endWord = fullString.IndexOf(endStr, fullString.IndexOf(startStr), StringComparison.OrdinalIgnoreCase)
        Else
            endWord = fullString.LastIndexOf(endStr, StringComparison.OrdinalIgnoreCase)
        End If

        Return fullString.AsSpan(startWord, endWord - startWord + endStr.Length - excessAmount).ToString()
    End Function
End Class
