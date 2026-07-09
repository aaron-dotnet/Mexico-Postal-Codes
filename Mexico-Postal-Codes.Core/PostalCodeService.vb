Option Strict On
Option Infer Off

Imports System.Collections.Generic
Imports System.Globalization
Imports System.IO
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks

Public NotInheritable Class PostalCodeService

    Public Const SepomexMainUrl As String = "https://www.correosdemexico.gob.mx/SSLServicios/ConsultaCP/CodigoPostal_Exportar.aspx"
    Public Const SepomexHost As String = "www.correosdemexico.gob.mx"
    Public Const SepomexRefererInitial As String = "https://www.correosdemexico.gob.mx/SSLServicios/ConsultaCP/Descarga.aspx"
    Public Const SepomexRefererExport As String = "https://www.correosdemexico.gob.mx/SSLServicios/ConsultaCP/CodigoPostal_Exportar.aspx"
    Public Const SepomexOrigin As String = "https://www.correosdemexico.gob.mx"
    Private Const DatabaseDirectoryName As String = "postal_codes"
    Private Const DatabaseFileName As String = "CPdescarga.txt"

    Private ReadOnly _logger As ILogger
    Private ReadOnly _workingDir As String
    Private ReadOnly _random As Random
    Private ReadOnly _httpTimeoutSeconds As Integer
    Private _postalCodes As List(Of PostalCodeEntry)
    Private _postalCodesReadOnly As IReadOnlyList(Of PostalCodeEntry)
    Private _isLoaded As Boolean

    Public Sub New(Optional ByVal workingDirectory As String = Nothing,
                   Optional ByVal logger As ILogger = Nothing,
                   Optional ByVal httpTimeoutSeconds As Integer = 30)
        _workingDir = If(String.IsNullOrEmpty(workingDirectory), AppContext.WorkingDirectory, workingDirectory)
        _logger = If(logger, AppContext.Logger)
        _random = New Random()
        _httpTimeoutSeconds = If(httpTimeoutSeconds > 0, httpTimeoutSeconds, 30)
        SetPostalCodes(New List(Of PostalCodeEntry)())
        _isLoaded = False
    End Sub

    Public ReadOnly Property PostalCodes As IReadOnlyList(Of PostalCodeEntry)
        Get
            If _postalCodesReadOnly Is Nothing AndAlso _postalCodes IsNot Nothing Then
                _postalCodesReadOnly = _postalCodes.AsReadOnly()
            End If
            Return _postalCodesReadOnly
        End Get
    End Property

    Public ReadOnly Property IsLoaded As Boolean
        Get
            Return _isLoaded
        End Get
    End Property

    Public ReadOnly Property DatabasePath As String
        Get
            Return Path.Combine(_workingDir, DatabaseDirectoryName, DatabaseFileName)
        End Get
    End Property

    Public Function HasCachedDatabase() As Boolean
        Return File.Exists(DatabasePath)
    End Function

    Public Function LoadOrDownload() As List(Of PostalCodeEntry)
        If HasCachedDatabase() Then
            Try
                SetPostalCodes(PostalCodeParser.Parse(DatabasePath, _logger))
                _isLoaded = True
                Return _postalCodes
            Catch ex As Exception
                _logger.Log($"Cached database at '{DatabasePath}' is unreadable. Re-downloading. ({ex.Message})", LogLevel.Warning)
            End Try
        End If
        Return DownloadParseAndCacheSync()
    End Function

    Public Async Function LoadOrDownloadAsync(Optional ByVal cancellationToken As CancellationToken = Nothing) As Task(Of List(Of PostalCodeEntry))
        If HasCachedDatabase() Then
            Try
                SetPostalCodes(PostalCodeParser.Parse(DatabasePath, _logger))
                _isLoaded = True
                Return _postalCodes
            Catch ex As Exception
                _logger.Log($"Cached database at '{DatabasePath}' is unreadable. Re-downloading. ({ex.Message})", LogLevel.Warning)
            End Try
        End If
        Return Await DownloadParseAndCacheAsync(cancellationToken).ConfigureAwait(False)
    End Function

    Public Function Refresh() As List(Of PostalCodeEntry)
        Return DownloadParseAndCacheSync()
    End Function

    Public Async Function RefreshAsync(Optional ByVal cancellationToken As CancellationToken = Nothing) As Task(Of List(Of PostalCodeEntry))
        Return Await DownloadParseAndCacheAsync(cancellationToken).ConfigureAwait(False)
    End Function

    Private Function DownloadParseAndCacheSync() As List(Of PostalCodeEntry)
        Dim downloadPath As String = DownloadAsync(Nothing).GetAwaiter().GetResult()
        If String.IsNullOrEmpty(downloadPath) Then
            _logger.Log("Download failed: no file was produced.", LogLevel.[Error])
            SetPostalCodes(New List(Of PostalCodeEntry)())
            _isLoaded = False
            Return _postalCodes
        End If
        SetPostalCodes(ExtractAndParse(downloadPath))
        _isLoaded = True
        Return _postalCodes
    End Function

    Private Async Function DownloadParseAndCacheAsync(ByVal cancellationToken As CancellationToken) As Task(Of List(Of PostalCodeEntry))
        Dim downloadPath As String = Await DownloadAsync(cancellationToken).ConfigureAwait(False)
        If String.IsNullOrEmpty(downloadPath) Then
            _logger.Log("Download failed: no file was produced.", LogLevel.[Error])
            SetPostalCodes(New List(Of PostalCodeEntry)())
            _isLoaded = False
            Return _postalCodes
        End If
        SetPostalCodes(ExtractAndParse(downloadPath))
        _isLoaded = True
        Return _postalCodes
    End Function

    Private Function ExtractAndParse(ByVal downloadPath As String) As List(Of PostalCodeEntry)
        Dim extractTarget As String = Path.Combine(_workingDir, DatabaseDirectoryName)
        Dim txtFile As String = ZipExtractor.ExtractZip(downloadPath, extractTarget, _logger)

        Try
            If File.Exists(downloadPath) Then
                File.Delete(downloadPath)
            End If
        Catch ex As Exception
            _logger.Log($"Could not delete temporary ZIP '{downloadPath}': {ex.Message}", LogLevel.Warning)
        End Try

        If String.IsNullOrEmpty(txtFile) OrElse Not File.Exists(txtFile) Then
            _logger.Log("Extraction failed: no text file was produced.", LogLevel.[Error])
            Return New List(Of PostalCodeEntry)()
        End If

        If Not txtFile.Equals(DatabasePath, StringComparison.OrdinalIgnoreCase) Then
            Try
                If File.Exists(DatabasePath) Then File.Delete(DatabasePath)
                Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath))
                File.Move(txtFile, DatabasePath)
                txtFile = DatabasePath
            Catch ex As Exception
                _logger.Log($"Could not rename extracted file to cache path '{DatabasePath}': {ex.Message}. Using original path '{txtFile}'.", LogLevel.Warning)
            End Try
        End If

        Return PostalCodeParser.Parse(txtFile, _logger)
    End Function

    Public Function Search(Optional ByVal query As String = Nothing) As List(Of PostalCodeEntry)
        If Not _isLoaded Then
            Throw New InvalidOperationException("No data loaded. Call LoadOrDownload first.")
        End If

        If String.IsNullOrWhiteSpace(query) Then
            Return New List(Of PostalCodeEntry)(_postalCodes)
        End If

        Dim trimmed As String = query.Trim()
        Dim results As New List(Of PostalCodeEntry)()
        For Each p As PostalCodeEntry In _postalCodes
            If MatchesQuery(p, trimmed) Then
                results.Add(p)
            End If
        Next
        Return results
    End Function

    Private Shared Function MatchesQuery(ByVal p As PostalCodeEntry, ByVal q As String) As Boolean
        Return ContainsIgnoreCase(p.CodigoPostal, q) OrElse
               ContainsIgnoreCase(p.Asentamiento, q) OrElse
               ContainsIgnoreCase(p.Municipio, q) OrElse
               ContainsIgnoreCase(p.Estado, q)
    End Function

    Private Shared Function ContainsIgnoreCase(ByVal source As String, ByVal value As String) As Boolean
        If String.IsNullOrEmpty(source) Then Return False
        Return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0
    End Function

    Public Function ExportToJson(ByVal filePath As String) As String
        If Not _isLoaded Then
            Throw New InvalidOperationException("No data loaded. Call LoadOrDownload first.")
        End If
        Dim exporter As New PostalCodeExporter(_workingDir, _logger)
        exporter.ExportToJson(_postalCodes, filePath)
        Return filePath
    End Function

    Public Function ExportToCsv(ByVal filePath As String) As String
        If Not _isLoaded Then
            Throw New InvalidOperationException("No data loaded. Call LoadOrDownload first.")
        End If
        Dim exporter As New PostalCodeExporter(_workingDir, _logger)
        exporter.ExportToCsv(_postalCodes, filePath)
        Return filePath
    End Function

    Public Function ExportToXml(ByVal filePath As String) As String
        If Not _isLoaded Then
            Throw New InvalidOperationException("No data loaded. Call LoadOrDownload first.")
        End If
        Dim exporter As New PostalCodeExporter(_workingDir, _logger)
        exporter.ExportToXml(_postalCodes, filePath)
        Return filePath
    End Function

    Public Function BuildExportPath(ByVal extension As String) As String
        Dim exporter As New PostalCodeExporter(_workingDir, _logger)
        Return exporter.BuildExportPath(extension)
    End Function

    Friend Async Function DownloadAsync(Optional ByVal cancellationToken As CancellationToken = Nothing) As Task(Of String)
        Using scraper As New Scraper(_logger, _workingDir, _httpTimeoutSeconds)
            scraper.Host = SepomexHost
            scraper.Referer = SepomexRefererInitial

            Dim response As String = Await scraper.GetAsync(SepomexMainUrl, cancellationToken).ConfigureAwait(False)
            If String.IsNullOrEmpty(response) Then
                _logger.Log("Initial SEPOMEX GET returned empty body.", LogLevel.[Error])
                Return String.Empty
            End If

            Dim formSection As String = HtmlHelper.GetString(response,
                                                             startIn:="<input type=""hidden"" name=""__EVENTTARGET""",
                                                             endTo:="<nav class=""navbar")
            If String.IsNullOrEmpty(formSection) Then
                _logger.Log("Could not locate ASP.NET form fields in SEPOMEX page.", LogLevel.[Error])
                Return String.Empty
            End If

            Dim coordinates As HtmlHelper.Coordinates = HtmlHelper.GenerateButtonCoordinates(_random)
            Dim postData As String = HtmlHelper.BuildSepomexPostData(formSection, coordinates)

            scraper.Origin = SepomexOrigin
            scraper.Referer = SepomexRefererExport

            Return Await scraper.PostAsync(SepomexMainUrl, postData, cancellationToken).ConfigureAwait(False)
        End Using
    End Function

    Private Sub SetPostalCodes(ByVal codes As List(Of PostalCodeEntry))
        _postalCodes = codes
        _postalCodesReadOnly = Nothing
    End Sub

End Class
