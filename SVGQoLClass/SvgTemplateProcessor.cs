using SkiaSharp;
using Svg;
using Svg.Skia;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace SVGQoLClass
{
    /// <summary>
    /// Предоставляет высокоуровневый API для работы с SVG-шаблонами:
    /// загрузку из файла, потока, строки или готового <see cref="SvgDocument"/>,
    /// редактирование текстовых полей и их геометрии, сохранение в SVG
    /// и экспорт в PDF.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Экземпляр класса создаётся исключительно через фабричные методы
    /// (<see cref="FromFile(string)"/>, <see cref="FromStream(Stream)"/>,
    /// <see cref="FromString(string)"/>, <see cref="FromDocument(SvgDocument)"/>),
    /// что гарантирует наличие корректно загруженного документа на момент
    /// использования. Конструктор объявлен <c>private</c>.
    /// </para>
    /// <para>
    /// Класс реализует <see cref="IDisposable"/>: после вызова <see cref="Dispose"/>
    /// любые операции с экземпляром приводят к <see cref="ObjectDisposedException"/>.
    /// Используйте <c>using</c> для автоматического освобождения ресурсов.
    /// </para>
    /// <para>
    /// Внутренняя реализация опирается на сборку <c>Svg.Custom</c>
    /// (поставляется вместе с <c>Svg.Skia</c>) для работы с DOM и на
    /// <see cref="SKSvg"/> для рендеринга в PDF.
    /// </para>
    /// <para>
    /// Помимо сохранения на диск (<see cref="SaveSvg(string)"/>,
    /// <see cref="ConvertToPdf(string, SKColor?)"/>), класс умеет возвращать
    /// результат в память — <see cref="GetByteArray"/> для SVG и
    /// <see cref="GetByteArrayAsPdf"/> для PDF. Это удобно для передачи
    /// данных по сети или сохранения в БД без промежуточных файлов.
    /// </para>
    /// </remarks>
    /// <example>
    /// Пример fluent-использования через методы расширения
    /// (<see cref="SvgTemplateProcessorExtensions"/>):
    /// <code>
    /// using (SvgTemplateProcessor
    ///     .FromFile("template.svg")
    ///     .WithFieldText("field_name", "Иван Петров")
    ///     .WithFittedText("field_rect", "Иван Петров")
    ///     .SaveTo("output.svg")
    ///     .SavePdfTo("output.pdf", SKColors.White))
    /// {
    ///     // работа завершена
    /// }
    /// </code>
    /// </example>
    public sealed class SvgTemplateProcessor : IDisposable
    {
        /// <summary>
        /// Нижележащий SVG-документ. Устанавливается в <c>null</c> после
        /// <see cref="Dispose"/> как маркер освобождённого состояния.
        /// </summary>
        private SvgDocument _document;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="SvgTemplateProcessor"/>
        /// с уже загруженным SVG-документом.
        /// </summary>
        /// <param name="document">Корректно разобранный SVG-документ.</param>
        private SvgTemplateProcessor(SvgDocument document)
        {
            _document = document;
        }

        // ==================== ФАБРИЧНЫЕ МЕТОДЫ ====================

        /// <summary>
        /// Создаёт экземпляр <see cref="SvgTemplateProcessor"/>, загружая SVG-документ
        /// из файла по указанному пути.
        /// </summary>
        /// <param name="path">Путь к существующему SVG-файлу.</param>
        /// <returns>Новый экземпляр <see cref="SvgTemplateProcessor"/> с загруженным документом.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> равен <c>null</c>, пустой строке или строке из пробелов.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// Файл по пути <paramref name="path"/> не существует.
        /// </exception>
        /// <remarks>
        /// Метод использует <see cref="SvgDocument.Open(string)"/> из сборки
        /// <c>Svg.Custom</c>. Если файл повреждён или не является корректным SVG,
        /// исключение будет сгенерировано на этапе парсинга внутри
        /// <see cref="SvgDocument.Open(string)"/>.
        /// </remarks>
        public static SvgTemplateProcessor FromFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Путь к файлу не задан.", nameof(path));
            if (!File.Exists(path))
                throw new FileNotFoundException("SVG-файл не найден: " + path, path);

            return new SvgTemplateProcessor(SvgDocument.Open(path));
        }

        /// <summary>
        /// Создаёт экземпляр <see cref="SvgTemplateProcessor"/>, читая SVG-содержимое
        /// из потока.
        /// </summary>
        /// <param name="stream">Поток с SVG-содержимым в кодировке UTF-8.</param>
        /// <returns>Новый экземпляр <see cref="SvgTemplateProcessor"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="stream"/> равен <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Не удалось разобрать содержимое потока как SVG-документ.
        /// </exception>
        /// <remarks>
        /// <para>
        /// Метод читает поток полностью в строку (с автоопределением BOM)
        /// и передаёт её в <see cref="SvgDocument.FromSvg{T}(string)"/>.
        /// Поскольку вендорная сборка <c>Svg.Custom</c> не предоставляет
        /// перегрузку <c>Open(Stream)</c>, парсинг выполняется через строку.
        /// </para>
        /// <para>
        /// Исходный поток <b>не закрывается</b> после чтения
        /// (<c>leaveOpen: true</c>); вызывающий код обязан освободить его
        /// самостоятельно.
        /// </para>
        /// </remarks>
        public static SvgTemplateProcessor FromStream(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            using (var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, leaveOpen: true))
            {
                string svgContent = reader.ReadToEnd();

                var document = SvgDocument.FromSvg<SvgDocument>(svgContent);

                if (document == null)
                    throw new InvalidOperationException("Не удалось разобрать SVG-содержимое из потока.");

                return new SvgTemplateProcessor(document);
            }
        }

        /// <summary>
        /// Создаёт экземпляр <see cref="SvgTemplateProcessor"/> из строки,
        /// содержащей SVG-разметку.
        /// </summary>
        /// <param name="svgContent">Строка с XML-содержимым SVG-документа.</param>
        /// <returns>Новый экземпляр <see cref="SvgTemplateProcessor"/>.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="svgContent"/> равен <c>null</c>, пустой строке или строке из пробелов.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Не удалось разобрать переданную строку как SVG-документ.
        /// </exception>
        /// <remarks>
        /// Внутри создаётся <see cref="MemoryStream"/> с UTF-8-представлением строки,
        /// который затем передаётся в <see cref="FromStream(Stream)"/>.
        /// Это удобный способ загрузки шаблона из ресурсов, БД или сетевого ответа.
        /// </remarks>
        public static SvgTemplateProcessor FromString(string svgContent)
        {
            if (string.IsNullOrWhiteSpace(svgContent))
                throw new ArgumentException("SVG-содержимое не задано.", nameof(svgContent));

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(svgContent)))
            {
                return FromStream(ms);
            }
        }

        /// <summary>
        /// Создаёт экземпляр <see cref="SvgTemplateProcessor"/> поверх
        /// уже готового <see cref="SvgDocument"/>.
        /// </summary>
        /// <param name="document">Экземпляр SVG-документа.</param>
        /// <returns>Новый экземпляр <see cref="SvgTemplateProcessor"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="document"/> равен <c>null</c>.
        /// </exception>
        /// <remarks>
        /// Ответственность за владение документом переходит к процессору:
        /// после <see cref="Dispose"/> ссылка внутри процессора обнуляется.
        /// Используйте этот метод, если документ уже загружен другим способом
        /// (например, через <c>SKSvg.SourceDocument</c>).
        /// </remarks>
        public static SvgTemplateProcessor FromDocument(SvgDocument document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            return new SvgTemplateProcessor(document);
        }

        // ==================== СВОЙСТВА ====================

        /// <summary>
        /// Возвращает нижележащий <see cref="SvgDocument"/> для нестандартных
        /// сценариев, не покрытых публичным API.
        /// </summary>
        /// <value>Корректный SVG-документ, загруженный при создании экземпляра.</value>
        /// <exception cref="ObjectDisposedException">
        /// Экземпляр уже освобождён вызовом <see cref="Dispose"/>.
        /// </exception>
        /// <remarks>
        /// Возвращаемая ссылка позволяет напрямую манипулировать DOM
        /// (<see cref="SvgElement"/> и его наследники). Учтите, что такие изменения
        /// не проходят через валидацию, выполняемую методами
        /// <see cref="SetFieldText"/> и <see cref="SetFieldSize"/>.
        /// </remarks>
        public SvgDocument Document
        {
            get
            {
                EnsureNotDisposed();
                return _document;
            }
        }

        // ==================== ОПЕРАЦИИ ====================

        /// <summary>
        /// Устанавливает новый текст для поля с указанным идентификатором.
        /// </summary>
        /// <param name="fieldId">
        /// Значение атрибута <c>id</c> целевого элемента в SVG-документе.
        /// </param>
        /// <param name="text">
        /// Новое текстовое содержимое. Может быть пустой строкой или <c>null</c>
        /// (в этом случае текст поля очищается).
        /// </param>
        /// <exception cref="ObjectDisposedException">
        /// Экземпляр уже освобождён.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Элемент с указанным <paramref name="fieldId"/> не найден,
        /// либо найденный элемент не является <see cref="SvgText"/>
        /// (например, это <see cref="SvgRectangle"/>).
        /// </exception>
        /// <remarks>
        /// Метод работает только с элементами <see cref="SvgText"/>.
        /// Если текст в шаблоне вложен в <c>&lt;tspan&gt;</c>, идентификатор
        /// следует присвоить самому элементу <c>&lt;tspan&gt;</c> и работать
        /// с ним как с самостоятельным полем.
        /// </remarks>
        public void SetFieldText(string fieldId, string text)
        {
            EnsureNotDisposed();
            var textElement = _document.GetElementById<SvgText>(fieldId);
            if (textElement == null)
                throw new ArgumentException(
                    "Текстовое поле с id '" + fieldId + "' не найдено или не является SvgText.",
                    nameof(fieldId));
            textElement.Text = text;
        }

        /// <summary>
        /// Изменяет ширину и высоту поля с указанным идентификатором.
        /// </summary>
        /// <param name="fieldId">
        /// Значение атрибута <c>id</c> целевого элемента.
        /// </param>
        /// <param name="width">Новая ширина в единицах пользователя SVG (px).</param>
        /// <param name="height">Новая высота в единицах пользователя SVG (px).</param>
        /// <exception cref="ObjectDisposedException">
        /// Экземпляр уже освобождён.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Элемент с указанным <paramref name="fieldId"/> не найден.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Тип найденного элемента не поддерживается методом
        /// (поддерживаются только <see cref="SvgRectangle"/> и <see cref="SvgText"/>).
        /// </exception>
        /// <remarks>
        /// <para>
        /// Для <see cref="SvgRectangle"/> напрямую меняются атрибуты
        /// <c>width</c> и <c>height</c>.
        /// </para>
        /// <para>
        /// Для <see cref="SvgText"/> параметр <paramref name="height"/>
        /// игнорируется; параметр <paramref name="width"/> записывается
        /// в атрибут <c>textLength</c>, который растягивает или сжимает текст
        /// по оси X. Если <paramref name="width"/> ≤ 0, атрибут не изменяется.
        /// </para>
        /// </remarks>
        public void SetFieldSize(string fieldId, float width, float height)
        {
            EnsureNotDisposed();
            var element = _document.GetElementById(fieldId);
            if (element == null)
                throw new ArgumentException("Элемент с id '" + fieldId + "' не найден.", nameof(fieldId));

            if (element is SvgRectangle rect)
            {
                rect.Width = new SvgUnit(SvgUnitType.User, width);
                rect.Height = new SvgUnit(SvgUnitType.User, height);
                return;
            }
            if (element is SvgText text)
            {
                if (width > 0)
                    text.TextLength = new SvgUnit(SvgUnitType.User, width);
                return;
            }
            throw new NotSupportedException(
                "Изменение размеров не реализовано для типа '" + element.GetType().Name + "'.");
        }

        /// <summary>
        /// Подбирает размеры поля эвристически, исходя из длины текста.
        /// </summary>
        /// <param name="fieldId">Значение атрибута <c>id</c> целевого элемента.</param>
        /// <param name="text">Текст, под который подгоняется размер.</param>
        /// <param name="charWidth">
        /// Средняя ширина одного символа в единицах SVG. Значение по умолчанию — 10.
        /// Зависит от используемого шрифта и его размера.
        /// </param>
        /// <param name="padding">
        /// Отступы по краям поля в единицах SVG. Значение по умолчанию — 4.
        /// </param>
        /// <exception cref="ObjectDisposedException">
        /// Экземпляр уже освобождён.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Элемент с указанным <paramref name="fieldId"/> не найден.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Тип элемента не поддерживается <see cref="SetFieldSize"/>.
        /// </exception>
        /// <remarks>
        /// <para>
        /// Если <paramref name="text"/> равен <c>null</c> или пустой строке,
        /// метод ничего не делает — размеры поля остаются прежними.
        /// </para>
        /// <para>
        /// Расчёт выполняется по формулам:
        /// <c>width = text.Length * charWidth + 2 * padding</c>;
        /// <c>height = charWidth * 1.5 + 2 * padding</c>.
        /// Это упрощённая модель; для точного расчёта рекомендуется
        /// измерять текст через <c>SKFont.MeasureText</c>.
        /// </para>
        /// </remarks>
        public void FitFieldToText(string fieldId, string text,
            float charWidth = 10f, float padding = 4f)
        {
            if (string.IsNullOrEmpty(text)) return;
            float estimatedWidth = text.Length * charWidth + padding * 2;
            float estimatedHeight = charWidth * 1.5f + padding * 2;
            SetFieldSize(fieldId, estimatedWidth, estimatedHeight);
        }

        /// <summary>
        /// Сохраняет текущее состояние SVG-документа в файл.
        /// </summary>
        /// <param name="outputPath">Путь к результирующему SVG-файлу.</param>
        /// <exception cref="ObjectDisposedException">
        /// Экземпляр уже освобождён.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="outputPath"/> равен <c>null</c>, пустой строке
        /// или строке из пробелов.
        /// </exception>
        /// <exception cref="IOException">
        /// Не удалось создать или записать файл.
        /// </exception>
        /// <remarks>
        /// Эквивалентен <c>File.WriteAllBytes(outputPath, GetByteArray())</c>.
        /// </remarks>
        public void SaveSvg(string outputPath)
        {
            EnsureNotDisposed();
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("Путь для сохранения не задан.", nameof(outputPath));

            File.WriteAllBytes(outputPath, GetByteArray());
        }

        /// <summary>
        /// Конвертирует текущее состояние SVG-документа в PDF-файл.
        /// </summary>
        /// <param name="outputPath">Путь к результирующему PDF-файлу.</param>
        /// <param name="backgroundColor">
        /// Цвет фона страницы PDF. Если <c>null</c>, используется прозрачный фон
        /// (<see cref="SKColors.Transparent"/>).
        /// </param>
        /// <exception cref="ObjectDisposedException">
        /// Экземпляр уже освобождён.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="outputPath"/> равен <c>null</c>, пустой строке
        /// или строке из пробелов.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// <see cref="SKSvg"/> не смог загрузить сериализованный SVG
        /// (например, из-за ошибок в документе или отсутствия нативных
        /// библиотек SkiaSharp/HarfBuzzSharp).
        /// </exception>
        /// <remarks>
        /// <para>
        /// Метод сначала сохраняет текущий документ во временный SVG-файл,
        /// затем загружает его через <see cref="SKSvg"/> и рендерит в PDF
        /// с масштабом 1:1. Временный файл удаляется в блоке <c>finally</c>.
        /// </para>
        /// <para>
        /// Для успешной работы требуются нативные библиотеки
        /// <c>libSkiaSharp</c> и <c>libHarfBuzzSharp</c>, доступные
        /// в выходной папке приложения (см. проблему <c>DllNotFoundException</c>
        /// для <c>libHarfBuzzSharp</c> в .NET Framework).
        /// </para>
        /// </remarks>
        public void ConvertToPdf(string outputPath, SKColor? backgroundColor = null)
        {
            EnsureNotDisposed();
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("Путь для сохранения не задан.", nameof(outputPath));

            string tempSvgPath = Path.Combine(Path.GetTempPath(),
                Guid.NewGuid().ToString("N") + ".svg");
            try
            {
                SaveSvg(tempSvgPath);
                using (var svg = new SKSvg())
                {
                    if (svg.Load(tempSvgPath) == null)
                        throw new InvalidOperationException("Не удалось загрузить SVG в Svg.Skia.");
                    svg.Picture.ToPdf(outputPath, backgroundColor ?? SKColors.Transparent, 1f, 1f);
                }
            }
            finally
            {
                if (File.Exists(tempSvgPath))
                {
                    try { File.Delete(tempSvgPath); } catch { }
                }
            }
        }

        /// <summary>
        /// Проверяет, что экземпляр не был освобождён.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// <see cref="_document"/> равен <c>null</c>, что означает,
        /// что был вызван <see cref="Dispose"/>.
        /// </exception>
        private void EnsureNotDisposed()
        {
            if (_document == null)
                throw new ObjectDisposedException(nameof(SvgTemplateProcessor));
        }
        /// <summary>
        /// Возвращает текущее состояние SVG-документа в виде массива байт
        /// (UTF-8 без BOM, с XML-объявлением и отступами).
        /// </summary>
        /// <returns>Массив байт с сериализованным SVG-документом.</returns>
        /// <exception cref="ObjectDisposedException">
        /// Экземпляр уже освобождён вызовом <see cref="Dispose"/>.
        /// </exception>
        /// <remarks>
        /// <para>
        /// Метод не пишет ничего на диск — результат полностью формируется в памяти,
        /// что удобно для передачи SVG по сети, сохранения в БД или передачи в
        /// другой API (например, в контроллер ASP.NET).
        /// </para>
        /// <para>
        /// Формат идентичен тому, что записывает <see cref="SaveSvg(string)"/>:
        /// <c>File.WriteAllBytes(path, GetByteArray())</c> даёт тот же файл, что
        /// и <c>SaveSvg(path)</c>.
        /// </para>
        /// </remarks>
        public byte[] GetByteArray()
        {
            EnsureNotDisposed();

            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                Encoding = new UTF8Encoding(false),
                OmitXmlDeclaration = false,
                CloseOutput = false
            };

            using (var ms = new MemoryStream())
            {
                using (var writer = XmlWriter.Create(ms, settings))
                {
                    _document.Write(writer);
                    writer.Flush();
                }
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Рендерит текущее состояние SVG-документа в PDF и возвращает
        /// результат в виде массива байт.
        /// </summary>
        /// <param name="backgroundColor">
        /// Цвет фона страницы PDF. Если <c>null</c>, используется прозрачный фон
        /// (<see cref="SKColors.Transparent"/>).
        /// </param>
        /// <returns>Массив байт с PDF-документом.</returns>
        /// <exception cref="ObjectDisposedException">
        /// Экземпляр уже освобождён вызовом <see cref="Dispose"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// <see cref="SKSvg"/> не смог загрузить сериализованный SVG
        /// (например, из-за ошибок в документе или отсутствия нативных
        /// библиотек SkiaSharp/HarfBuzzSharp).
        /// </exception>
        /// <remarks>
        /// <para>
        /// Метод не пишет ничего на диск: SVG сериализуется во временный файл
        /// (требование <see cref="SKSvg.Load(string)"/>), рендерится через
        /// <see cref="SKPicture.ToPdf(Stream, SKColor, float, float)"/>
        /// в <see cref="MemoryStream"/>, после чего временный файл удаляется
        /// в блоке <c>finally</c>.
        /// </para>
        /// <para>
        /// Для успешной работы, как и для <see cref="ConvertToPdf(string, SKColor?)"/>,
        /// требуются нативные библиотеки <c>libSkiaSharp</c> и <c>libHarfBuzzSharp</c>,
        /// доступные в выходной папке приложения.
        /// </para>
        /// </remarks>
        public byte[] GetByteArrayAsPdf(SKColor? backgroundColor = null)
        {
            EnsureNotDisposed();

            string tempSvgPath = Path.Combine(Path.GetTempPath(),
                Guid.NewGuid().ToString("N") + ".svg");
            try
            {
                SaveSvg(tempSvgPath);
                using (var svg = new SKSvg())
                {
                    if (svg.Load(tempSvgPath) == null)
                        throw new InvalidOperationException("Не удалось загрузить SVG в Svg.Skia.");

                    using (var pdfStream = new MemoryStream())
                    {
                        svg.Picture.ToPdf(pdfStream, backgroundColor ?? SKColors.Transparent, 1f, 1f);
                        return pdfStream.ToArray();
                    }
                }
            }
            finally
            {
                if (File.Exists(tempSvgPath))
                {
                    try { File.Delete(tempSvgPath); } catch { }
                }
            }
        }
        /// <summary>
        /// Устанавливает текст в поле с автоматическим переносом строк.
        /// Сначала ищет <see cref="SvgText"/> по id. Если текст не найден,
        /// ищет <see cref="SvgRectangle"/> с тем же id, создаёт поверх него
        /// <see cref="SvgText"/> и при необходимости увеличивает высоту прямоугольника.
        /// </summary>
        /// <param name="fieldId">id текстового поля или прямоугольника.</param>
        /// <param name="text">Текст для вставки.</param>
        /// <param name="fontSize">Размер шрифта в единицах SVG.</param>
        /// <param name="padding">Внутренний отступ от краёв прямоугольника.</param>
        /// <param name="charWidthFactor">
        /// Приблизительная ширина одного символа относительно размера шрифта.
        /// </param>
        /// <param name="fontColor">
        /// Цвет шрифта. Если <c>null</c>, атрибут <c>fill</c> у текста не меняется
        /// (сохраняется цвет из шаблона или наследуется от родителя).
        /// </param>
        public void SetFieldTextWithAutoWrap(
            string fieldId,
            string text,
            float fontSize,
            float padding = 4f,
            float charWidthFactor = 0.55f,
            SKColor? fontColor = null)
        {
            EnsureNotDisposed();

            if (string.IsNullOrWhiteSpace(fieldId))
                throw new ArgumentException("id поля не задан.", nameof(fieldId));
            if (fontSize <= 0)
                throw new ArgumentException("Размер шрифта должен быть больше нуля.", nameof(fontSize));

            // 1. Ищем существующий SvgText (в т.ч. созданный ранее как <id>_text).
            var textElement =
                _document.GetElementById<SvgText>(fieldId) ??
                _document.GetElementById<SvgText>(fieldId + "_text");

            // 2. Ищем прямоугольник с тем же id.
            var rect = _document.GetElementById<SvgRectangle>(fieldId);

            if (textElement == null && rect == null)
                throw new ArgumentException(
                    "Элемент с id '" + fieldId + "' не найден ни как SvgText, ни как SvgRectangle.",
                    nameof(fieldId));

            // 3. Вычисляем область размещения текста.
            float left, top, maxWidth;

            if (rect != null)
            {
                left = rect.X.Value + padding;
                top = rect.Y.Value + padding;
                maxWidth = rect.Width.Value - padding * 2f;
            }
            else
            {
                float existingX = (textElement.X != null && textElement.X.Count > 0)
                    ? textElement.X[0].Value : 0f;
                float existingBaselineY = (textElement.Y != null && textElement.Y.Count > 0)
                    ? textElement.Y[0].Value : fontSize;

                left = existingX;
                top = existingBaselineY - fontSize;
                maxWidth = 0f;
            }

            float charWidth = fontSize * charWidthFactor;
            float lineHeight = fontSize * 1.2f;
            var lines = WrapText(text, maxWidth, charWidth);

            // 4. При необходимости создаём SvgText поверх rect.
            if (textElement == null)
            {
                textElement = new SvgText
                {
                    ID = fieldId + "_text",
                    TextAnchor = SvgTextAnchor.Start
                };

                if (rect is ISvgTransformable transformableRect &&
                    transformableRect.Transforms != null &&
                    transformableRect.Transforms.Count > 0)
                {
                    textElement.Transforms = transformableRect.Transforms;
                }

                if (rect != null && rect.Parent != null)
                    rect.Parent.Children.Add(textElement);
                else
                    _document.Children.Add(textElement);
            }

            // 5. Задаём позицию и добавляем строки в виде <tspan>.
            textElement.Children.Clear();
            textElement.Text = string.Empty;
            textElement.Nodes.Clear();

            textElement.FontSize = new SvgUnit(SvgUnitType.User, fontSize);
            textElement.TextAnchor = SvgTextAnchor.Start;

            // X/Y на самом <text> — позиция первой строки.
            // X дублируется на каждом <tspan>, чтобы каждая строка начиналась слева,
            // а не продолжала предыдущую.
            textElement.X = new SvgUnitCollection
{
    new SvgUnit(SvgUnitType.User, left)
};
            textElement.Y = new SvgUnitCollection
{
    new SvgUnit(SvgUnitType.User, top + fontSize)
};

            if (fontColor.HasValue)
            {
                var c = fontColor.Value;
                textElement.Fill = new SvgColourServer(
                    System.Drawing.Color.FromArgb(c.Alpha, c.Red, c.Green, c.Blue));
            }

            for (int i = 0; i < lines.Count; i++)
            {
                var span = new SvgTextSpan
                {
                    Text = lines[i]
                };

                // КЛЮЧЕВОЕ: сбрасываем X для каждого <tspan>, иначе строка
                // продолжается с конца предыдущей, а не начинается заново.
                span.X = new SvgUnitCollection
    {
        new SvgUnit(SvgUnitType.User, left)
    };

                if (i > 0)
                {
                    span.Dy = new SvgUnitCollection
        {
            new SvgUnit(SvgUnitType.User, lineHeight)
        };
                }

                if (fontColor.HasValue)
                {
                    var c = fontColor.Value;
                    span.Fill = new SvgColourServer(
                        System.Drawing.Color.FromArgb(c.Alpha, c.Red, c.Green, c.Blue));
                }

                textElement.Children.Add(span);
            }

            // 6. Увеличиваем высоту rect, если строки не помещаются.
            if (rect != null)
            {
                float requiredHeight = padding * 2f + lines.Count * lineHeight;
                if (requiredHeight > rect.Height.Value)
                    rect.Height = new SvgUnit(SvgUnitType.User, requiredHeight);
            }
        }

        /// <summary>
        /// Задаёт координаты X/Y для текстового элемента SVG.
        /// </summary>
        private static void SetTextPosition(SvgTextBase element, float x, float y)
        {
            element.X = new SvgUnitCollection
    {
        new SvgUnit(SvgUnitType.User, x)
    };

            element.Y = new SvgUnitCollection
    {
        new SvgUnit(SvgUnitType.User, y)
    };
        }

        /// <summary>
        /// Эвристически разбивает текст на строки по ширине.
        /// Длинные слова при необходимости разрезаются посимвольно.
        /// </summary>
        private static List<string> WrapText(string text, float maxWidth, float charWidth)
        {
            var result = new List<string>();

            if (maxWidth <= 0 || charWidth <= 0)
            {
                result.Add(text ?? string.Empty);
                return result;
            }

            string normalized = (text ?? string.Empty)
                .Replace("\r\n", "\n")
                .Replace('\r', '\n');

            string[] paragraphs = normalized.Split('\n');

            foreach (string paragraph in paragraphs)
            {
                if (paragraph.Length == 0)
                {
                    result.Add(string.Empty);
                    continue;
                }

                string[] words = paragraph.Split(
                    new[] { ' ' },
                    StringSplitOptions.RemoveEmptyEntries);

                var line = new StringBuilder();

                foreach (string word in words)
                {
                    string candidate = line.Length == 0
                        ? word
                        : line + " " + word;

                    if (candidate.Length * charWidth <= maxWidth)
                    {
                        if (line.Length > 0)
                            line.Append(' ');

                        line.Append(word);
                        continue;
                    }

                    if (line.Length > 0)
                    {
                        result.Add(line.ToString());
                        line.Clear();
                    }

                    // Слово само помещается в строку.
                    if (word.Length * charWidth <= maxWidth)
                    {
                        line.Append(word);
                        continue;
                    }

                    // Слово слишком длинное — режем посимвольно.
                    int maxChars = Math.Max(1, (int)Math.Floor(maxWidth / charWidth));

                    for (int i = 0; i < word.Length; i += maxChars)
                    {
                        int length = Math.Min(maxChars, word.Length - i);
                        string part = word.Substring(i, length);

                        if (i + length < word.Length)
                            result.Add(part);
                        else
                            line.Append(part);
                    }
                }

                if (line.Length > 0)
                    result.Add(line.ToString());
            }

            if (result.Count == 0)
                result.Add(string.Empty);

            return result;
        }
        /// <summary>
        /// Освобождает ресурсы, удерживаемые процессором, и обнуляет
        /// внутреннюю ссылку на <see cref="SvgDocument"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// После вызова этого метода любые операции с экземпляром
        /// (включая чтение свойства <see cref="Document"/>) приводят
        /// к <see cref="ObjectDisposedException"/>.
        /// </para>
        /// <para>
        /// Метод идемпотентен: повторный вызов не генерирует исключений.
        /// </para>
        /// </remarks>
        public void Dispose()
        {
            _document = null;
        }
    }
}