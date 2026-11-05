# SvgTemplateProcessor

Высокоуровневая обёртка над библиотеками **Svg.Custom** и **Svg.Skia** для работы с SVG-шаблонами: загрузка, редактирование именованных полей, изменение размеров, сохранение в SVG и экспорт в PDF. Поддерживает .NET Framework 4.7.2+ и .NET 6+.

## Возможности

- Загрузка SVG из **файла**, **потока**, **строки** или готового `SvgDocument`
- Установка текста в поля по их `id` (обычно `<text>`)
- Изменение размеров полей (`<rect>`, `<text>`)
- Автоматическая подгонка размера поля под длину текста
- Сохранение в **SVG-файл** или **массив байт**
- Конвертация в **PDF-файл** или **массив байт**
- **Fluent-синтаксис** через методы расширения
- Полная XML-документация и модульные тесты

## Установка

Добавьте в проект пакеты NuGet:

```xml
<PackageReference Include="Svg.Skia" Version="5.2.3" />
```

> **Для .NET Framework** дополнительно требуются нативные библиотеки `libSkiaSharp.dll` и `libHarfBuzzSharp.dll` в выходной папке приложения. Скопируйте их из папки `packages` (см. раздел «Известные проблемы»).

Если вы работаете с проектом в формате `packages.config`, добавьте в него записи:

```xml
<package id="Svg.Skia" version="5.2.3" targetFramework="net472" />
<package id="SkiaSharp.NativeAssets.Win32" version="4.148.0" targetFramework="net472" />
<package id="HarfBuzzSharp.NativeAssets.Win32" version="14.2.0" targetFramework="net472" />
```

## Быстрый старт

```csharp
using SvgProcessing;

using (var processor = SvgTemplateProcessor
    .FromFile("template.svg")
    .WithFieldText("field_name", "Иван Петров")
    .SaveTo("output.svg")
    .SavePdfTo("output.pdf"))
{
    Console.WriteLine("Готово.");
}
```

## Фабричные методы

Экземпляр `SvgTemplateProcessor` создаётся только через один из четырёх методов — это гарантирует, что внутри всегда находится валидный документ.

### `FromFile(string path)`

Загружает SVG из файла.

```csharp
using (var processor = SvgTemplateProcessor.FromFile(@"C:\Templates\certificate.svg"))
{
    // ...
}
```

### `FromStream(Stream stream)`

Загружает SVG из потока. **Поток не закрывается** — вызывающий код обязан освободить его сам.

```csharp
using (var stream = Assembly.GetExecutingAssembly()
    .GetManifestResourceStream("MyApp.template.svg"))
using (var processor = SvgTemplateProcessor.FromStream(stream))
{
    // ...
}
```

### `FromString(string svgContent)`

Загружает SVG из строки. Удобно для шаблонов, хранящихся в БД или полученных по сети.

```csharp
const string template = @"<svg xmlns=""http://www.w3.org/2000/svg"" ...>...</svg>";

using (var processor = SvgTemplateProcessor.FromString(template))
{
    // ...
}
```

### `FromDocument(SvgDocument document)`

Оборачивает уже готовый `SvgDocument`. Ответственность за владение переходит к процессору.

```csharp
var document = SvgDocument.Open("template.svg");
using (var processor = SvgTemplateProcessor.FromDocument(document))
{
    // ...
}
```

## Редактирование полей

### Установка текста

Находит элемент `<text>` с указанным `id` и заменяет его содержимое:

```csharp
using (var processor = SvgTemplateProcessor.FromFile("template.svg"))
{
    processor.SetFieldText("field_name", "Иван Петров");
    processor.SetFieldText("field_date", "05.11.2026");
    processor.SetFieldText("field_course", "Программирование на C#");
}
```

### Изменение размеров

Работает для `<rect>` (напрямую меняет `width`/`height`) и для `<text>` (устанавливает `textLength`):

