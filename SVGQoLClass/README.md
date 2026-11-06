# SVGQoLClass

Класс-обёртка над библиотеками Svg.Custom и Svg.Skia для работы с SVG-шаблонами в .NET Framework 4.7.2+ и .NET 6+.

## Назначение

Проект решает следующие задачи:

- Загрузка SVG-документа из файла, потока, строки или готового `SvgDocument`.
- Замена текста в именованных полях (элементы `<text>` с заданным `id`).
- Изменение ширины и высоты полей (`<rect>`, `<text>`).
- Автоматическая подгонка размеров поля под длину текста.
- Сохранение SVG-документа в файл или массив байт.
- Конвертация SVG-документа в PDF-файл или массив байт.
- Построение цепочек операций через методы расширения.

## Зависимости

```xml
<PackageReference Include="Svg.Skia" Version="5.2.3" />
```

Для .NET Framework дополнительно требуются нативные библиотеки `libSkiaSharp.dll` и `libHarfBuzzSharp.dll` в выходной папке приложения.

Для проекта с `packages.config`:

```xml
<package id="Svg.Skia" version="5.2.3" targetFramework="net472" />
<package id="SkiaSharp.NativeAssets.Win32" version="4.148.0" targetFramework="net472" />
<package id="HarfBuzzSharp.NativeAssets.Win32" version="14.2.0" targetFramework="net472" />
```

## API

### Фабричные методы

| Метод | Возвращает | Описание |
| :--- | :--- | :--- |
| `FromFile(string path)` | `SvgTemplateProcessor` | Загрузка из файла |
| `FromStream(Stream stream)` | `SvgTemplateProcessor` | Загрузка из потока. Поток не закрывается |
| `FromString(string svgContent)` | `SvgTemplateProcessor` | Загрузка из строки |
| `FromDocument(SvgDocument document)` | `SvgTemplateProcessor` | Обёртка готового документа |

### Операции редактирования

| Метод | Описание |
| :--- | :--- |
| `SetFieldText(string fieldId, string text)` | Устанавливает текст элемента `<text>` по `id` |
| `SetFieldSize(string fieldId, float width, float height)` | Устанавливает размеры `<rect>` (width/height) или `<text>` (textLength) |
| `FitFieldToText(string fieldId, string text, float charWidth = 10f, float padding = 4f)` | Вычисляет и устанавливает размеры поля по длине текста |

### Сохранение на диск

| Метод | Описание |
| :--- | :--- |
| `SaveSvg(string outputPath)` | Записывает SVG-документ в файл |
| `ConvertToPdf(string outputPath, SKColor? backgroundColor = null)` | Записывает PDF-документ в файл |

### Сохранение в память

| Метод | Возвращает | Описание |
| :--- | :--- | :--- |
| `GetByteArray()` | `byte[]` | SVG-документ в кодировке UTF-8 без BOM |
| `GetByteArrayAsPdf(SKColor? backgroundColor = null)` | `byte[]` | PDF-документ |

### Свойства

| Свойство | Тип | Описание |
| :--- | :--- | :--- |
| `Document` | `SvgDocument` | Нижележащий SVG-документ |

### Методы расширения: редактирование

| Метод | Аналог |
| :--- | :--- |
| `WithFieldText(string id, string text)` | `SetFieldText` |
| `WithFieldSize(string id, float width, float height)` | `SetFieldSize` |
| `WithFittedText(string id, string text, float charWidth, float padding)` | `FitFieldToText` |
| `WithFields(IDictionary<string, string> fields)` | Пакетный вызов `SetFieldText` |

### Методы расширения: сохранение

| Метод | Аналог |
| :--- | :--- |
| `SaveTo(string path)` | `SaveSvg` |
| `SavePdfTo(string path, SKColor? background)` | `ConvertToPdf` |
| `ToByteArray()` | `GetByteArray` |
| `ToPdfByteArray(SKColor? background)` | `GetByteArrayAsPdf` |
| `GetByteArray()` | Обёртка над `GetByteArray`. Перекрывается экземплярным методом |
| `GetByteArrayAsPdf(SKColor? background)` | Обёртка над `GetByteArrayAsPdf`. Перекрывается экземплярным методом |

## Исключения

| Исключение | Условие |
| :--- | :--- |
| `ArgumentException` | Некорректный аргумент: пустая строка, отсутствующий `id`, неверный тип элемента |
| `ArgumentNullException` | Аргумент равен `null` |
| `FileNotFoundException` | Файл по указанному пути не существует |
| `NotSupportedException` | `SetFieldSize` вызван для неподдерживаемого типа элемента |
| `InvalidOperationException` | Не удалось разобрать или отрендерить SVG |
| `ObjectDisposedException` | Операция вызвана после `Dispose` |
| `IOException` | Ошибка записи файла |

## Примеры использования

### Загрузка и сохранение в файл

