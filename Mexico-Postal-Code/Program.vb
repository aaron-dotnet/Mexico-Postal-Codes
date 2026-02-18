Option Strict On
Option Infer Off

Imports System.IO

Imports Mexico_Postal_Code.c_Functions

Module Program
    Property DesktopPath As String = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)

    Sub Main()
        ' # Note: En vb.net no se puede usar async main

        Dim downloadPath As String = DownloadPostalCodes().GetAwaiter().GetResult()
        Dim txtFile As String = ExtractZip($"{DesktopPath}\CPdescargatxt.zip",
                                           $"{DesktopPath}\postal_codes\")

        ParseTextFile($"{DesktopPath}\postal_codes\CPdescarga.txt")
        Stop
    End Sub
    Public Function ParseTextFile(filePath As String) As Boolean
        ' response headers | Content-Type:Text/ html; charset=iso-8859-1 (Latin-1)
        Dim lines As String() = File.ReadAllLines(filePath, Text.Encoding.Latin1)

        ' quitar las dos primeras líneas que no contienen datos relevantes
        lines = lines.Skip(2).ToArray()

        Dim l_postalCodes As New List(Of c_PostalCode)

        For Each line As String In lines
            Dim fields As String() = line.Split("|"c)
            Dim postalCode As New c_PostalCode
            With postalCode
                .CodigoPostal = fields(0)
                .Asentamiento = fields(1)
                .TipoAsentamiento = fields(2)
                .Municipio = fields(3)
                .Estado = fields(4)
                .Ciudad = fields(5)
            End With
            l_postalCodes.Add(postalCode)
        Next

        Return True
    End Function

    Public Async Function DownloadPostalCodes() As Task(Of String)
        Dim mainUrl As String = "https://www.correosdemexico.gob.mx/SSLServicios/ConsultaCP/CodigoPostal_Exportar.aspx"
        Dim host As String = "www.correosdemexico.gob.mx"

        Using scraper As New c_Scraper()
            scraper.Host = host
            scraper.Referer = "https://www.correosdemexico.gob.mx/SSLServicios/ConsultaCP/Descarga.aspx"

            ' # Primera interacción con la página para obtener los datos necesarios para el Post
            Dim response As String = Await scraper.Get(mainUrl)
            Dim necessaryData As String = GetString(response,
                                                    startStr:="<input type=""hidden"" name=""__EVENTTARGET""",
                                                    endStr:="<nav class=""navbar",
                                                    firstCoincidence:=True)

            Dim postData As String = BuildPostData(necessaryData)

            scraper.Origin = "https://www.correosdemexico.gob.mx"
            scraper.Referer = "https://www.correosdemexico.gob.mx/SSLServicios/ConsultaCP/CodigoPostal_Exportar.aspx"

            ' # Segunda interacción, mandamos petición post con la data para descargar el archivo
            Dim filePath As String = Await scraper.Post(mainUrl, postData)
            Return filePath
        End Using
    End Function

    Public Function BuildPostData(ByRef htmlContent As String) As String
        Dim coor As (x_pos As Integer, y_pos As Integer) = GenerateCoordinates()
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
            $"btnDescarga.x={coor.x_pos}&" &
            $"btnDescarga.y={coor.y_pos}"

        Return post
    End Function

    Public Function GetInputValue(ByRef fullString As String, id As String) As String
        Dim start As String = $"{id}"" value="""
        Dim [end] As String = """ />"
        Dim result As String = GetString(fullString, start, [end],
                                         excessAmount:=[end].Length,
                                         firstCoincidence:=True).
                                         Replace(start, String.Empty)
        If String.IsNullOrEmpty(result) Then
            Return String.Empty
        End If

        ' Se hace un encoding al value antes de enviarlo en el Post
        Return System.Net.WebUtility.UrlEncode(result)
    End Function


    Public Function GenerateCoordinates() As (x_pos As Integer, y_pos As Integer)
        ' # Cordenadas asociadas a un boton de descarga de la pagina
        '   que registra donde se hizo clic (dentro del boton) para mandarlo en el Post
        Dim generator As New Random()
        Dim Y_POS As Integer = generator.Next(2, 22)
        Dim X_POS As Integer = generator.Next(2, 72)

        Return (X_POS, Y_POS)
    End Function
End Module