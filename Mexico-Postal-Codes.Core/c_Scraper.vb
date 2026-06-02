Option Strict On
Option Infer Off

Imports System.IO
Imports System.Net
Imports System.Net.Http
Imports System.Net.Http.Headers
Imports System.Text
Imports System.Threading.Tasks

Public Class c_Scraper
    Implements IDisposable

    Private ReadOnly _handler As HttpClientHandler
    Private ReadOnly _httpClient As HttpClient
    Private ReadOnly _logger As ILogger
    Private ReadOnly _cookieContainer As CookieContainer

    Public Property Host As String = String.Empty
    Public Property Referer As String = String.Empty
    Public Property Origin As String = String.Empty

    Public Sub New()
        Me.New(Nothing)
    End Sub

    Public Sub New(ByVal logger As ILogger)
        _logger = If(logger, NullLogger.Instance)
        _cookieContainer = New CookieContainer()
        _handler = New HttpClientHandler() With {
            .AutomaticDecompression = DecompressionMethods.GZip Or DecompressionMethods.Deflate,
            .CookieContainer = _cookieContainer,
            .UseCookies = True
        }
        _httpClient = New HttpClient(_handler)
    End Sub

    Private Sub SetHeaders()
        Const version As String = "149.0"

        With _httpClient.DefaultRequestHeaders
            If Not .Contains("User-Agent") Then
                .Add("User-Agent", $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{version}.0.0.0 Safari/537.36")
            End If
            If Not .Contains("Accept") Then
                .Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8")
            End If
            If Not .Contains("Accept-Language") Then
                .Add("Accept-Language", "es-MX,es;q=0.9,en-US;q=0.8,en;q=0.7")
            End If
            If Not .Contains("Accept-Encoding") Then
                .Add("Accept-Encoding", "gzip,deflate,br,zstd")
            End If
            If Not .Contains("Connection") Then
                .Add("Connection", "keep-alive")
            End If
            If Not String.IsNullOrEmpty(Origin) AndAlso Not .Contains("Origin") Then
                .Add("Origin", Origin)
            End If
            If Not String.IsNullOrEmpty(Host) Then
                .Host = Host
            End If
            If Not String.IsNullOrEmpty(Referer) Then
                .Referrer = New Uri(Referer)
            End If
        End With
    End Sub

    Public Async Function GetAsync(ByVal url As String) As Task(Of String)
        If String.IsNullOrEmpty(url) Then
            Throw New ArgumentException("URL cannot be null or empty.", NameOf(url))
        End If

        SetHeaders()
        Try
            Using response As HttpResponseMessage = Await _httpClient.GetAsync(url).ConfigureAwait(False)
                If response.IsSuccessStatusCode Then
                    Return Await response.Content.ReadAsStringAsync().ConfigureAwait(False)
                End If
                _logger.Log($"GET {url} failed with status {(CInt(response.StatusCode))} {response.ReasonPhrase}", LogLevel.Warning)
            End Using
        Catch ex As Exception
            _logger.Log($"GET {url} failed: {ex.Message}", LogLevel.[Error])
        End Try
        Return String.Empty
    End Function

    Public Async Function PostAsync(ByVal url As String, ByVal content As String) As Task(Of String)
        If String.IsNullOrEmpty(url) Then
            Throw New ArgumentException("URL cannot be null or empty.", NameOf(url))
        End If
        Dim httpContent As New StringContent(content, Encoding.UTF8, "application/x-www-form-urlencoded")
        Return Await InternalPostAsync(url, httpContent).ConfigureAwait(False)
    End Function

    Private Async Function InternalPostAsync(ByVal url As String, ByVal content As HttpContent) As Task(Of String)
        SetHeaders()
        Try
            Using response As HttpResponseMessage = Await _httpClient.PostAsync(url, content).ConfigureAwait(False)
                If response.IsSuccessStatusCode Then
                    Using respContent As HttpContent = response.Content
                        Dim contentDisposition As ContentDispositionHeaderValue = respContent.Headers.ContentDisposition
                        Dim contentType As String = If(respContent.Headers.ContentType?.MediaType, String.Empty)
                        Dim isFile As Boolean = False
                        Dim fileName As String = Nothing

                        If contentDisposition IsNot Nothing AndAlso Not String.IsNullOrEmpty(contentDisposition.FileName) Then
                            isFile = True
                            fileName = contentDisposition.FileName.Trim(""""c)
                        ElseIf Not String.IsNullOrEmpty(contentType) AndAlso
                                Not contentType.StartsWith("text", StringComparison.OrdinalIgnoreCase) AndAlso
                                Not contentType.Contains("json") AndAlso
                                Not contentType.Contains("xml") AndAlso
                                Not contentType.Contains("html") Then
                            isFile = True
                        End If

                        If isFile Then
                            Return Await DownloadFileAsync(fileName, contentType, respContent).ConfigureAwait(False)
                        Else
                            Return Await respContent.ReadAsStringAsync().ConfigureAwait(False)
                        End If
                    End Using
                End If
                _logger.Log($"POST {url} failed with status {(CInt(response.StatusCode))} {response.ReasonPhrase}", LogLevel.Warning)
            End Using
        Catch ex As Exception
            _logger.Log($"POST {url} failed: {ex.Message}", LogLevel.[Error])
        End Try
        Return String.Empty
    End Function

    Private Async Function DownloadFileAsync(ByVal fileName As String,
                                             ByVal contentType As String,
                                             ByVal respContent As HttpContent) As Task(Of String)
        Dim bytes As Byte() = Await respContent.ReadAsByteArrayAsync().ConfigureAwait(False)

        If String.IsNullOrEmpty(fileName) Then
            fileName = "download_" & DateTime.Now.ToString("yyyyMMddHHmmss")
            Select Case contentType
                Case "application/pdf"
                    fileName &= ".pdf"
                Case "application/zip"
                    fileName &= ".zip"
                Case "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                     "application/vnd.ms-excel"
                    fileName &= ".xlsx"
                Case "image/png"
                    fileName &= ".png"
                Case "image/jpeg"
                    fileName &= ".jpg"
                Case Else
                    If Not String.IsNullOrEmpty(contentType) AndAlso contentType.Contains("/"c) Then
                        Dim parts As String() = contentType.Split("/"c)
                        If parts.Length = 2 Then
                            fileName &= "." & parts(1)
                        End If
                    End If
            End Select
        End If

        Dim target As String = Path.Combine(AppContext.WorkingDirectory, fileName)
        If File.Exists(target) Then
            File.Delete(target)
        End If
        File.WriteAllBytes(target, bytes)

        Return target
    End Function

    Public Sub Dispose() Implements IDisposable.Dispose
        _httpClient?.Dispose()
        _handler?.Dispose()
        GC.SuppressFinalize(Me)
    End Sub

End Class