```csharp
using (var processor = SvgTemplateProcessor.FromFile("template.svg"))
{
    // Для <rect>
    processor.SetFieldSize("field_rect", width: 300, height: 120);

    // Для <text> — растягивает/сжимает по оси X
    processor.SetFieldSize("field_name", width: 250, height: 30);
}
```

### Автоподгонка под текст

Эвристически рассчитывает размер поля исходя из длины текста:

```csharp
using (var processor = SvgTemplateProcessor.FromFile("template.svg"))
{
    processor.FitFieldToText(
        "field_name",
        "Иван Петров",
        charWidth: 9.5f,   // средняя ширина символа (зависит от шрифта)
        padding: 5f);       // отступы по краям
}
```

> **Совет:** параметр `charWidth` подбирайте под конкретный шрифт. Для шрифта размером 16pt обычно подходит значение 8–10.

## Сохранение

### В файл

```csharp
processor.SaveSvg("output.svg");
processor.ConvertToPdf("output.pdf", SKColors.White);
```

### В память (без записи на диск)

```csharp
byte[] svgBytes = processor.GetByteArray();
byte[] pdfBytes = processor.GetByteArrayAsPdf(SKColors.White);
```

`GetByteArray()` эквивалентен `File.WriteAllBytes(path, GetByteArray())` — оба возвращают идентичные байты.

## Fluent-синтаксис

Все операции доступны как методы расширения, возвращающие тот же экземпляр. Это позволяет выстроить читаемую цепочку:

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
    // работа завершена
}
```

### Пакетное обновление

```csharp
using (SvgTemplateProcessor
    .FromFile("template.svg")
    .WithFields(new Dictionary<string, string>
    {
        { "field_name",   "Иван Петров" },
        { "field_date",   "05.11.2026" },
        { "field_course", "Программирование на C#" },
        { "field_org",    "ООО «Ромашка»" }
    })
    .SaveTo("output.svg"))
{
}
```

## Примеры из реальной жизни

### ASP.NET: отдать SVG без записи на диск

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

### ASP.NET Core: отдать PDF

```csharp
[HttpGet("certificate.pdf")]
public IActionResult GetCertificatePdf()
{
    using (var processor = SvgTemplateProcessor
        .FromFile("Templates/certificate.svg")
        .WithFieldText("field_name", "Иван Петров")
        .WithFittedText("field_name", "Иван Петров"))
    {
        return File(
            processor.GetByteArrayAsPdf(SKColors.White),
            "application/pdf",
            "certificate.pdf");
    }
}
```

### Сохранение в БД

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

### Пакетная генерация сертификатов

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

## Структура SVG-шаблона

Шаблон — это обычный SVG-файл, в котором поля помечены атрибутом `id`. Пример:

```xml
<?xml version="1.0" encoding="utf-8"?>
<svg xmlns="http://www.w3.org/2000/svg" width="800" height="600">
  <!-- Рамка -->
  <rect x="0" y="0" width="800" height="600" fill="white" stroke="black"/>

  <!-- Поле для ФИО -->
  <text id="field_name" x="100" y="200" font-family="Arial" font-size="24">
    Placeholder
  </text>

  <!-- Поле для даты -->
  <text id="field_date" x="100" y="250" font-family="Arial" font-size="18">
    01.01.2000
  </text>

  <!-- Прямоугольник, размер которого можно менять -->
  <rect id="field_rect" x="100" y="300" width="200" height="60" fill="lightgray"/>
