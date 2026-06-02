Option Strict On
Option Infer Off

Imports System.IO
Imports System.IO.Compression
Imports System.Text

Friend NotInheritable Class ZipExtractor
    Public Shared Function ExtractZip(ByVal zipPath As String,
                                      ByVal extractPath As String,
                                      Optional ByVal logger As ILogger = Nothing) As String

        If String.IsNullOrEmpty(zipPath) Then
            Throw New ArgumentException("Zip path cannot be null or empty.", NameOf(zipPath))
        End If
        If Not File.Exists(zipPath) Then
            Throw New FileNotFoundException($"The file '{zipPath}' does not exist.", zipPath)
        End If
        If String.IsNullOrEmpty(extractPath) Then
            Throw New ArgumentException("Extract path cannot be null or empty.", NameOf(extractPath))
        End If

        Dim effectiveLogger As ILogger = If(logger, AppContext.Logger)
        Dim firstExtractedFile As String = String.Empty

        Try
            Directory.CreateDirectory(extractPath)

            ' Abrir el archivo ZIP para leer sus entradas
            Using archive As ZipArchive = ZipFile.OpenRead(zipPath)
                For Each entry As ZipArchiveEntry In archive.Entries
                    ' Calcular la ruta completa de destino para cada archivo
                    Dim destinationPath As String = Path.GetFullPath(Path.Combine(extractPath, entry.FullName))

                    ' Evitar vulnerabilidad de Zip Slip (Optional pero recomendado)
                    If Not destinationPath.StartsWith(Path.GetFullPath(extractPath), StringComparison.OrdinalIgnoreCase) Then
                        Continue For
                    End If

                    ' Si es un directorio dentro del ZIP, crearlo
                    If entry.FullName.EndsWith("/") OrElse entry.FullName.EndsWith("\") Then
                        Directory.CreateDirectory(destinationPath)
                        Continue For
                    End If

                    ' --- SIMULACIÓN DE OVERWRITE ---
                    ' Si el archivo ya existe en el destino, se elimina antes de extraer
                    If File.Exists(destinationPath) Then
                        File.Delete(destinationPath)
                    End If

                    ' Asegurar que el directorio contenedor exista
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath))

                    ' Extraer el archivo
                    entry.ExtractToFile(destinationPath)

                    ' Guardar la ruta del primer archivo para el retorno
                    If String.IsNullOrEmpty(firstExtractedFile) Then
                        firstExtractedFile = destinationPath
                    End If
                Next
            End Using

        Catch ioex As IOException
            effectiveLogger.Log($"Error trying to write to '{extractPath}': {ioex.Message}", LogLevel.[Error])
            Return String.Empty
        Catch unauthex As UnauthorizedAccessException
            effectiveLogger.Log($"Not have the required permission: {unauthex.Message}", LogLevel.[Error])
            Return String.Empty
        Catch ex As Exception
            effectiveLogger.Log(ex.Message, LogLevel.[Error])
            Return String.Empty
        End Try

        If String.IsNullOrEmpty(firstExtractedFile) Then
            effectiveLogger.Log($"No file was extracted into '{extractPath}'.", LogLevel.Warning)
            Return String.Empty
        End If

        Return firstExtractedFile
    End Function
End Class
