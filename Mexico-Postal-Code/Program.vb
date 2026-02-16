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
        PrimeraParte().GetAwaiter().GetResult()
    End Sub

    Public Async Function PrimeraParte() As Task
        Dim main_url As String = "https://www.correosdemexico.gob.mx/SSLServicios/ConsultaCP/CodigoPostal_Exportar.aspx"

        Using scraper As New c_Scraper()
            scraper.Host = "www.correosdemexico.gob.mx"
            scraper.Referer = "https://www.correosdemexico.gob.mx/SSLServicios/ConsultaCP/Descarga.aspx"

            ' primera interacción
            Dim response As String = Await scraper.GET(main_url)

            Dim htmlContent As String = GetString(response,
                                                  startStr:="<input type=""hidden"" name=""__EVENTTARGET""",
                                                  endStr:="<nav class=""navbar",
                                                  firstCoincidence:=True)

            Dim post As String = BuildPostData(htmlContent)

            scraper.POST("", "")
        End Using

        'GetString("", "", 1)
    End Function

    Public Function BuildPostData(htmlContent As String) As String
        Dim coor As s_Coordinates = GenerarCordenadas()

        Dim post As String =
            $"__EVENTTARGET={GetInputValue(htmlContent, "__EVENTTARGET")}&" &
            $"__EVENTARGUMENT={GetInputValue(htmlContent, "__EVENTARGUMENT")}&" &
            $"__LASTFOCUS={GetInputValue(htmlContent, "__LASTFOCUS")}&" &
            $"__VIEWSTATE={GetInputValue(htmlContent, "__VIEWSTATE")}&" &
            $"__VIEWSTATEGENERATOR={GetInputValue(htmlContent, "__VIEWSTATEGENERATOR")}&" &
            $"__EVENTVALIDATION={GetInputValue(htmlContent, "__EVENTVALIDATION")}&" &
            $"cboEdo=00&" &
            $"rblTipo=txt&" &
            $"btnDescarga.x={coor.X_POS}&" &
            $"btnDescarga.y={coor.Y_POS}"

        ' Formateamos 
        Return post
    End Function

    Public Function GetInputValue(fullString As String, id As String) As String
        Dim start As String = $"{id}"" value="""
        Dim [end] As String = """ />"
        Dim result As String = GetString(fullString, start, [end],
                                         excessAmount:=[end].Length,
                                         firstCoincidence:=True).
                                         Replace(start, String.Empty)

        Return result
    End Function


    Public Function GenerarCordenadas() As s_Coordinates
        ' # Cordenadas asociadas a un boton de la pagina que registra donde se hizo clic
        '   para mandarlo en el Post
        Dim generator As New System.Random()
        Dim Y_POS As Integer = generator.Next(2, 22)
        Dim X_POS As Integer = generator.Next(2, 72)

        Return New s_Coordinates(X_POS, Y_POS)
    End Function

    Private Function GetString(fullString As String, startStr As String, endStr As String,
                               Optional excessAmount As Integer = 0,
                               Optional firstCoincidence As Boolean = False) As String

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