```csharp
using SvgProcessing;

using (var processor = SvgTemplateProcessor.FromFile("template.svg"))
{
    processor.SetFieldText("field_name", "Иван Петров");
    processor.SetFieldText("field_date", "05.11.2026");
    processor.SaveSvg("output.svg");
}
```

### Загрузка из строки

```csharp
const string template = @"<svg xmlns=""http://www.w3.org/2000/svg"">...</svg>";

using (var processor = SvgTemplateProcessor.FromString(template))
{
    processor.SetFieldText("field_name", "Иван Петров");
    processor.SaveSvg("output.svg");
}
```

### Загрузка из потока

```csharp
using (var stream = File.OpenRead("template.svg"))
using (var processor = SvgTemplateProcessor.FromStream(stream))
{
    processor.SetFieldText("field_name", "Иван Петров");
    processor.SaveSvg("output.svg");
}
```

### Изменение размеров поля

```csharp
using (var processor = SvgTemplateProcessor.FromFile("template.svg"))
{
    processor.SetFieldSize("field_rect", width: 300, height: 120);
    processor.SetFieldSize("field_name", width: 250, height: 30);
    processor.SaveSvg("output.svg");
}
```

### Автоподгонка размеров под текст

```csharp
using (var processor = SvgTemplateProcessor.FromFile("template.svg"))
{
    processor.FitFieldToText("field_rect", "0123456789", charWidth: 10f, padding: 4f);
    processor.SaveSvg("output.svg");
}
```

### Конвертация в PDF

```csharp
using (var processor = SvgTemplateProcessor.FromFile("template.svg"))
{
    processor.SetFieldText("field_name", "Иван Петров");
    processor.ConvertToPdf("output.pdf", SKColors.White);
}
```

### Сохранение в память

```csharp
byte[] svgBytes, pdfBytes;

using (var processor = SvgTemplateProcessor.FromFile("template.svg"))
{
    processor.SetFieldText("field_name", "Иван Петров");
    svgBytes = processor.GetByteArray();
    pdfBytes = processor.GetByteArrayAsPdf(SKColors.White);
}
```

### Fluent-цепочка с сохранением на диск

```csharp
using (SvgTemplateProcessor
    .FromFile("template.svg")
    .WithFieldText("field_name", "Иван Петров")
    .WithFieldText("field_date", "05.11.2026")
    .WithFieldSize("field_rect", 300, 120)
    .WithFittedText("field_course", "Программирование на C#")
    .SaveTo("output.svg")
    .SavePdfTo("output.pdf", SKColors.White))
{
}
```

### Fluent-цепочка с возвратом байтов

```csharp
byte[] pdfBytes = SvgTemplateProcessor
    .FromFile("template.svg")
    .WithFieldText("field_name", "Иван Петров")
    .WithFieldText("field_date", "05.11.2026")
    .ToPdfByteArray(SKColors.White);

File.WriteAllBytes("output.pdf", pdfBytes);
```

### Пакетное обновление полей

```csharp
using (SvgTemplateProcessor
    .FromFile("template.svg")
    .WithFields(new Dictionary<string, string>
    {
        { "field_name",   "Иван Петров" },
        { "field_date",   "05.11.2026" },
        { "field_course", "Программирование на C#" }
    })
    .SaveTo("output.svg"))
{
}
```

### Отдача SVG из ASP.NET

```csharp
public ActionResult DownloadCertificate()
{
    using (var processor = SvgTemplateProcessor
        .FromFile(Server.MapPath("~/Templates/certificate.svg"))
        .WithFieldText("field_name", "Иван Петров")
        .WithFieldText("field_date", DateTime.Today.ToString("dd.MM.yyyy")))
    {
        return File(
            processor.GetByteArray(),
            "image/svg+xml",
            "certificate.svg");
    }
}
```

### Отдача PDF из ASP.NET Core

```csharp
[HttpGet("certificate/{id}")]
public IActionResult GetCertificate(int id)
{
    byte[] pdf = SvgTemplateProcessor
        .FromFile("Templates/certificate.svg")
        .WithFieldText("field_name", "Иван Петров")
        .WithFieldText("field_date", DateTime.Today.ToString("dd.MM.yyyy"))
        .ToPdfByteArray(SKColors.White);

    return File(pdf, "application/pdf", $"certificate_{id}.pdf");
}
```

### Сохранение в базу данных

```csharp
using (var processor = SvgTemplateProcessor
    .FromString(svgTemplate)
    .WithFieldText("field_name", "Иван Петров"))
{
    byte[] svgBytes = processor.GetByteArray();
    byte[] pdfBytes = processor.GetByteArrayAsPdf(SKColors.White);

    using (var cmd = new SqlCommand(
        "INSERT INTO Documents (Svg, Pdf) VALUES (@svg, @pdf)", connection))
    {
        cmd.Parameters.Add("@svg", SqlDbType.VarBinary, -1).Value = svgBytes;
        cmd.Parameters.Add("@pdf", SqlDbType.VarBinary, -1).Value = pdfBytes;
        cmd.ExecuteNonQuery();
    }
}
```

