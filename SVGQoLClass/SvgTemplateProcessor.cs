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
    /// Высокоуровневый API для работы с SVG-шаблонами: загрузка, редактирование
    /// текстовых полей, сохранение в SVG и экспорт в PDF.
    /// </summary>
    /// <remarks>
    /// Создаётся только через фабричные методы. Реализует <see cref="IDisposable"/>.
    /// </remarks>
    public sealed class SvgTemplateProcessor : IDisposable
    {
        #region ПОЛЯ
        /// <summary>
        /// Нижележащий SVG-документ. <c>null</c> после <see cref="Dispose"/>.
        /// </summary>
        private SvgDocument _document;
        #endregion
        #region СВОЙСТВА
        /// <summary>
        /// Нижележащий <see cref="SvgDocument"/> для нестандартных сценариев.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Экземпляр освобождён.</exception>
        public SvgDocument Document
        {
            get
            {
                EnsureNotDisposed();
                return _document;
            }
        }
        #endregion

        /// <summary>
        /// Инициализирует экземпляр с уже загруженным SVG-документом.
        /// </summary>
        private SvgTemplateProcessor(SvgDocument document)
        {
            _document = document;
        }

        /// <summary>
        /// Освобождает ресурсы и обнуляет ссылку на документ. Идемпотентен.
        /// </summary>
        public void Dispose()
        {
            _document = null;
        }
        #region ФАБРИЧНЫЕ МЕТОДЫ

        /// <summary>
        /// Загружает SVG-документ из файла.
        /// </summary>
        /// <param name="path">Путь к существующему SVG-файлу.</param>
        /// <exception cref="ArgumentException">Путь не задан.</exception>
        /// <exception cref="FileNotFoundException">Файл не найден.</exception>
        public static SvgTemplateProcessor FromFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Путь к файлу не задан.", nameof(path));
            if (!File.Exists(path))
                throw new FileNotFoundException("SVG-файл не найден: " + path, path);

            return new SvgTemplateProcessor(SvgDocument.Open(path));
        }

        /// <summary>
        /// Загружает SVG-документ из потока (UTF-8). Поток не закрывается.
        /// </summary>
        /// <exception cref="ArgumentNullException">Поток равен <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Не удалось разобрать SVG.</exception>
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
        /// Загружает SVG-документ из строки с разметкой.
        /// </summary>
        /// <exception cref="ArgumentException">Строка пуста или <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Не удалось разобрать SVG.</exception>
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
        /// Создаёт экземпляр поверх готового <see cref="SvgDocument"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">Документ равен <c>null</c>.</exception>
        public static SvgTemplateProcessor FromDocument(SvgDocument document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            return new SvgTemplateProcessor(document);
        }
        #endregion
        #region ОПЕРАЦИИ
        /// <summary>
        /// Устанавливает текст для поля с указанным id.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Экземпляр освобождён.</exception>
        /// <exception cref="ArgumentException">Элемент не найден или не является <see cref="SvgText"/>.</exception>
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
        /// Изменяет размеры поля. Для <see cref="SvgRectangle"/> — width/height,
        /// для <see cref="SvgText"/> — textLength (height игнорируется).
        /// </summary>
        /// <exception cref="ObjectDisposedException">Экземпляр освобождён.</exception>
        /// <exception cref="ArgumentException">Элемент не найден.</exception>
        /// <exception cref="NotSupportedException">Тип элемента не поддерживается.</exception>
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
        /// Эвристически подбирает размеры поля под длину текста.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Экземпляр освобождён.</exception>
        /// <exception cref="ArgumentException">Элемент не найден.</exception>
        /// <exception cref="NotSupportedException">Тип элемента не поддерживается.</exception>
        public void FitFieldToText(string fieldId, string text,
            float charWidth = 10f, float padding = 4f)
        {
            if (string.IsNullOrEmpty(text)) return;
            float estimatedWidth = text.Length * charWidth + padding * 2;
            float estimatedHeight = charWidth * 1.5f + padding * 2;
            SetFieldSize(fieldId, estimatedWidth, estimatedHeight);
        }

        /// <summary>
        /// Сохраняет текущее состояние SVG в файл.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Экземпляр освобождён.</exception>
        /// <exception cref="ArgumentException">Путь не задан.</exception>
        /// <exception cref="IOException">Ошибка записи файла.</exception>
        public void SaveSvg(string outputPath)
        {
            EnsureNotDisposed();
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("Путь для сохранения не задан.", nameof(outputPath));

            File.WriteAllBytes(outputPath, GetByteArray());
        }

        /// <summary>
        /// Конвертирует SVG в PDF-файл.
        /// </summary>
        /// <param name="backgroundColor">Цвет фона; по умолчанию прозрачный.</param>
        /// <exception cref="ObjectDisposedException">Экземпляр освобождён.</exception>
        /// <exception cref="ArgumentException">Путь не задан.</exception>
        /// <exception cref="InvalidOperationException">Ошибка загрузки в <see cref="SKSvg"/>.</exception>
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
        /// Проверяет, что экземпляр не освобождён.
        /// </summary>
        private void EnsureNotDisposed()
        {
            if (_document == null)
                throw new ObjectDisposedException(nameof(SvgTemplateProcessor));
        }

        /// <summary>
        /// Возвращает текущий SVG в виде массива байт (UTF-8 без BOM, с отступами).
        /// </summary>
        /// <exception cref="ObjectDisposedException">Экземпляр освобождён.</exception>
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
        /// Рендерит текущий SVG в PDF и возвращает массив байт.
        /// </summary>
        /// <param name="backgroundColor">Цвет фона; по умолчанию прозрачный.</param>
        /// <exception cref="ObjectDisposedException">Экземпляр освобождён.</exception>
        /// <exception cref="InvalidOperationException">Ошибка загрузки в <see cref="SKSvg"/>.</exception>
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
        /// Устанавливает текст с автоматическим переносом строк.
        /// Ищет <see cref="SvgText"/> по id, либо <see cref="SvgRectangle"/> с тем же id
        /// (тогда создаётся <see cref="SvgText"/> и при необходимости увеличивается высота прямоугольника).
        /// </summary>
        public void SetFieldTextWithAutoWrap(
            string fieldId,
            string text,
            float fontSize,
            float padding = 4f,
            float charWidthFactor = 0.55f,
            SKColor? fontColor = null)
        {
            EnsureNotDisposed();
            ValidateWrapArgs(fieldId, fontSize);

            ResolveFieldElements(fieldId, out var textElement, out var rect);

            ComputeLayoutArea(
                rect, textElement, padding, fontSize,
                out float left, out float top, out float maxWidth);

            float charWidth = fontSize * charWidthFactor;
            float lineHeight = fontSize * 1.2f;
            var lines = WrapText(text, maxWidth, charWidth);

            textElement = EnsureTextElement(textElement, rect, fieldId);
            ApplyLinesToText(textElement, lines, left, top, fontSize, lineHeight, fontColor);
            EnsureRectHeightForLines(rect, lines.Count, lineHeight, padding);
        }

        /// <summary>
        /// Устанавливает текст с автопереносом и подбором размера шрифта,
        /// чтобы уложиться в прямоугольник (с возможностью увеличить его
        /// не более чем на <paramref name="maxRowsIncrement"/> строк).
        /// </summary>
        public void SetFieldTextWithAutoFontSizing(
            string fieldId,
            string text,
            float fontSize,
            int maxRowsIncrement = 3,
            float padding = 4f,
            float charWidthFactor = 0.55f,
            SKColor? fontColor = null)
        {
            EnsureNotDisposed();
            ValidateWrapArgs(fieldId, fontSize);

            if (maxRowsIncrement < 0)
                throw new ArgumentException(
                    "Максимальное количество добавляемых строк не может быть отрицательным.",
                    nameof(maxRowsIncrement));

            ResolveFieldElements(fieldId, out var textElement, out var rect);

            float finalFontSize;
            float finalLineHeight;
            List<string> finalLines;

            if (rect != null)
            {
                finalFontSize = FitFontSizeToRect(
                    text, rect, fontSize, padding, charWidthFactor, maxRowsIncrement,
                    out finalLines, out finalLineHeight);
            }
            else
            {
                finalFontSize = fontSize;
                finalLineHeight = fontSize * 1.2f;
                finalLines = WrapText(text, 0f, finalFontSize * charWidthFactor);
            }

            ComputeLayoutArea(
                rect, textElement, padding, finalFontSize,
                out float left, out float top, out _);

            textElement = EnsureTextElement(textElement, rect, fieldId);
            ApplyLinesToText(
                textElement, finalLines, left, top,
                finalFontSize, finalLineHeight, fontColor);
            EnsureRectHeightForLines(rect, finalLines.Count, finalLineHeight, padding);
        }
        #endregion
        #region ПРИВАТНЫЕ ХЕЛПЕРЫ ДЛЯ РАБОТЫ С ТЕКСТОМ
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
        /// Эвристически разбивает текст на строки по ширине; длинные слова режутся посимвольно.
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
        /// Проверяет аргументы, общие для методов автозаписи текста.
        /// </summary>
        private static void ValidateWrapArgs(string fieldId, float fontSize)
        {
            if (string.IsNullOrWhiteSpace(fieldId))
                throw new ArgumentException("id поля не задан.", nameof(fieldId));
            if (fontSize <= 0)
                throw new ArgumentException("Размер шрифта должен быть больше нуля.", nameof(fontSize));
        }

        /// <summary>
        /// Ищет <see cref="SvgText"/> по id (или <c>id + "_text"</c>) и <see cref="SvgRectangle"/>
        /// по тому же id. Бросает исключение, если ничего не найдено.
        /// </summary>
        private void ResolveFieldElements(
            string fieldId,
            out SvgText textElement,
            out SvgRectangle rect)
        {
            textElement = _document.GetElementById<SvgText>(fieldId)
                          ?? _document.GetElementById<SvgText>(fieldId + "_text");
            rect = _document.GetElementById<SvgRectangle>(fieldId);

            if (textElement == null && rect == null)
                throw new ArgumentException(
                    "Элемент с id '" + fieldId + "' не найден ни как SvgText, ни как SvgRectangle.",
                    nameof(fieldId));
        }

        /// <summary>
        /// Вычисляет область размещения текста (left, top, maxWidth).
        /// </summary>
        private static void ComputeLayoutArea(
            SvgRectangle rect,
            SvgText textElement,
            float padding,
            float fontSize,
            out float left,
            out float top,
            out float maxWidth)
        {
            if (rect != null)
            {
                left = rect.X.Value + padding;
                top = rect.Y.Value + padding;
                maxWidth = rect.Width.Value - padding * 2f;
                return;
            }

            float existingX = (textElement.X != null && textElement.X.Count > 0)
                ? textElement.X[0].Value
                : 0f;
            float existingBaselineY = (textElement.Y != null && textElement.Y.Count > 0)
                ? textElement.Y[0].Value
                : fontSize;

            left = existingX;
            top = existingBaselineY - fontSize;
            maxWidth = 0f;
        }

        /// <summary>
        /// Возвращает существующий <see cref="SvgText"/> либо создаёт новый (id = <c>fieldId + "_text"</c>)
        /// и добавляет его в DOM.
        /// </summary>
        private SvgText EnsureTextElement(
            SvgText textElement,
            SvgRectangle rect,
            string fieldId)
        {
            if (textElement != null)
                return textElement;

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

            return textElement;
        }

        /// <summary>
        /// Очищает <see cref="SvgText"/> и записывает строки в виде <c>&lt;tspan&gt;</c>
        /// с позиционированием, размером шрифта и опциональным цветом.
        /// </summary>
        private static void ApplyLinesToText(
            SvgText textElement,
            List<string> lines,
            float left,
            float top,
            float fontSize,
            float lineHeight,
            SKColor? fontColor)
        {
            textElement.Children.Clear();
            textElement.Text = string.Empty;
            textElement.Nodes.Clear();

            textElement.FontSize = new SvgUnit(SvgUnitType.User, fontSize);
            textElement.TextAnchor = SvgTextAnchor.Start;

            // Первая строка — на baseline, соответствующем top + fontSize.
            SetTextPosition(textElement, left, top + fontSize);

            if (fontColor.HasValue)
            {
                var c = fontColor.Value;
                textElement.Fill = new SvgColourServer(
                    System.Drawing.Color.FromArgb(c.Alpha, c.Red, c.Green, c.Blue));
            }

            for (int i = 0; i < lines.Count; i++)
            {
                var span = new SvgTextSpan { Text = lines[i] };

                // КЛЮЧЕВОЕ: X на каждом tspan, иначе строка «продолжается»
                // с конца предыдущей, а не начинается заново.
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
        }

        /// <summary>
        /// Увеличивает высоту прямоугольника (если нужно), чтобы поместились все строки.
        /// Уменьшать высоту не умеет.
        /// </summary>
        private static void EnsureRectHeightForLines(
            SvgRectangle rect,
            int lineCount,
            float lineHeight,
            float padding)
        {
            if (rect == null)
                return;

            float requiredHeight = padding * 2f + lineCount * lineHeight;
            if (requiredHeight > rect.Height.Value)
                rect.Height = new SvgUnit(SvgUnitType.User, requiredHeight);
        }

        /// <summary>
        /// Подбирает минимальный (шаг 0.5) размер шрифта, при котором текст
        /// укладывается в прямоугольник, увеличенный не более чем на
        /// <paramref name="maxRowsIncrement"/> строк.
        /// </summary>
        private static float FitFontSizeToRect(
            string text,
            SvgRectangle rect,
            float initialFontSize,
            float padding,
            float charWidthFactor,
            int maxRowsIncrement,
            out List<string> lines,
            out float lineHeight)
        {
            const float MinFontSize = 1f;
            const float ShrinkStep = 0.5f;

            float initialLineHeight = initialFontSize * 1.2f;
            float initialAvailableHeight = rect.Height.Value - padding * 2f;
            int initialRows = Math.Max(1, (int)Math.Floor(initialAvailableHeight / initialLineHeight));
            int maxRows = initialRows + maxRowsIncrement;
            float maxAllowedHeight = padding * 2f + maxRows * initialLineHeight;
            float maxWidth = rect.Width.Value - padding * 2f;

            float currentFontSize = initialFontSize;

            while (true)
            {
                float currentLineHeight = currentFontSize * 1.2f;
                float currentCharWidth = currentFontSize * charWidthFactor;

                lines = WrapText(text, maxWidth, currentCharWidth);

                float neededHeight = padding * 2f + lines.Count * currentLineHeight;
                if (neededHeight <= maxAllowedHeight || currentFontSize <= MinFontSize)
                {
                    lineHeight = currentLineHeight;
                    return currentFontSize;
                }

                currentFontSize -= ShrinkStep;
                if (currentFontSize < MinFontSize)
                    currentFontSize = MinFontSize;
            }
        }
    }
    #endregion
}