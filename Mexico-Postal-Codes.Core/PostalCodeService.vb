Option Strict On
Option Infer Off

Imports System.Collections.Generic
Imports System.IO
Imports System.Threading.Tasks

Public NotInheritable Class PostalCodeService

    Public Const SepomexMainUrl As String = "https://www.correosdemexico.gob.mx/SSLServicios/ConsultaCP/CodigoPostal_Exportar.aspx"
    Public Const SepomexHost As String = "www.correosdemexico.gob.mx"
    Public Const SepomexRefererInitial As String = "https://www.correosdemexico.gob.mx/SSLServicios/ConsultaCP/Descarga.aspx"
    Public Const SepomexRefererExport As String = "https://www.correosdemexico.gob.mx/SSLServicios/ConsultaCP/CodigoPostal_Exportar.aspx"
    Public Const SepomexOrigin As String = "https://www.correosdemexico.gob.mx"
    Public Const DatabaseDirectoryName As String = "postal_codes"
    Public Const DatabaseFileName As String = "CPdescarga.txt"
    Public Const DownloadZipFileName As String = "CPdescargatxt.zip"

    Private ReadOnly _logger As ILogger
    Private ReadOnly _workingDir As String
    Private ReadOnly _random As Random

    Public Sub New()
        Me.New(Nothing, Nothing, Nothing)
    End Sub

    Public Sub New(ByVal workingDirectory As String)
        Me.New(workingDirectory, Nothing, Nothing)
    End Sub

    Public Sub New(ByVal workingDirectory As String, ByVal logger As ILogger)
        Me.New(workingDirectory, logger, Nothing)
    End Sub

    Public Sub New(ByVal workingDirectory As String, ByVal logger As ILogger, ByVal random As Random)
        _workingDir = If(String.IsNullOrEmpty(workingDirectory), AppContext.WorkingDirectory, workingDirectory)
        _logger = If(logger, AppContext.Logger)
        _random = If(random, New Random())
    End Sub

    Public ReadOnly Property DatabasePath As String
        Get
            Return Path.Combine(_workingDir, DatabaseDirectoryName, DatabaseFileName)
        End Get
    End Property

    Public Function HasCachedDatabase() As Boolean
        Return File.Exists(DatabasePath)
    End Function

    Public Function LoadOrDownload() As List(Of c_PostalCode)
        If HasCachedDatabase() Then
            Try
                Return PostalCodeParser.Parse(DatabasePath, _logger)
            Catch ex As Exception
                _logger.Log($"Cached database at '{DatabasePath}' is unreadable. Re-downloading. ({ex.Message})", LogLevel.Warning)
            End Try
        End If
        Return DownloadParseAndCache()
    End Function

    Public Function Refresh() As List(Of c_PostalCode)
        Return DownloadParseAndCache()
    End Function

    Public Function DownloadParseAndCache() As List(Of c_PostalCode)
        Dim downloadPath As String = DownloadAsync().GetAwaiter().GetResult()
        If String.IsNullOrEmpty(downloadPath) Then
            _logger.Log("Download failed: no file was produced.", LogLevel.[Error])
            Return New List(Of c_PostalCode)()
        End If

        Dim extractTarget As String = Path.Combine(_workingDir, DatabaseDirectoryName)
        Dim zipToExtract As String = Path.Combine(_workingDir, DownloadZipFileName)
        If Not File.Exists(zipToExtract) Then
            zipToExtract = downloadPath
        End If

        Dim txtFile As String = ZipExtractor.ExtractZip(zipToExtract, extractTarget, _logger)
        If String.IsNullOrEmpty(txtFile) OrElse Not File.Exists(txtFile) Then
            _logger.Log("Extraction failed: no text file was produced.", LogLevel.[Error])
            Return New List(Of c_PostalCode)()
        End If

        Return PostalCodeParser.Parse(txtFile, _logger)
    End Function

    Public Async Function DownloadAsync() As Task(Of String)
        Using scraper As New c_Scraper(_logger)
            scraper.Host = SepomexHost
            scraper.Referer = SepomexRefererInitial

            Dim response As String = Await scraper.GetAsync(SepomexMainUrl).ConfigureAwait(False)
            If String.IsNullOrEmpty(response) Then
                _logger.Log("Initial SEPOMEX GET returned empty body.", LogLevel.[Error])
                Return String.Empty
            End If

            Dim formSection As String = HtmlHelper.GetString(response,
                                                             startIn:="<input type=""hidden"" name=""__EVENTTARGET""",
                                                             endTo:="<nav class=""navbar",
                                                             firstCoincidence:=True)
            If String.IsNullOrEmpty(formSection) Then
                _logger.Log("Could not locate ASP.NET form fields in SEPOMEX page.", LogLevel.[Error])
                Return String.Empty
            End If

            Dim coordinates As (X As Integer, Y As Integer) = HtmlHelper.GenerateButtonCoordinates(_random)
            Dim postData As String = HtmlHelper.BuildSepomexPostData(formSection, "txt", coordinates)

            scraper.Origin = SepomexOrigin
            scraper.Referer = SepomexRefererExport

            Return Await scraper.PostAsync(SepomexMainUrl, postData).ConfigureAwait(False)
        End Using
    End Function

End Class