### Пакетная генерация

```csharp
var students = new[]
{
    new { Name = "Иван Петров",    Course = "C#" },
    new { Name = "Мария Сидорова", Course = "Python" },
    new { Name = "Пётр Иванов",    Course = "Java" }
};

foreach (var student in students)
{
    string safeName = string.Join("_", student.Name.Split(Path.GetInvalidFileNameChars()));

    using (SvgTemplateProcessor
        .FromFile("template.svg")
        .WithFieldText("field_name", student.Name)
        .WithFieldText("field_course", student.Course)
        .WithFieldText("field_date", DateTime.Today.ToString("dd.MM.yyyy"))
        .SaveTo($"{safeName}.svg")
        .SavePdfTo($"{safeName}.pdf", SKColors.White))
    {
    }
}
```

## Формат SVG-шаблона

Поля помечаются атрибутом `id`. Поддерживаются элементы:

| Элемент | Требование |
| :--- | :--- |
| `<text>` | Уникальный `id`. Текст-заглушка может быть любым |
| `<tspan>` | Уникальный `id`. Используется, если текст вложен в `<tspan>` |
| `<rect>` | Уникальный `id`. Для изменения размеров через `SetFieldSize` |

Пример шаблона:

```xml
<?xml version="1.0" encoding="utf-8"?>
<svg xmlns="http://www.w3.org/2000/svg" width="800" height="600">
  <rect x="0" y="0" width="800" height="600" fill="white" stroke="black"/>
  <text id="field_name" x="100" y="200" font-family="Arial" font-size="24">
    Placeholder
  </text>
  <text id="field_date" x="100" y="250" font-family="Arial" font-size="18">
    01.01.2000
  </text>
  <rect id="field_rect" x="100" y="300" width="200" height="60" fill="lightgray"/>
</svg>
```

## Ограничения и известные проблемы

### `DllNotFoundException: libHarfBuzzSharp`

В .NET Framework нативные библиотеки не копируются в выходную папку автоматически. Требуется скопировать вручную:

```
packages\SkiaSharp.NativeAssets.Win32.4.148.0\runtimes\win-x64\native\libSkiaSharp.dll
packages\HarfBuzzSharp.NativeAssets.Win32.14.2.0\runtimes\win-x64\native\libHarfBuzzSharp.dll
```

Файлы копируются в `bin\Debug` или `bin\Release`. Автоматизация в `.csproj`:

```xml
<Target Name="CopyNativeLibs" AfterTargets="Build">
  <ItemGroup>
    <NativeLibs Include="..\packages\SkiaSharp.NativeAssets.Win32.4.148.0\runtimes\win-x64\native\*.dll" />
    <NativeLibs Include="..\packages\HarfBuzzSharp.NativeAssets.Win32.14.2.0\runtimes\win-x64\native\*.dll" />
  </ItemGroup>
  <Copy SourceFiles="@(NativeLibs)" DestinationFolder="$(OutputPath)" SkipUnchangedFiles="true" />
</Target>
```

### Конфликт типа `SvgDocument`

Пакет `Svg` нельзя подключать одновременно с `Svg.Skia`. Приводит к ошибке компиляции `CS0433`. `Svg.Skia` содержит совместимую сборку `Svg.Custom` с тем же пространством имён.

### Отсутствие `SvgDocument.Save`

В сборке `Svg.Custom` метод `Save` отсутствует. Сериализация выполняется через `XmlWriter` внутри `SaveSvg` / `GetByteArray` / `ToByteArray`.

### Неидентичность PDF-файлов

Два вызова `GetByteArrayAsPdf()` на одном документе возвращают разные байты из-за встроенных метаданных PDF (дата создания, идентификатор). Сравнение PDF выполняется по сигнатуре `%PDF-` и размеру.

### `charWidth` в `FitFieldToText`

Параметр `charWidth` зависит от шрифта и его размера. Для шрифта 16pt типичное значение — 8–10. Точный расчёт требует измерения текста через `SKFont.MeasureText`.

## Тестирование

Модульные тесты реализованы на MSTest v2. Пример:

```csharp
[TestMethod()]
public void ToByteArray_InChain_ReturnsValidSvgBytes()
{
    byte[] bytes = SvgTemplateProcessor
        .FromFile(_templatePath)
        .WithFieldText("field_name", "Иван Петров")
        .ToByteArray();

    Assert.IsNotNull(bytes);
    Assert.IsTrue(bytes.Length > 0);

    string content = Encoding.UTF8.GetString(bytes);
    StringAssert.Contains(content, "Иван Петров");
}
```
