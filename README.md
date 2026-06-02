# Mexico-Postal-Codes

![.NET](https://img.shields.io/badge/netstandard-2.0-blue)
![License](https://img.shields.io/badge/license-MIT-green)

Librería .NET (netstandard 2.0) para descargar, consultar y exportar el catálogo oficial de códigos postales de México publicado por SEPOMEX. Ideal para validación de direcciones en e-commerce, logística y sistemas de envío dentro del país.

No hay API pública. Esta librería hace scraping del sitio de descarga, parsea el ZIP resultante y expone los datos como objetos `c_PostalCode`. La primera vez descarga el catálogo de SEPOMEX y lo cachea en disco; las siguientes lecturas usan el archivo local sin necesidad de conexión.

## Uso rápido

```vb.net
Imports Mexico_Postal_Code.Core

AppContext.Logger = New FileLogger("logs.txt")
Dim service As New PostalCodeService()
Dim codigos As List(Of c_PostalCode) = service.LoadOrDownload()

' Buscar por colonia, municipio o estado
Dim resultados = PostalCodeQuery.Search(codigos, "Mérida")
' Buscar por código postal exacto
Dim porCp = PostalCodeQuery.Search(codigos, "97100")

' Exportar
Dim exporter As New PostalCodeExporter()
exporter.ExportToJson(codigos, "codigos.json")
```

## API pública

| Clase | Métodos clave |
|-------|---------------|
| `PostalCodeService` | `LoadOrDownload()`, `Refresh()`, `HasCachedDatabase()`, `DatabasePath` |
| `PostalCodeQuery` | `Search(postalCodes, query)` — búsqueda por CP, colonia, municipio o estado |
| `PostalCodeStatistics` | `Compute(postalCodes)` — totales, únicos, top estados y tipos |
| `PostalCodeExporter` | `ExportToJson()`, `ExportToCsv()`, `ExportToXml()` |
| `c_PostalCode` | 15 propiedades: `CodigoPostal`, `Asentamiento`, `TipoAsentamiento`, `Municipio`, `Estado`, `Ciudad`, y más |

## Modelo de datos

Campos principales: `CodigoPostal` (5 dígitos), `Asentamiento` (colonia), `TipoAsentamiento` (Fraccionamiento, Colonia, etc.), `Municipio`, `Estado`, `Ciudad`, `d_zona` (Urbana/Rural).

El resto de campos (`c_Estado`, `c_Oficina`, `id_Asentamiento_cpcons`, etc.) son de referencia cruzada del archivo original de SEPOMEX.

## Logging

Cualquier clase que implemente `ILogger`.

```vbnet
Public Interface ILogger
    Sub Log(ByVal message As String, ByVal level As LogLevel)
End Interface
```

## Dependencias

- **Core DLL:** Sin dependencias externas. Apunta a `netstandard2.0` (compatible con .NET Framework 4.6.1+, .NET Core 2.0+ y .NET 5+).
- **CLI demo** (`Mexico-Postal-Code`): `Spectre.Console` 0.55.2 (no incluido en el DLL).

## Licencia

MIT
