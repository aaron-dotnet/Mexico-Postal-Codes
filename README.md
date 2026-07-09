# Mexico-Postal-Codes

![.NET](https://img.shields.io/badge/netstandard-2.0-blue)
![License](https://img.shields.io/badge/license-MIT-green)

Librería .NET (netstandard 2.0) para descargar, consultar y exportar el catálogo oficial de códigos postales de México publicado por SEPOMEX. Ideal para validación de direcciones en e-commerce, logística y sistemas de envío dentro del país.

La primera vez descarga el catálogo de SEPOMEX y lo cachea en disco; las siguientes lecturas usan el archivo local sin necesidad de conexión.

## Uso rápido

```vbnet
Imports Mexico_Postal_Code.Core

Dim service As New PostalCodeService()
service.LoadOrDownload()                     ' descarga o usa caché

' Búsqueda sin acentos (encuentra "Mérida" incluso buscando "Merida")
Dim resultados = service.Search("Merida")

' Exportar a archivo
service.ExportToJson("codigos.json")
service.ExportToCsv("codigos.csv")
service.ExportToXml("codigos.xml")
```

## Demo

<img width="1270" height="770" alt="sample" src="https://github.com/user-attachments/assets/e4ec14df-05bf-4909-8bfe-0a4de54db20c" />


## API pública

| Clase | Métodos clave |
|-------|---------------|
| `PostalCodeService` | `LoadOrDownload()`, `LoadOrDownloadAsync(token)`, `Refresh()`, `RefreshAsync(token)`, `Search(query)`, `ExportToJson/Csv/Xml(path)`, `BuildExportPath(ext)`, `HasCachedDatabase()`, `DatabasePath` |
| `PostalCodeEntry` | 15 propiedades: `CodigoPostal`, `Asentamiento`, `TipoAsentamiento`, `Municipio`, `Estado`, `Ciudad`, `d_zona`, etc. |

`PostalCodeService` es stateful: mantiene los datos internamente. `Search()`, `ExportTo*()` lanzan `InvalidOperationException` si no hay datos cargados.

La búsqueda normaliza acentos: "Merida", "Mérida" y "MERIDA" retornan los mismos resultados.

## Constructor

```vbnet
' Valores por defecto (workingDir = AppData/~/.local, logger = NullLogger, timeout = 30s)
Dim service As New PostalCodeService()

' Personalizado
Dim service As New PostalCodeService(
    workingDirectory:="C:\data",
    logger:=New FileLogger("log.txt"),
    httpTimeoutSeconds:=60
)
```

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

Incluye `FileLogger` y `NullLogger`. Configuración global vía `AppContext.Logger`.

## Dependencias

- **Core DLL:** Sin dependencias externas. Apunta a `netstandard2.0` (compatible con .NET Framework 4.6.1+, .NET Core 2.0+ y .NET 5+).
- **CLI demo** (`Mexico-Postal-Code`): `Spectre.Console` 0.55.2 (no incluido en el DLL).

## Licencia

MIT