</svg>
```

### Требования к шаблону

| Элемент | Требование |
| :--- | :--- |
| `<text>` | Должен иметь уникальный `id`, текст-заглушка может быть любым |
| `<tspan>` | Если текст вложен в `<tspan>`, присвойте `id` самому `<tspan>` |
| `<rect>` | Для изменения размеров через `SetFieldSize` требуется `id` |
| Прочие элементы | `SetFieldSize` для них бросает `NotSupportedException` |

## Известные проблемы

### `DllNotFoundException: libHarfBuzzSharp`

В .NET Framework нативные библиотеки `libSkiaSharp.dll` и `libHarfBuzzSharp.dll` не всегда копируются в `bin` автоматически. Решение — скопировать их вручную из папки `packages`:

```
packages\SkiaSharp.NativeAssets.Win32.4.148.0\runtimes\win-x64\native\libSkiaSharp.dll
packages\HarfBuzzSharp.NativeAssets.Win32.14.2.0\runtimes\win-x64\native\libHarfBuzzSharp.dll
```

Скопируйте оба файла в `bin\Debug` (или `bin\Release`) вашего проекта. Для автоматизации добавьте в `.csproj`:

```xml
<Target Name="CopyNativeLibs" AfterTargets="Build">
  <ItemGroup>
    <NativeLibs Include="..\packages\SkiaSharp.NativeAssets.Win32.4.148.0\runtimes\win-x64\native\*.dll" />
    <NativeLibs Include="..\packages\HarfBuzzSharp.NativeAssets.Win32.14.2.0\runtimes\win-x64\native\*.dll" />
  </ItemGroup>
  <Copy SourceFiles="@(NativeLibs)" DestinationFolder="$(OutputPath)" SkipUnchangedFiles="true" />
</Target>
```

### Конфликт `SvgDocument` между пакетами `Svg` и `Svg.Custom`

**Не подключайте** пакет `Svg` одновременно с `Svg.Skia` — возникнет ошибка `CS0433`. `Svg.Skia` уже содержит совместимую сборку `Svg.Custom` с тем же пространством имён `Svg`.

### `SvgDocument.Save` отсутствует

В сборке `Svg.Custom` нет метода `Save`. Используйте `SaveSvg` / `GetByteArray` из `SvgTemplateProcessor` — они сериализуют документ через `XmlWriter`.

## Тестирование

Класс покрыт модульными тестами на MSTest v2. Пример:

```csharp
[TestMethod()]
public void FluentChain_ProducesBothOutputs()
{
    string outSvg = Path.Combine(_testDir, "chain.svg");
    string outPdf = Path.Combine(_testDir, "chain.pdf");

    using (SvgTemplateProcessor
        .FromFile(_templatePath)
        .WithFieldText("field_name", "Иван Петров")
        .SaveTo(outSvg)
        .SavePdfTo(outPdf, SKColors.White))
    {
    }

    Assert.IsTrue(File.Exists(outSvg));
    Assert.IsTrue(File.Exists(outPdf));
}
```

## API Reference

### Фабричные методы

| Метод | Описание |
| :--- | :--- |
| `FromFile(string path)` | Загрузка из файла |
| `FromStream(Stream stream)` | Загрузка из потока (не закрывает поток) |
| `FromString(string svgContent)` | Загрузка из строки |
| `FromDocument(SvgDocument document)` | Обёртка готового документа |

### Операции

| Метод | Описание |
| :--- | :--- |
| `SetFieldText(string fieldId, string text)` | Установить текст поля `<text>` |
| `SetFieldSize(string fieldId, float width, float height)` | Изменить размер `<rect>` или `<text>` |
| `FitFieldToText(string fieldId, string text, float charWidth, float padding)` | Автоподгонка размера |
| `SaveSvg(string outputPath)` | Сохранить SVG-файл |
| `ConvertToPdf(string outputPath, SKColor? background)` | Сохранить PDF-файл |
| `GetByteArray()` | Вернуть SVG как `byte[]` |
| `GetByteArrayAsPdf(SKColor? background)` | Вернуть PDF как `byte[]` |

### Fluent-расширения

| Метод | Аналог |
| :--- | :--- |
| `WithFieldText(id, text)` | `SetFieldText` |
| `WithFieldSize(id, w, h)` | `SetFieldSize` |
| `WithFittedText(id, text, ...)` | `FitFieldToText` |
| `WithFields(IDictionary<string, string>)` | Пакетный `SetFieldText` |
| `SaveTo(path)` | `SaveSvg` |
| `SavePdfTo(path, background)` | `ConvertToPdf` |

### Свойства

| Свойство | Описание |
| :--- | :--- |
| `Document` | Доступ к нижележащему `SvgDocument` |