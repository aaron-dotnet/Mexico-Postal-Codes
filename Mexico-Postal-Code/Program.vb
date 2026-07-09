Option Strict On
Option Infer Off

Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports Spectre.Console
Imports Mexico_Postal_Code.Core

Module Program

    Sub Main()
        ConfigureLogger()
        ConfigureConsole()
        ShowWelcomeHeader()

        Dim service As New PostalCodeService()
        LoadWithStatus(service, "Loading postal codes database...")

        If Not service.IsLoaded OrElse service.PostalCodes.Count = 0 Then
            AnsiConsole.MarkupLine("[bold red]Failed to load any postal codes. Please check your internet connection or logs.[/]")
            AnsiConsole.MarkupLine("Press any key to exit...")
            Console.ReadKey()
            Return
        End If

        RunMainLoop(service)
    End Sub

    Private Sub LoadWithStatus(ByVal service As PostalCodeService, ByVal statusMessage As String)
        AnsiConsole.Status().Start(statusMessage,
                                   Sub(ctx As StatusContext)
                                       service.LoadOrDownload()
                                   End Sub)
        AnsiConsole.MarkupLine($"[green]Loaded {service.PostalCodes.Count:N0} postal codes.[/]")
    End Sub

    Private Sub RefreshWithStatus(ByVal service As PostalCodeService)
        AnsiConsole.Status().Start("[bold yellow]Re-downloading database from SEPOMEX...[/]",
                                   Sub(ctx As StatusContext)
                                       service.Refresh()
                                   End Sub)

        If service.IsLoaded Then
            AnsiConsole.MarkupLine($"[bold green]✓ Successfully refreshed {service.PostalCodes.Count:N0} postal codes![/]")
        Else
            AnsiConsole.MarkupLine("[bold red]Re-download failed. The database could not be updated. Check the logs for details.[/]")
            If service.HasCachedDatabase() Then
                AnsiConsole.MarkupLine("[yellow]Falling back to previous local cache.[/]")
                service.LoadOrDownload()
            End If
            If Not service.IsLoaded Then
                AnsiConsole.MarkupLine("[yellow]No local cache available. Continuing with an empty dataset.[/]")
            End If
        End If
    End Sub

    Private Sub ConfigureLogger()
        Dim logFile As String = Path.Combine(AppContext.WorkingDirectory, "MexicoPostalCodes.log")
        AppContext.Logger = New FileLogger(logFile)
    End Sub

    Private Sub ConfigureConsole()
        Try
            Console.Title = "Mexico Postal Codes Scraper & Explorer"
        Catch
        End Try
    End Sub

    Private Sub ShowWelcomeHeader()
        AnsiConsole.Clear()
        AnsiConsole.Write(
            New Panel("[bold green]MÉXICO POSTAL CODES SCRAPER & EXPLORER[/]" & vbCrLf & "[grey]Official SEPOMEX Database Scraper & Query Tool[/]") With {
                .Border = BoxBorder.Double,
                .Padding = New Padding(2, 1, 2, 1),
                .Header = New PanelHeader("SEPOMEX CLI v1.5", Justify.Right)
            }
        )
    End Sub

    Private Sub RunMainLoop(ByVal service As PostalCodeService)
        Dim exitApp As Boolean = False
        Dim choices As String() = New String() {
            "Search & Browse Postal Codes",
            "View Statistics",
            "Export Dataset",
            "Refresh/Re-download Data",
            "Exit"
        }

        While Not exitApp
            AnsiConsole.WriteLine()
            Dim choice As String = SelectOption("[yellow]Select an option:[/]", choices)

            Select Case choice
                Case "Search & Browse Postal Codes"
                    Try
                        BrowsePostalCodes(service)
                    Catch ex As InvalidOperationException
                        AnsiConsole.MarkupLine($"[red]{ex.Message}[/]")
                    End Try
                Case "View Statistics"
                    ShowStatistics(service)
                Case "Export Dataset"
                    ExportDataset(service)
                Case "Refresh/Re-download Data"
                    If ConfirmChoice("[yellow]Are you sure you want to re-download the database? This might take a few seconds.[/]") Then
                        RefreshWithStatus(service)
                    End If
                Case "Exit"
                    exitApp = True
                    AnsiConsole.MarkupLine("[bold green]Goodbye![/] ¡Adiós!")
            End Select
        End While
    End Sub

    Private Sub BrowsePostalCodes(ByVal service As PostalCodeService)
        Dim searchAgain As Boolean = True
        While searchAgain
            Dim query As String = PromptSearchQuery()
            Dim filtered As List(Of PostalCodeEntry) = service.Search(query)

            If filtered.Count = 0 Then
                AnsiConsole.MarkupLine("[red]No postal codes found matching the query.[/]")
                searchAgain = ConfirmChoice("Try another search?")
                Continue While
            End If

            searchAgain = PaginateAndDisplayResults(filtered, query)
        End While
    End Sub

    Private Function PromptSearchQuery() As String
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
        Return query
    End Function

    Private Function PaginateAndDisplayResults(ByVal filtered As List(Of PostalCodeEntry), ByVal query As String) As Boolean
        Dim pageSize As Integer = 15
        Dim pageIndex As Integer = 0
        Dim totalPages As Integer = CInt(Math.Ceiling(filtered.Count / CDbl(pageSize)))
        Dim stayInPagination As Boolean = True
        Dim searchAgain As Boolean = True

        While stayInPagination
            AnsiConsole.Clear()
            Dim title As String = If(String.IsNullOrWhiteSpace(query), "All Postal Codes", $"Search Results for '{query}'")
            AnsiConsole.Write(New Rule($"[yellow]{title} - Page {pageIndex + 1} of {totalPages} (Total: {filtered.Count:N0})[/]"))
            AnsiConsole.WriteLine()

            RenderResultsTable(filtered, pageIndex, pageSize)

            Dim choices As New List(Of String)()
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
                searchAgain = True
            ElseIf choice = "Back to Main Menu" Then
                stayInPagination = False
                searchAgain = False
            End If
        End While

        Return searchAgain
    End Function

    Private Sub RenderResultsTable(ByVal filtered As List(Of PostalCodeEntry), ByVal pageIndex As Integer, ByVal pageSize As Integer)
        Dim table As New Table()
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
            Dim p As PostalCodeEntry = filtered(idx)
            table.AddRow(
                If(p.CodigoPostal, String.Empty),
                If(FixToPrint(p.Asentamiento), String.Empty),
                If(FixToPrint(p.TipoAsentamiento), String.Empty),
                If(FixToPrint(p.Municipio), String.Empty),
                If(p.Estado, String.Empty),
                If(p.d_zona, "N/A")
            )
        Next

        AnsiConsole.Write(table)
    End Sub

    Private Function FixToPrint(ByVal str As String) As String
        If String.IsNullOrEmpty(str) Then Return str
        If str.Contains("[", StringComparison.OrdinalIgnoreCase) Then
            Return str.Replace("[", "[[").Replace("]", "]]")
        End If
        Return str
    End Function

    Private Sub ShowStatistics(ByVal service As PostalCodeService)
        AnsiConsole.Clear()
        AnsiConsole.Write(New Rule("[yellow]Database Statistics[/]"))
        AnsiConsole.WriteLine()

        Dim stats As PostalCodeStatistics = PostalCodeStatistics.Compute(service.PostalCodes)
        RenderOverviewTable(stats)
        AnsiConsole.WriteLine()
        RenderTopStatesTable(stats)
        AnsiConsole.WriteLine()
        RenderDistributionChart(stats.TopStates.Take(5).ToList())
        AnsiConsole.WriteLine()
        RenderSettlementTypesTable(stats)
        AnsiConsole.WriteLine()

        AnsiConsole.MarkupLine("[grey]Press any key to return to main menu...[/]")
        Console.ReadKey(True)
    End Sub

    Private Sub RenderOverviewTable(ByVal stats As PostalCodeStatistics)
        Dim summaryTable As New Table()
        With summaryTable
            .Border = TableBorder.DoubleEdge
            .Title = New TableTitle("[bold green]Overview[/]")
            .AddColumn("[bold blue]Metric[/]")
            .AddColumn("[bold blue]Count[/]")
            .AddRow("Total Postal Records", stats.TotalRecords.ToString("N0"))
            .AddRow("Unique States", stats.UniqueStates.ToString("N0"))
            .AddRow("Unique Municipalities", stats.UniqueMunicipalities.ToString("N0"))
            .AddRow("Unique Settlement Names", stats.UniqueSettlements.ToString("N0"))
        End With
        AnsiConsole.Write(summaryTable)
    End Sub

    Private Sub RenderTopStatesTable(ByVal stats As PostalCodeStatistics)
        Dim statesTable As New Table()
        statesTable.Border = TableBorder.Rounded
        statesTable.Title = New TableTitle("[bold green]Top 10 States by Record Count[/]")
        statesTable.AddColumn("[bold blue]State[/]")
        statesTable.AddColumn("[bold blue]Records[/]")
        statesTable.AddColumn("[bold blue]Percentage[/]")

        For Each s As KeyValuePair(Of String, Integer) In stats.TopStates
            statesTable.AddRow(s.Key, s.Value.ToString("N0"), $"{Percentage(s.Value, stats.TotalRecords):F2}%")
        Next

        AnsiConsole.Write(statesTable)
    End Sub

    Private Sub RenderDistributionChart(ByVal items As List(Of KeyValuePair(Of String, Integer)))
        Dim chart As New BarChart()
        chart.Width = 60
        chart.Label = "[bold green]Distribution of Top 5 States[/]"

        Dim colors As Color() = {Color.Red, Color.Green, Color.Blue, Color.Yellow, Color.Aqua}
        Dim colorIndex As Integer = 0
        For Each s As KeyValuePair(Of String, Integer) In items
            chart.AddItem(s.Key, s.Value, colors(colorIndex Mod colors.Length))
            colorIndex += 1
        Next

        AnsiConsole.Write(chart)
    End Sub

    Private Sub RenderSettlementTypesTable(ByVal stats As PostalCodeStatistics)
        Dim typesTable As New Table()
        typesTable.Border = TableBorder.Rounded
        typesTable.Title = New TableTitle("[bold green]Top 10 Settlement Types[/]")
        typesTable.AddColumn("[bold blue]Settlement Type[/]")
        typesTable.AddColumn("[bold blue]Records[/]")
        typesTable.AddColumn("[bold blue]Percentage[/]")

        For Each t As KeyValuePair(Of String, Integer) In stats.TopSettlementTypes
            typesTable.AddRow(t.Key, t.Value.ToString("N0"), $"{Percentage(t.Value, stats.TotalRecords):F2}%")
        Next

        AnsiConsole.Write(typesTable)
    End Sub

    Private Function Percentage(ByVal value As Integer, ByVal total As Integer) As Double
        If total <= 0 Then Return 0.0
        Return (value / CDbl(total)) * 100.0
    End Function

    Private Sub ExportDataset(ByVal service As PostalCodeService)
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

        Dim fullPath As String = String.Empty

        Try
            Select Case format
                Case "JSON (Highly detailed, formatted)"
                    fullPath = service.BuildExportPath("json")
                    AnsiConsole.Status().Start("Serializing dataset to JSON...",
                                               Sub(ctx As StatusContext)
                                                   service.ExportToJson(fullPath)
                                               End Sub)

                Case "CSV (Comma Separated Values)"
                    fullPath = service.BuildExportPath("csv")
                    AnsiConsole.Status().Start("Serializing dataset to CSV...",
                                               Sub(ctx As StatusContext)
                                                   service.ExportToCsv(fullPath)
                                               End Sub)

                Case "XML (Extensible Markup Language)"
                    fullPath = service.BuildExportPath("xml")
                    AnsiConsole.Status().Start("Serializing dataset to XML...",
                                               Sub(ctx As StatusContext)
                                                   service.ExportToXml(fullPath)
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

    Private Sub DrainInputBuffer()
        Try
            While Console.KeyAvailable
                Console.ReadKey(True)
            End While
        Catch
        End Try
    End Sub

    Private Function SelectOption(ByVal title As String, ByVal choices As String()) As String
        Try
            If AnsiConsole.Profile.Capabilities.Interactive Then
                Dim menuPrompt As New SelectionPrompt(Of String)()
                menuPrompt.Title = title
                menuPrompt.PageSize = 10
                menuPrompt.AddChoices(choices)
                Return AnsiConsole.Prompt(menuPrompt)
            End If
        Catch ex As Exception
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

    Private Function ConfirmChoice(ByVal promptText As String) As Boolean
        Try
            If AnsiConsole.Profile.Capabilities.Interactive Then
                Return AnsiConsole.Confirm(promptText)
            End If
        Catch
        End Try

        DrainInputBuffer()
        AnsiConsole.Markup(promptText & " (y/n): ")
        Dim input As String = Console.ReadLine()
        If String.IsNullOrEmpty(input) Then Return False
        input = input.Trim().ToLowerInvariant()
        Return input = "y" OrElse input = "yes"
    End Function

End Module