Option Strict On
Option Infer Off

Module Program
    Public Structure s_Coordinates
        Public ReadOnly X_POS As Integer
        Public ReadOnly Y_POS As Integer
        Sub New(x As Integer, y As Integer)
            X_POS = x
            Y_POS = y
        End Sub
    End Structure

    Sub Main()
        GetPostalCodes().GetAwaiter().GetResult()
    End Sub

    Public Async Function GetPostalCodes() As Task
        Dim mainUrl As String = "https://www.correosdemexico.gob.mx/SSLServicios/ConsultaCP/CodigoPostal_Exportar.aspx"
        Dim host As String = "www.correosdemexico.gob.mx"

        Using scraper As New c_Scraper()
            scraper.Host = host
            scraper.Referer = "https://www.correosdemexico.gob.mx/SSLServicios/ConsultaCP/Descarga.aspx"

            ' primera interacción
            Dim response As String = Await scraper.Get(mainUrl)
            Dim necessaryData As String = GetString(response,
                                                    startStr:="<input type=""hidden"" name=""__EVENTTARGET""",
                                                    endStr:="<nav class=""navbar",
                                                    firstCoincidence:=True)

            Dim postData As String = BuildPostData(necessaryData)

            scraper.Origin = "https://www.correosdemexico.gob.mx"
            scraper.Referer = "https://www.correosdemexico.gob.mx/SSLServicios/ConsultaCP/CodigoPostal_Exportar.aspx"

            Dim response2 As String = Await scraper.Post(mainUrl, postData)
            Stop
        End Using

        'GetString("", "", 1)
    End Function

    Public Function BuildPostData(htmlContent As String) As String
        Dim coor As s_Coordinates = GenerateCoordinates()
        Dim fileType As String = "txt"

        ' En este caso, la página solo responde con valores estos inputs:
        ' VIEWSTATE, VIEWSTATEGENERATOR y EVENTVALIDATION,
        ' pero se agregarón los demás campos ya que existen en el HTML y se mandan tal cual en el Post.

        Dim post As String =
            $"__EVENTTARGET={GetInputValue(htmlContent, "__EVENTTARGET")}&" &
            $"__EVENTARGUMENT={GetInputValue(htmlContent, "__EVENTARGUMENT")}&" &
            $"__LASTFOCUS={GetInputValue(htmlContent, "__LASTFOCUS")}&" &
            $"__VIEWSTATE={GetInputValue(htmlContent, "__VIEWSTATE")}&" &
            $"__VIEWSTATEGENERATOR={GetInputValue(htmlContent, "__VIEWSTATEGENERATOR")}&" &
            $"__EVENTVALIDATION={GetInputValue(htmlContent, "__EVENTVALIDATION")}&" &
            $"cboEdo=00&" &
            $"rblTipo={fileType}&" &
            $"btnDescarga.x={coor.X_POS}&" &
            $"btnDescarga.y={coor.Y_POS}"

        Return post
    End Function

    Public Function GetInputValue(fullString As String, id As String) As String
        Dim start As String = $"{id}"" value="""
        Dim [end] As String = """ />"
        Dim result As String = GetString(fullString, start, [end],
                                         excessAmount:=[end].Length,
                                         firstCoincidence:=True).
                                         Replace(start, String.Empty)
        If String.IsNullOrEmpty(result) Then
            Return String.Empty
        End If

        Return System.Net.WebUtility.UrlEncode(result)
    End Function


    Public Function GenerateCoordinates() As s_Coordinates
        ' # Cordenadas asociadas a un boton de descarga de la pagina
        '   que registra donde se hizo clic (dentro del boton) para mandarlo en el Post
        Dim generator As New System.Random()
        Dim Y_POS As Integer = generator.Next(2S, 22S)
        Dim X_POS As Integer = generator.Next(2S, 72s)

        Return New s_Coordinates(X_POS, Y_POS)
    End Function

    Private Function GetString(fullString As String, startStr As String, endStr As String,
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
End Module