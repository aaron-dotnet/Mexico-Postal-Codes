Option Strict On
Option Infer Off

Imports System.IO
Imports System.Linq
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports Spectre.Console
Imports Mexico_Postal_Code.c_Functions

Module Program

    Sub Main()
        ' Set console title if possible
        Try
            Console.Title = "Mexico Postal Codes Scraper & Explorer"
        Catch
            ' Ignore if title is not supported on the host terminal
        End Try

        ' Main Header
        AnsiConsole.Clear()
        AnsiConsole.Write(
            New Panel("[bold green]MÉXICO POSTAL CODES SCRAPER & EXPLORER[/]" & vbCrLf & "[grey]Official SEPOMEX Database Scraper & Query Tool[/]") With {
                .Border = BoxBorder.Double,
                .Padding = New Padding(2, 1, 2, 1),
                .Header = New PanelHeader("SEPOMEX CLI v1.5", Justify.Right)
            }
        )

        Dim mylist As List(Of c_PostalCode) = New List(Of c_PostalCode)()
        Dim dbPath As String = Path.Combine(WorkingDirectory, "postal_codes", "CPdescarga.txt")

        If Not File.Exists(dbPath) Then
            AnsiConsole.MarkupLine("[yellow]Database not found locally. Starting automatic download...[/]")
            mylist = RunDownloadAndParseFlow()
        Else
            AnsiConsole.Status().Start("Loading local postal codes database...", Sub(ctx As StatusContext)
                mylist = ParseTextFile(dbPath)
            End Sub)
            AnsiConsole.MarkupLine($"[green]Loaded {mylist.Count:N0} postal codes from local cache.[/]")
        End If

        If mylist.Count = 0 Then
            AnsiConsole.MarkupLine("[bold red]Failed to load any postal codes. Please check your internet connection or logs.[/]")
            AnsiConsole.MarkupLine("Press any key to exit...")
            Console.ReadKey()
            Return
        End If

        ' Main loop
        Dim exitApp As Boolean = False
        While Not exitApp
            AnsiConsole.WriteLine()
            
            Dim choices As String() = New String() {
                "Search & Browse Postal Codes",
                "View Statistics",
                "Export Dataset",
                "Refresh/Re-download Data",
                "Exit"
            }
            
            Dim choice As String = SelectOption("[yellow]Select an option:[/]", choices)

            Select Case choice
                Case "Search & Browse Postal Codes"
                    BrowsePostalCodes(mylist)
                Case "View Statistics"
                    ShowStatistics(mylist)
                Case "Export Dataset"
                    ExportDataset(mylist)
                Case "Refresh/Re-download Data"
                    If ConfirmChoice("[yellow]Are you sure you want to re-download the database? This might take a few seconds.[/]") Then
                        mylist = RunDownloadAndParseFlow()
                    End If
                Case "Exit"
                    exitApp = True
                    AnsiConsole.MarkupLine("[bold green]Goodbye![/] ¡Adiós!")
            End Select
        End While
    End Sub

    Public Function RunDownloadAndParseFlow() As List(Of c_PostalCode)
        Dim downloadPath As String = String.Empty
        Dim txtFile As String = String.Empty
        Dim mylist As List(Of c_PostalCode) = New List(Of c_PostalCode)()

        Try
            ' 1. Download
            AnsiConsole.Status().Start("[bold yellow]Downloading latest postal codes from SEPOMEX...[/]", Sub(ctx As StatusContext)
                downloadPath = DownloadPostalCodes().GetAwaiter().GetResult()
            End Sub)

            If String.IsNullOrEmpty(downloadPath) Then
                AnsiConsole.MarkupLine("[red]Error: Download failed.[/]")
                Return mylist
            End If

            ' 2. Extract
            AnsiConsole.Status().Start("[bold yellow]Extracting zip package...[/]", Sub(ctx As StatusContext)
                Dim zipFileToExtract As String = Path.Combine(WorkingDirectory, "CPdescargatxt.zip")
                If Not File.Exists(zipFileToExtract) AndAlso File.Exists(downloadPath) Then
                    zipFileToExtract = downloadPath
                End If
                txtFile = ExtractZip(zipFileToExtract, Path.Combine(WorkingDirectory, "postal_codes"))
            End Sub)

            If String.IsNullOrEmpty(txtFile) OrElse Not File.Exists(txtFile) Then
                AnsiConsole.MarkupLine("[red]Error: Extraction failed.[/]")
                Return mylist
            End If

            ' 3. Parse
            AnsiConsole.Status().Start("[bold yellow]Parsing postal codes dataset...[/]", Sub(ctx As StatusContext)
                mylist = ParseTextFile(txtFile)
            End Sub)

            AnsiConsole.MarkupLine($"[bold green]✓ Successfully downloaded and parsed {mylist.Count:N0} postal codes![/]")

        Catch ex As Exception
            AnsiConsole.MarkupLine("[red]An error occurred during download/parse flow:[/]")
            AnsiConsole.WriteException(ex)
        End Try

        Return mylist
    End Function

    Public Function ParseTextFile(filePath As String) As List(Of c_PostalCode)
        Dim l_postalCodes As List(Of c_PostalCode) = New List(Of c_PostalCode)()
        If Not File.Exists(filePath) Then
            Return l_postalCodes
        End If

        ' Use Latin1 (iso-8859-1) encoding as specified in the original code
        Using reader As New StreamReader(filePath, System.Text.Encoding.Latin1)
            ' Skip first two lines (metadata and headers)
            reader.ReadLine()
            reader.ReadLine()

            While Not reader.EndOfStream
                Dim line As String = reader.ReadLine()
                If String.IsNullOrWhiteSpace(line) Then Continue While

                Dim fields As String() = line.Split("|"c)
                If fields.Length >= 6 Then
                    Dim postalCode As New c_PostalCode()
                    With postalCode
                        .CodigoPostal = fields(0)
                        .Asentamiento = fields(1)
                        .TipoAsentamiento = fields(2)
                        .Municipio = fields(3)
                        .Estado = fields(4)
                        .Ciudad = fields(5)
                        If fields.Length >= 15 Then
                            .D_CP = fields(6)
                            .c_Estado = fields(7)
                            .c_Oficina = fields(8)
                            .c_CP = fields(9)
                            .c_TipoAsentamiento = fields(10)
                            .c_Municipio = fields(11)
                            .id_Asentamiento_cpcons = fields(12)
                            .d_zona = fields(13)
                            .c_cve_ciudad = fields(14)
                        End If
                    End With
                    l_postalCodes.Add(postalCode)
                End If
            End While
        End Using

        Return l_postalCodes
    End Function

    Private Sub BrowsePostalCodes(mylist As List(Of c_PostalCode))
        Dim searchAgain As Boolean = True
        While searchAgain
            Dim query As String = String.Empty
            
            DrainInputBuffer()
            Try
                If AnsiConsole.Profile.Capabilities.Interactive Then
                    Dim queryPrompt As New TextPrompt(Of String)("[green]Enter search query (or press Enter to browse all):[/]")
                    queryPrompt.AllowEmpty = True
                    query = AnsiConsole.Prompt(queryPrompt)
                Else
                    Console.Write("Enter search query (or press Enter to browse all): ")
                    query = Console.ReadLine()
                End If
            Catch
                Console.Write("Enter search query (or press Enter to browse all): ")
                query = Console.ReadLine()
            End Try

            Dim filtered As List(Of c_PostalCode) = mylist
            If Not String.IsNullOrWhiteSpace(query) Then
                Dim q As String = query.ToLowerInvariant()
                filtered = New List(Of c_PostalCode)()
                For Each p As c_PostalCode In mylist
                    If (p.CodigoPostal IsNot Nothing AndAlso p.CodigoPostal.ToLowerInvariant().Contains(q)) OrElse
                       (p.Asentamiento IsNot Nothing AndAlso p.Asentamiento.ToLowerInvariant().Contains(q)) OrElse
                       (p.Municipio IsNot Nothing AndAlso p.Municipio.ToLowerInvariant().Contains(q)) OrElse
                       (p.Estado IsNot Nothing AndAlso p.Estado.ToLowerInvariant().Contains(q)) Then
                        filtered.Add(p)
                    End If
                Next
            End If

            If filtered.Count = 0 Then
                AnsiConsole.MarkupLine("[red]No postal codes found matching the query.[/]")
                searchAgain = ConfirmChoice("Try another search?")
                Continue While
            End If

            Dim pageSize As Integer = 15
            Dim pageIndex As Integer = 0
            Dim totalPages As Integer = CInt(Math.Ceiling(filtered.Count / pageSize))

            Dim stayInPagination As Boolean = True
            While stayInPagination
                AnsiConsole.Clear()
                Dim title As String = If(String.IsNullOrWhiteSpace(query), "All Postal Codes", $"Search Results for '{query}'")
                AnsiConsole.Write(New Rule($"[yellow]{title} - Page {pageIndex + 1} of {totalPages} (Total: {filtered.Count:N0})[/]"))
                AnsiConsole.WriteLine()

                Dim table As Table = New Table()
                table.Border = TableBorder.Rounded
                table.AddColumn("[bold blue]CP[/]")
                table.AddColumn("[bold blue]Settlement[/]")
                table.AddColumn("[bold blue]Type[/]")
                table.AddColumn("[bold blue]Municipio[/]")
                table.AddColumn("[bold blue]State[/]")
                table.AddColumn("[bold blue]Zone[/]")

                Dim startIndex As Integer = pageIndex * pageSize
                Dim endIndex As Integer = Math.Min(startIndex + pageSize, filtered.Count)

                For idx As Integer = startIndex To endIndex - 1
                    Dim p As c_PostalCode = filtered(idx)
                    table.AddRow(
                        If(p.CodigoPostal, String.Empty),
                        If(p.Asentamiento, String.Empty),
                        If(p.TipoAsentamiento, String.Empty),
                        If(p.Municipio, String.Empty),
                        If(p.Estado, String.Empty),
                        If(p.d_zona, "N/A")
                    )
                Next

                AnsiConsole.Write(table)

                Dim choices As List(Of String) = New List(Of String)()
                If pageIndex < totalPages - 1 Then choices.Add("Next Page")
                If pageIndex > 0 Then choices.Add("Previous Page")
                choices.Add("New Search")
                choices.Add("Back to Main Menu")

                Dim choice As String = SelectOption("[yellow]Navigate:[/]", choices.ToArray())

                If choice = "Next Page" Then
                    pageIndex += 1
                ElseIf choice = "Previous Page" Then
                    pageIndex -= 1
                ElseIf choice = "New Search" Then
                    stayInPagination = False
                ElseIf choice = "Back to Main Menu" Then
                    stayInPagination = False
                    searchAgain = False
                End If
            End While
        End While
    End Sub

    Private Sub ShowStatistics(mylist As List(Of c_PostalCode))
        AnsiConsole.Clear()
        AnsiConsole.Write(New Rule("[yellow]Database Statistics[/]"))
        AnsiConsole.WriteLine()

        Dim totalCodes As Integer = mylist.Count
        
        Dim statesHash As HashSet(Of String) = New HashSet(Of String)()
        Dim municipalitiesHash As HashSet(Of String) = New HashSet(Of String)()
        Dim settlementsHash As HashSet(Of String) = New HashSet(Of String)()
        
        For Each p As c_PostalCode In mylist
            If Not String.IsNullOrEmpty(p.Estado) Then statesHash.Add(p.Estado)
            If Not String.IsNullOrEmpty(p.Municipio) Then municipalitiesHash.Add(p.Municipio)
            If Not String.IsNullOrEmpty(p.Asentamiento) Then settlementsHash.Add(p.Asentamiento)
        Next

        Dim summaryTable As Table = New Table()
        summaryTable.Border = TableBorder.DoubleEdge
        summaryTable.Title = New TableTitle("[bold green]Overview[/]")
        summaryTable.AddColumn("[bold blue]Metric[/]")
        summaryTable.AddColumn("[bold blue]Count[/]")
        
        summaryTable.AddRow("Total Postal Records", totalCodes.ToString("N0"))
        summaryTable.AddRow("Unique States", statesHash.Count.ToString("N0"))
        summaryTable.AddRow("Unique Municipalities", municipalitiesHash.Count.ToString("N0"))
        summaryTable.AddRow("Unique Settlement Names", settlementsHash.Count.ToString("N0"))
        
        AnsiConsole.Write(summaryTable)
        AnsiConsole.WriteLine()

        Dim stateGroups As Dictionary(Of String, Integer) = New Dictionary(Of String, Integer)()
        For Each p As c_PostalCode In mylist
            If Not String.IsNullOrEmpty(p.Estado) Then
                If stateGroups.ContainsKey(p.Estado) Then
                    stateGroups(p.Estado) = stateGroups(p.Estado) + 1
                Else
                    stateGroups(p.Estado) = 1
                End If
            End If
        Next

        Dim sortedStates As List(Of KeyValuePair(Of String, Integer)) = _
            stateGroups.OrderByDescending(Function(x As KeyValuePair(Of String, Integer)) x.Value) _
                       .Take(10) _
                       .ToList()

        Dim statesTable As Table = New Table()
        statesTable.Border = TableBorder.Rounded
        statesTable.Title = New TableTitle("[bold green]Top 10 States by Record Count[/]")
        statesTable.AddColumn("[bold blue]State[/]")
        statesTable.AddColumn("[bold blue]Records[/]")
        statesTable.AddColumn("[bold blue]Percentage[/]")

        For Each s As KeyValuePair(Of String, Integer) In sortedStates
            Dim percentage As Double = (s.Value / totalCodes) * 100
            statesTable.AddRow(s.Key, s.Value.ToString("N0"), $"{percentage:F2}%")
        Next

        AnsiConsole.Write(statesTable)
        AnsiConsole.WriteLine()

        Dim chart As BarChart = New BarChart()
        chart.Width = 60
        chart.Label = "[bold green]Distribution of Top 5 States[/]"

        Dim colors As Color() = { Color.Red, Color.Green, Color.Blue, Color.Yellow, Color.Aqua }
        Dim colorIndex As Integer = 0
        For Each s As KeyValuePair(Of String, Integer) In sortedStates.Take(5)
            chart.AddItem(s.Key, s.Value, colors(colorIndex Mod colors.Length))
            colorIndex += 1
        Next

        AnsiConsole.Write(chart)
        AnsiConsole.WriteLine()

        Dim typeGroups As Dictionary(Of String, Integer) = New Dictionary(Of String, Integer)()
        For Each p As c_PostalCode In mylist
            If Not String.IsNullOrEmpty(p.TipoAsentamiento) Then
                If typeGroups.ContainsKey(p.TipoAsentamiento) Then
                    typeGroups(p.TipoAsentamiento) = typeGroups(p.TipoAsentamiento) + 1
                Else
                    typeGroups(p.TipoAsentamiento) = 1
                End If
            End If
        Next

        Dim sortedTypes As List(Of KeyValuePair(Of String, Integer)) = _
            typeGroups.OrderByDescending(Function(x As KeyValuePair(Of String, Integer)) x.Value) _
                       .Take(10) _
                       .ToList()

        Dim typesTable As Table = New Table()
        typesTable.Border = TableBorder.Rounded
        typesTable.Title = New TableTitle("[bold green]Top 10 Settlement Types[/]")
        typesTable.AddColumn("[bold blue]Settlement Type[/]")
        typesTable.AddColumn("[bold blue]Records[/]")
        typesTable.AddColumn("[bold blue]Percentage[/]")

        For Each t As KeyValuePair(Of String, Integer) In sortedTypes
            Dim percentage As Double = (t.Value / totalCodes) * 100
            typesTable.AddRow(t.Key, t.Value.ToString("N0"), $"{percentage:F2}%")
        Next

        AnsiConsole.Write(typesTable)
        AnsiConsole.WriteLine()

        AnsiConsole.MarkupLine("[grey]Press any key to return to main menu...[/]")
        Console.ReadKey(True)
    End Sub

    Private Sub ExportDataset(mylist As List(Of c_PostalCode))
        AnsiConsole.Clear()
        AnsiConsole.Write(New Rule("[yellow]Export Dataset[/]"))
        AnsiConsole.WriteLine()

        Dim choices As String() = New String() {
            "JSON (Highly detailed, formatted)",
            "CSV (Comma Separated Values)",
            "XML (Extensible Markup Language)",
            "Cancel"
        }

        Dim format As String = SelectOption("[green]Choose export format:[/]", choices)

        If format = "Cancel" Then Return

        Dim exportDir As String = Path.Combine(WorkingDirectory, "exports")
        Try
            Directory.CreateDirectory(exportDir)
        Catch ex As Exception
            AnsiConsole.MarkupLine($"[red]Error creating directory {exportDir}: {ex.Message}[/]")
            Return
        End Try

        Dim fileName As String = "mexico_postal_codes_" & DateTime.Now.ToString("yyyyMMdd_HHmmss")
        Dim fullPath As String = String.Empty

        Try
            Select Case format
                Case "JSON (Highly detailed, formatted)"
                    fileName &= ".json"
                    fullPath = Path.Combine(exportDir, fileName)
                    AnsiConsole.Status().Start("Serializing dataset to JSON...", Sub(ctx As StatusContext)
                        Dim options As New System.Text.Json.JsonSerializerOptions() With {
                            .WriteIndented = True,
                            .Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                        }
                        Dim jsonString As String = System.Text.Json.JsonSerializer.Serialize(mylist, options)
                        File.WriteAllText(fullPath, jsonString, System.Text.Encoding.UTF8)
                    End Sub)

                Case "CSV (Comma Separated Values)"
                    fileName &= ".csv"
                    fullPath = Path.Combine(exportDir, fileName)
                    AnsiConsole.Status().Start("Serializing dataset to CSV...", Sub(ctx As StatusContext)
                        Using writer As New StreamWriter(fullPath, False, System.Text.Encoding.UTF8)
                            writer.WriteLine("CodigoPostal,Asentamiento,TipoAsentamiento,Municipio,Estado,Ciudad,D_CP,c_Estado,c_Oficina,c_CP,c_TipoAsentamiento,c_Municipio,id_Asentamiento_cpcons,d_zona,c_cve_ciudad")
                            For Each p As c_PostalCode In mylist
                                Dim line As String = $"{EscapeCsv(p.CodigoPostal)},{EscapeCsv(p.Asentamiento)},{EscapeCsv(p.TipoAsentamiento)},{EscapeCsv(p.Municipio)},{EscapeCsv(p.Estado)},{EscapeCsv(p.Ciudad)},{EscapeCsv(p.D_CP)},{EscapeCsv(p.c_Estado)},{EscapeCsv(p.c_Oficina)},{EscapeCsv(p.c_CP)},{EscapeCsv(p.c_TipoAsentamiento)},{EscapeCsv(p.c_Municipio)},{EscapeCsv(p.id_Asentamiento_cpcons)},{EscapeCsv(p.d_zona)},{EscapeCsv(p.c_cve_ciudad)}"
                                writer.WriteLine(line)
                            Next
                        End Using
                    End Sub)

                Case "XML (Extensible Markup Language)"
                    fileName &= ".xml"
                    fullPath = Path.Combine(exportDir, fileName)
                    AnsiConsole.Status().Start("Serializing dataset to XML...", Sub(ctx As StatusContext)
                        Dim doc As New System.Xml.XmlDocument()
                        Dim root As System.Xml.XmlElement = doc.CreateElement("PostalCodes")
                        doc.AppendChild(root)

                        For Each p As c_PostalCode In mylist
                            Dim el As System.Xml.XmlElement = doc.CreateElement("PostalCode")
                            el.SetAttribute("Code", p.CodigoPostal)
                            el.SetAttribute("Settlement", p.Asentamiento)
                            el.SetAttribute("Type", p.TipoAsentamiento)
                            el.SetAttribute("Municipio", p.Municipio)
                            el.SetAttribute("State", p.Estado)
                            el.SetAttribute("City", p.Ciudad)
                            el.SetAttribute("D_CP", p.D_CP)
                            el.SetAttribute("c_Estado", p.c_Estado)
                            el.SetAttribute("c_Oficina", p.c_Oficina)
                            el.SetAttribute("c_CP", p.c_CP)
                            el.SetAttribute("c_TipoAsentamiento", p.c_TipoAsentamiento)
                            el.SetAttribute("c_Municipio", p.c_Municipio)
                            el.SetAttribute("id_Asentamiento_cpcons", p.id_Asentamiento_cpcons)
                            el.SetAttribute("d_zona", p.d_zona)
                            el.SetAttribute("c_cve_ciudad", p.c_cve_ciudad)
                            root.AppendChild(el)
                        Next
                        doc.Save(fullPath)
                    End Sub)
            End Select

            AnsiConsole.MarkupLine($"[bold green]✓ Dataset successfully exported to:[/] [cyan]{fullPath}[/]")

        Catch ex As Exception
            AnsiConsole.MarkupLine("[bold red]Failed to export dataset.[/]")
            AnsiConsole.WriteException(ex)
        End Try

        AnsiConsole.MarkupLine("[grey]Press any key to return to main menu...[/]")
        Console.ReadKey(True)
    End Sub

    Private Function EscapeCsv(value As String) As String
        If String.IsNullOrEmpty(value) Then Return String.Empty
        If value.Contains(","c) OrElse value.Contains(""""c) OrElse value.Contains(Environment.NewLine) Then
            Return $"""{value.Replace("""", """""")}"""
        End If
        Return value
    End Function

    Public Function DownloadPostalCodes() As Task(Of String)
        Dim mainUrl As String = "https://www.correosdemexico.gob.mx/SSLServicios/ConsultaCP/CodigoPostal_Exportar.aspx"
        Dim host As String = "www.correosdemexico.gob.mx"

        Dim downloadTask As Task(Of String) = Task.Run(Async Function() As Task(Of String)
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
        End Function)

        Return downloadTask
    End Function

    Public Function BuildPostData(ByRef htmlContent As String) As String
        Dim coor As (x_pos As Integer, y_pos As Integer) = GenerateCoordinates()
        Dim fileType As String = "txt"

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

        Return System.Net.WebUtility.UrlEncode(result)
    End Function

    Public Function GenerateCoordinates() As (x_pos As Integer, y_pos As Integer)
        Dim generator As New Random()
        Dim Y_POS As Integer = generator.Next(2, 22)
        Dim X_POS As Integer = generator.Next(2, 72)

        Return (X_POS, Y_POS)
    End Function

    Private Sub DrainInputBuffer()
        Try
            While Console.KeyAvailable
                Console.ReadKey(True)
            End While
        Catch
            ' Ignore if KeyAvailable is not supported
        End Try
    End Sub

    ' Graceful fallback methods for non-interactive environments
    Private Function SelectOption(title As String, choices As String()) As String
        Try
            If AnsiConsole.Profile.Capabilities.Interactive Then
                Dim menuPrompt As New SelectionPrompt(Of String)()
                menuPrompt.Title = title
                menuPrompt.PageSize = 10
                menuPrompt.AddChoices(choices)
                Return AnsiConsole.Prompt(menuPrompt)
            End If
        Catch ex As Exception
            ' Fall through to fallback
        End Try

        AnsiConsole.MarkupLine(title)
        For idx As Integer = 0 To choices.Length - 1
            Console.WriteLine($"  {idx + 1}. {choices(idx)}")
        Next
        
        DrainInputBuffer()

        While True
            Console.Write("Enter choice number: ")
            Dim input As String = Console.ReadLine()
            If String.IsNullOrEmpty(input) Then
                Continue While
            End If
            Dim choiceIdx As Integer = 0
            If Integer.TryParse(input, choiceIdx) AndAlso choiceIdx >= 1 AndAlso choiceIdx <= choices.Length Then
                Return choices(choiceIdx - 1)
            End If
            Console.WriteLine("Invalid choice. Try again.")
        End While
        Return String.Empty
    End Function

    Private Function ConfirmChoice(promptText As String) As Boolean
        Try
            If AnsiConsole.Profile.Capabilities.Interactive Then
                Return AnsiConsole.Confirm(promptText)
            End If
        Catch
            ' Fall through to fallback
        End Try

        DrainInputBuffer()
        AnsiConsole.Markup(promptText & " (y/n): ")
        Dim input As String = Console.ReadLine()
        If String.IsNullOrEmpty(input) Then Return False
        input = input.Trim().ToLowerInvariant()
        Return input = "y" OrElse input = "yes"
    End Function
End Module